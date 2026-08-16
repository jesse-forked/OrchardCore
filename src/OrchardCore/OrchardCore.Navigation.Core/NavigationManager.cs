using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrchardCore.Environment.Shell;

namespace OrchardCore.Navigation;

public class NavigationManager : INavigationManager
{
    private readonly IEnumerable<INavigationProvider> _navigationProviders;
    private readonly ILogger _logger;
    protected readonly ShellSettings _shellSettings;
    private readonly IUrlHelperFactory _urlHelperFactory;
    private readonly IAuthorizationService _authorizationService;
    private readonly NavigationOptions _navigationOptions;

    private IUrlHelper _urlHelper;

    public NavigationManager(
        IEnumerable<INavigationProvider> navigationProviders,
        ILogger<NavigationManager> logger,
        ShellSettings shellSettings,
        IUrlHelperFactory urlHelperFactory,
        IAuthorizationService authorizationService,
        IOptions<NavigationOptions> navigationOptions)
    {
        _navigationProviders = navigationProviders;
        _logger = logger;
        _shellSettings = shellSettings;
        _urlHelperFactory = urlHelperFactory;
        _authorizationService = authorizationService;
        _navigationOptions = navigationOptions.Value;
    }

    public async Task<IEnumerable<MenuItem>> BuildMenuAsync(string name, ActionContext actionContext)
    {
        var builder = new NavigationBuilder();

        // Processes all navigation builders to create a flat list of menu items.
        // If a navigation builder fails, it is ignored.
        foreach (var navigationProvider in _navigationProviders)
        {
            try
            {
                await navigationProvider.BuildNavigationAsync(name, builder);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An exception occurred while building the menu '{MenuName}'", name);
            }
        }

        var menuItems = builder.Build();

        // Merge all menu hierarchies into a single one.
        Merge(menuItems, _navigationOptions.RequireMenuItemId);

        if (_navigationOptions.RequireMenuItemId)
        {
            menuItems = RemoveItemsWithoutId(menuItems, name);
        }

        // Remove unauthorized menu items.
        menuItems = await AuthorizeAsync(menuItems, actionContext.HttpContext.User);

        // Compute Url and RouteValues properties to Href.
        menuItems = ComputeHref(menuItems, actionContext);

        // Keep only menu items with an Href, or that have child items with an Href.
        menuItems = Reduce(menuItems);

        return menuItems;
    }

    /// <summary>
    /// Mutates a list of <see cref="MenuItem"/> into a hierarchy.
    /// </summary>
    /// <param name="requireMenuItemId">
    /// When <c>true</c>, items are matched for merging by <see cref="MenuItem.Id"/> instead of by
    /// caption, so merging is unaffected by localization. Only items that both have a non-empty
    /// <see cref="MenuItem.Id"/> can match; items without one are left alone here and are removed
    /// later by <see cref="RemoveItemsWithoutId"/>. When <c>false</c>, matching is by caption, to
    /// preserve current behavior for sites and third-party navigation providers that don't set an
    /// <see cref="MenuItem.Id"/> yet.
    /// </param>
    private static void Merge(List<MenuItem> items, bool requireMenuItemId)
    {
        // Use two cursors to find all similar items. If the same item is represented
        // by multiple menu items, try to merge it recursively.
        for (var i = 0; i < items.Count; i++)
        {
            var source = items[i];
            var merged = false;
            for (var j = items.Count - 1; j > i; j--)
            {
                var cursor = items[j];

                var isMatch = requireMenuItemId
                    ? !string.IsNullOrEmpty(cursor.Id) && !string.IsNullOrEmpty(source.Id) && string.Equals(cursor.Id, source.Id, StringComparison.Ordinal)
                    : string.Equals(cursor.Text.Name, source.Text.Name, StringComparison.OrdinalIgnoreCase);

                // A match is found, add all its items to the source.
                if (isMatch)
                {
                    merged = true;
                    foreach (var child in cursor.Items)
                    {
                        source.Items.Add(child);
                    }

                    items.RemoveAt(j);

                    // If the item to merge is more authoritative then use its values.
                    if (cursor.Priority > source.Priority)
                    {
                        source.Culture = cursor.Culture;
                        source.Href = cursor.Href;
                        source.Id = cursor.Id;
                        source.LinkToFirstChild = cursor.LinkToFirstChild;
                        source.LocalNav = cursor.LocalNav;
                        source.Position = cursor.Position;
                        source.Resource = cursor.Resource;
                        source.RouteValues = cursor.RouteValues;
                        source.Text = cursor.Text;
                        source.Url = cursor.Url;
                        source.Target = cursor.Target;

                        source.Permissions.Clear();
                        source.Permissions.AddRange(cursor.Permissions);

                        source.Classes.Clear();
                        source.Classes.AddRange(cursor.Classes);
                    }

                    // Fallback to get the same behavior than before having the Priority var.
                    if (cursor.Priority == source.Priority)
                    {
                        if (cursor.Position != null && source.Position == null)
                        {
                            source.Culture = cursor.Culture;
                            source.Href = cursor.Href;
                            source.Id = cursor.Id;
                            source.LinkToFirstChild = cursor.LinkToFirstChild;
                            source.LocalNav = cursor.LocalNav;
                            source.Position = cursor.Position;
                            source.Resource = cursor.Resource;
                            source.RouteValues = cursor.RouteValues;
                            source.Text = cursor.Text;
                            source.Url = cursor.Url;
                            source.Target = cursor.Target;

                            source.Permissions.Clear();
                            source.Permissions.AddRange(cursor.Permissions);

                            source.Classes.Clear();
                            source.Classes.AddRange(cursor.Classes);
                        }
                    }
                }
            }

            // If some items have been merged, apply recursively.
            if (merged)
            {
                Merge(source.Items, requireMenuItemId);
            }
        }
    }

    /// <summary>
    /// Removes any <see cref="MenuItem"/> that does not have an explicit <see cref="MenuItem.Id"/>,
    /// logging an error for each one removed. Never throws.
    /// </summary>
    private List<MenuItem> RemoveItemsWithoutId(List<MenuItem> items, string menuName)
    {
        var filtered = new List<MenuItem>(items.Count);
        foreach (var item in items)
        {
            item.Items = RemoveItemsWithoutId(item.Items, menuName);

            if (string.IsNullOrEmpty(item.Id))
            {
                _logger.LogError(
                    "Menu item '{MenuItemText}' in menu '{MenuName}' was excluded because it does not have an Id. " +
                    "Set an Id on the navigation item to include it in the menu.",
                    item.Text?.Name, menuName);

                continue;
            }

            filtered.Add(item);
        }

        return filtered;
    }

    /// <summary>
    /// Computes the <see cref="MenuItem.Href"/> properties based on <see cref="MenuItem.Url"/>
    /// and <see cref="MenuItem.RouteValues"/> values.
    /// </summary>
    private List<MenuItem> ComputeHref(List<MenuItem> menuItems, ActionContext actionContext)
    {
        foreach (var menuItem in menuItems)
        {
            menuItem.Href = GetUrl(menuItem.Url, menuItem.RouteValues, actionContext);
            menuItem.Items = ComputeHref(menuItem.Items, actionContext);
        }

        return menuItems;
    }

    /// <summary>
    /// Gets the url.from a menu item url a routeValueDictionary and an actionContext.
    /// </summary>
    /// <param name="menuItemUrl"></param>
    /// <param name="routeValueDictionary"></param>
    /// <param name="actionContext"></param>
    /// <returns></returns>
    private string GetUrl(string menuItemUrl, RouteValueDictionary routeValueDictionary, ActionContext actionContext)
    {
        if (routeValueDictionary?.Count > 0)
        {
            _urlHelper ??= _urlHelperFactory.GetUrlHelper(actionContext);

            return _urlHelper.RouteUrl(new UrlRouteContext { Values = routeValueDictionary });
        }

        if (string.IsNullOrEmpty(menuItemUrl))
        {
            return "#";
        }

        if (menuItemUrl[0] == '/' || menuItemUrl.Contains("://"))
        {
            // Return the unescaped url and let the browser generate all uri components.
            return menuItemUrl;
        }

        if (menuItemUrl.StartsWith("~/", StringComparison.Ordinal))
        {
            menuItemUrl = menuItemUrl[2..];
        }

        // Use the unescaped 'Value' to not encode some possible reserved delimiters.
        return actionContext.HttpContext.Request.PathBase.Add($"/{menuItemUrl}").Value;
    }

    /// <summary>
    /// Updates the items by checking for permissions.
    /// </summary>
    private async Task<List<MenuItem>> AuthorizeAsync(IEnumerable<MenuItem> items, ClaimsPrincipal user)
    {
        var filtered = new List<MenuItem>();
        foreach (var item in items)
        {
            // TODO: Attach actual user and remove this clause.
            if (user == null)
            {
                filtered.Add(item);
            }
            else if (item.Permissions.Count == 0)
            {
                filtered.Add(item);
            }
            else
            {
                // When multiple permissions are supplied all permissions must be authorized.
                var isAuthorized = true;
                foreach (var permission in item.Permissions)
                {
                    if (!(await _authorizationService.AuthorizeAsync(user, permission, item.Resource)))
                    {
                        isAuthorized = false;
                        break;
                    }
                }

                if (isAuthorized)
                {
                    filtered.Add(item);
                }
            }

            // Process child items.
            item.Items = (await AuthorizeAsync(item.Items, user));
        }

        return filtered;
    }

    /// <summary>
    /// Retains only menu items with an Href, or that have child items with an Href.
    /// </summary>
    private static List<MenuItem> Reduce(IEnumerable<MenuItem> items)
    {
        var filtered = items.ToList();
        foreach (var item in items)
        {
            if (!HasHrefOrChildHref(item))
            {
                filtered.Remove(item);
            }

            item.Items = Reduce(item.Items);
        }

        return filtered;
    }

    private static bool HasHrefOrChildHref(MenuItem item)
    {
        if (item.Href != "#")
        {
            return true;
        }

        return item.Items.Any(HasHrefOrChildHref);
    }
}

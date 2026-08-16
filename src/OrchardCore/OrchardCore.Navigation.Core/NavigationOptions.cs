namespace OrchardCore.Navigation;

/// <summary>
/// Provides configuration options for building navigation menus.
/// </summary>
public class NavigationOptions
{
    /// <summary>
    /// <para>When <c>true</c>, menu items are merged by <see cref="MenuItem.Id"/> instead of by
    /// caption, so merging is unaffected by localization. Afterwards, any <see cref="MenuItem"/>
    /// that still does not have an explicit <see cref="MenuItem.Id"/> is excluded from the built
    /// menu. Each excluded item is logged as an error identifying its menu and caption so it can
    /// be found and fixed. Building a menu never throws because of this setting; missing items
    /// are simply omitted from the result, matching how a failing <see cref="INavigationProvider"/>
    /// is already logged and skipped.</para>
    ///
    /// <para>This defaults to <c>false</c> to preserve current behavior for existing sites and
    /// third-party navigation providers: menu items are merged by caption, and an item without an
    /// <see cref="MenuItem.Id"/> is still included. Requiring an explicit <see cref="MenuItem.Id"/>
    /// and merging by it is expected to become the default, and eventually the only, behavior in a
    /// future version, so navigation providers should be updated to always set one.</para>
    /// </summary>
    public bool RequireMenuItemId { get; set; }
}

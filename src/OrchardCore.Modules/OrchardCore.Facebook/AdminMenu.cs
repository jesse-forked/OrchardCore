using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;

namespace OrchardCore.Facebook;

public sealed class AdminMenu : AdminNavigationProvider
{
    private static readonly RouteValueDictionary s_routeValues = new()
    {
        { "area", "OrchardCore.Settings" },
        { "groupId", FacebookConstants.Features.Core },
    };

    internal readonly IStringLocalizer S;

    public AdminMenu(IStringLocalizer<AdminMenu> stringLocalizer)
    {
        S = stringLocalizer;
    }

    protected override ValueTask BuildAsync(NavigationBuilder builder)
    {
        if (NavigationHelper.UseLegacyFormat())
        {
            builder
                .Add(S["Configuration"], configuration => configuration.Id("configuration")
                    .Add(S["Settings"], settings => settings.Id("settings")
                        .Add(S["Meta App"], S["Meta App"].PrefixPosition(), metaApp => metaApp
                            .AddClass("facebookApp")
                            .Id("facebookApp")
                            .Action("Index", "Admin", s_routeValues)
                            .Permission(Permissions.ManageFacebookApp)
                            .LocalNav()
                        )
                    )
                );

            return ValueTask.CompletedTask;
        }

        builder
            .Add(S["Settings"], settings => settings.Id("settings")
                .Add(S["Integrations"], S["Integrations"].PrefixPosition(), integrations => integrations.Id("integrations")
                    .Add(S["Meta App"], S["Meta App"].PrefixPosition(), metaApp => metaApp
                        .AddClass("facebookApp")
                        .Id("facebookApp")
                        .Action("Index", "Admin", s_routeValues)
                        .Permission(Permissions.ManageFacebookApp)
                        .LocalNav()
                    )
                )
            );

        return ValueTask.CompletedTask;
    }
}

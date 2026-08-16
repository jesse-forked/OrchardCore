using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;

namespace OrchardCore.Facebook;

public sealed class AdminMenuPixel : AdminNavigationProvider
{
    private static readonly RouteValueDictionary s_routeValues = new()
    {
        { "area", "OrchardCore.Settings" },
        { "groupId", FacebookConstants.PixelSettingsGroupId },
    };

    internal readonly IStringLocalizer S;

    public AdminMenuPixel(
        IStringLocalizer<AdminMenuLogin> stringLocalizer)
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
                       .Add(S["Meta Pixel"], S["Meta Pixel"].PrefixPosition(), pixel => pixel
                           .AddClass("facebookPixel")
                           .Id("facebookPixel")
                           .Action("Index", "Admin", s_routeValues)
                           .Permission(FacebookConstants.ManageFacebookPixelPermission)
                           .LocalNav()
                       )
                   )
               );

            return ValueTask.CompletedTask;
        }

        builder
            .Add(S["Settings"], settings => settings.Id("settings")
                .Add(S["Integrations"], S["Integrations"].PrefixPosition(), integrations => integrations.Id("integrations")
                    .Add(S["Meta Pixel"], S["Meta Pixel"].PrefixPosition(), pixel => pixel
                        .AddClass("facebookPixel")
                        .Id("facebookPixel")
                        .Action("Index", "Admin", s_routeValues)
                        .Permission(FacebookConstants.ManageFacebookPixelPermission)
                        .LocalNav()
                    )
                )
            );

        return ValueTask.CompletedTask;
    }
}

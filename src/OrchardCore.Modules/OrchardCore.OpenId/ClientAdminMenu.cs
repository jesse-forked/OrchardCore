using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;

namespace OrchardCore.OpenId;

public sealed class ClientAdminMenu : AdminNavigationProvider
{
    private static readonly RouteValueDictionary s_clientRouteValues = new()
    {
        { "area", "OrchardCore.Settings" },
        { "groupId", "OrchardCore.OpenId.Client" },
    };


    internal readonly IStringLocalizer S;

    public ClientAdminMenu(IStringLocalizer<ClientAdminMenu> stringLocalizer)
    {
        S = stringLocalizer;
    }

    protected override ValueTask BuildAsync(NavigationBuilder builder)
    {
        if (NavigationHelper.UseLegacyFormat())
        {
            builder
               .Add(S["Security"], security => security.Id("security")
                   .Add(S["OpenID Connect"], S["OpenID Connect"].PrefixPosition(), openId => openId
                       .AddClass("openid")
                       .Id("openid")
                       .Add(S["Settings"], S["Settings"].PrefixPosition(), settings => settings.Id("settings")
                           .Add(S["Authentication Client"], S["Authentication Client"].PrefixPosition(), client => client.Id("authentication-client")
                               .Action("Index", "Admin", s_clientRouteValues)
                               .Permission(OpenIdPermissions.ManageClientSettings)
                               .LocalNav()
                           )
                       )
                   )
               );

            return ValueTask.CompletedTask;
        }

        builder
            .Add(S["Settings"], settings => settings.Id("settings")
                .Add(S["OpenID Connect"], S["OpenID Connect"].PrefixPosition(), openId => openId.Id("openid-connect")
                    .Add(S["Authentication Client"], S["Authentication Client"].PrefixPosition(), client => client.Id("authentication-client")
                        .Action("Index", "Admin", s_clientRouteValues)
                        .Permission(OpenIdPermissions.ManageClientSettings)
                        .LocalNav()
                    )
                )
            );

        return ValueTask.CompletedTask;
    }
}

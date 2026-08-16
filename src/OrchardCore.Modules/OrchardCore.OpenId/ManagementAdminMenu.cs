using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;

namespace OrchardCore.OpenId;

public sealed class ManagementAdminMenu : AdminNavigationProvider
{
    internal readonly IStringLocalizer S;

    public ManagementAdminMenu(IStringLocalizer<ManagementAdminMenu> stringLocalizer)
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
                    .Add(S["Management"], S["Management"].PrefixPosition(), management => management.Id("management")
                        .Add(S["Applications"], S["Applications"].PrefixPosition(), applications => applications.Id("applications")
                            .Action("Index", "Application", "OrchardCore.OpenId")
                            .Permission(OpenIdPermissions.ManageApplications)
                            .LocalNav()
                        )
                        .Add(S["Scopes"], S["Scopes"].PrefixPosition(), applications => applications.Id("scopes")
                            .Action("Index", "Scope", "OrchardCore.OpenId")
                            .Permission(OpenIdPermissions.ManageScopes)
                            .LocalNav()
                        )
                    )
                )
            );

            return ValueTask.CompletedTask;
        }

        builder
            .Add(S["Access Control"], accessControl => accessControl.Id("access-control")
                .Add(S["OpenID Connect"], S["OpenID Connect"].PrefixPosition(), openId => openId
                    .AddClass("openid")
                    .Id("openid")
                    .Add(S["Applications"], S["Applications"].PrefixPosition(), applications => applications.Id("applications")
                        .Action("Index", "Application", "OrchardCore.OpenId")
                        .Permission(OpenIdPermissions.ManageApplications)
                        .LocalNav()
                    )
                    .Add(S["Scopes"], S["Scopes"].PrefixPosition(), applications => applications.Id("scopes")
                        .Action("Index", "Scope", "OrchardCore.OpenId")
                        .Permission(OpenIdPermissions.ManageScopes)
                        .LocalNav()
                    )
                )
            );

        return ValueTask.CompletedTask;
    }
}

using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;

namespace OrchardCore.Roles;

public sealed class AdminMenu : AdminNavigationProvider
{
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
                .Add(S["Security"], security => security.Id("security")
                    .Add(S["Roles"], S["Roles"].PrefixPosition(), roles => roles
                        .AddClass("roles")
                        .Id("roles")
                        .Action("Index", "Admin", "OrchardCore.Roles")
                        .Permission(RolesPermissions.ViewRoles)
                        .LocalNav()
                    )
                );

            return ValueTask.CompletedTask;
        }

        builder
            .Add(S["Access Control"], accessControl => accessControl.Id("access-control")
                .Add(S["Roles"], S["Roles"].PrefixPosition(), roles => roles
                    .AddClass("roles")
                    .Id("roles")
                    .Action("Index", "Admin", "OrchardCore.Roles")
                    .Permission(RolesPermissions.ViewRoles)
                    .LocalNav()
                )
            );

        return ValueTask.CompletedTask;
    }
}

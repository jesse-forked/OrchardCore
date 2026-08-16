using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;

namespace OrchardCore.Deployment;

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
                .Add(S["Configuration"], configuration => configuration.Id("configuration")
                    .Add(S["Import/Export"], S["Import/Export"].PrefixPosition(), import => import.Id("import-export")
                        .Add(S["Deployment Plans"], S["Deployment Plans"].PrefixPosition(), deployment => deployment.Id("deployment-plans")
                            .Action("Index", "DeploymentPlan", "OrchardCore.Deployment")
                            .Permission(DeploymentPermissions.Export)
                            .LocalNav()
                        )
                        .Add(S["Package Import"], S["Package Import"].PrefixPosition(), deployment => deployment.Id("package-import")
                            .Action("Index", "Import", "OrchardCore.Deployment")
                            .Permission(DeploymentPermissions.Import)
                            .LocalNav()
                        )
                        .Add(S["JSON Import"], S["JSON Import"].PrefixPosition(), deployment => deployment.Id("json-import")
                            .Action("Json", "Import", "OrchardCore.Deployment")
                            .Permission(DeploymentPermissions.Import)
                            .LocalNav()
                        )
                    )
                );

            return ValueTask.CompletedTask;
        }

        builder
            .Add(S["Tools"], tools => tools.Id("tools")
                .Add(S["Deployments"], S["Deployments"].PrefixPosition(), import => import.Id("deployments")
                    .Add(S["Plans"], S["Plans"].PrefixPosition("1"), deployment => deployment.Id("plans")
                        .Action("Index", "DeploymentPlan", "OrchardCore.Deployment")
                        .Permission(DeploymentPermissions.Export)
                        .LocalNav()
                    )
                    .Add(S["Package Import"], S["Package Import"].PrefixPosition(), deployment => deployment.Id("package-import")
                        .Action("Index", "Import", "OrchardCore.Deployment")
                        .Permission(DeploymentPermissions.Import)
                        .LocalNav()
                    )
                    .Add(S["JSON Import"], S["JSON Import"].PrefixPosition(), deployment => deployment.Id("json-import")
                        .Action("Json", "Import", "OrchardCore.Deployment")
                        .Permission(DeploymentPermissions.Import)
                        .LocalNav()
                    )
                )
            );

        return ValueTask.CompletedTask;
    }
}

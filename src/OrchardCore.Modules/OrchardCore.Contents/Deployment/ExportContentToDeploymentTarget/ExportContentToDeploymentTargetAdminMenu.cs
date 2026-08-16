using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using OrchardCore.Deployment;
using OrchardCore.Navigation;

namespace OrchardCore.Contents.Deployment.ExportContentToDeploymentTarget;

public sealed class ExportContentToDeploymentTargetAdminMenu : AdminNavigationProvider
{
    private static readonly RouteValueDictionary s_routeValues = new()
    {
        { "area", "OrchardCore.Settings" },
        { "groupId", ExportContentToDeploymentTargetSettingsDisplayDriver.GroupId },
    };

    internal readonly IStringLocalizer S;

    public ExportContentToDeploymentTargetAdminMenu(IStringLocalizer<AdminMenu> stringLocalizer)
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
                    .Add(S["Settings"], settings => settings.Id("settings")
                        .Add(S["Export target"], S["Export target"].PrefixPosition(), targetSettings => targetSettings.Id("export-target")
                            .Action("Index", "Admin", s_routeValues)
                            .Permission(DeploymentPermissions.ManageDeploymentPlan)
                            .LocalNav()
                        )
                    )
                )
            );

            return ValueTask.CompletedTask;
        }

        builder
            .Add(S["Settings"], settings => settings.Id("settings")
                .Add(S["Deployment Targets"], S["Deployment Targets"].PrefixPosition(), targetSettings => targetSettings.Id("deployment-targets")
                    .Action("Index", "Admin", s_routeValues)
                    .Permission(DeploymentPermissions.ManageDeploymentPlan)
                    .LocalNav()
                )
            );

        return ValueTask.CompletedTask;
    }
}

using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;

namespace OrchardCore.BackgroundTasks;

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
                    .Add(S["Tasks"], S["Tasks"].PrefixPosition(), tasks => tasks.Id("tasks")
                        .Add(S["Background Tasks"], S["Background Tasks"].PrefixPosition(), backgroundTasks => backgroundTasks.Id("background-tasks")
                            .Action("Index", "BackgroundTask", "OrchardCore.BackgroundTasks")
                            .Permission(Permissions.ManageBackgroundTasks)
                            .LocalNav()
                        )
                    )
                );

            return ValueTask.CompletedTask;
        }

        builder
            .Add(S["Tools"], tools => tools.Id("tools")
                .Add(S["Background Tasks"], S["Background Tasks"].PrefixPosition(), backgroundTasks => backgroundTasks.Id("background-tasks")
                    .Action("Index", "BackgroundTask", "OrchardCore.BackgroundTasks")
                    .Permission(Permissions.ManageBackgroundTasks)
                    .LocalNav()
                )
            );

        return ValueTask.CompletedTask;
    }
}

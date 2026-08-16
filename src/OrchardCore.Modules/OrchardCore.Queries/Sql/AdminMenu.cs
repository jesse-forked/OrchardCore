using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;

namespace OrchardCore.Queries.Sql;

public sealed class AdminMenu : AdminNavigationProvider
{
    internal readonly IStringLocalizer S;

    public AdminMenu(IStringLocalizer<AdminMenu> stringLocalizer)
    {
        S = stringLocalizer;
    }

    protected override ValueTask BuildAsync(NavigationBuilder builder)
    {
        builder
            .Add(S["Search"], NavigationConstants.AdminMenuSearchPosition, search => search.Id("search")
                .Add(S["Queries"], S["Queries"].PrefixPosition(), queries => queries.Id("queries")
                    .Add(S["Run SQL Query"], S["Run SQL Query"].PrefixPosition(), sql => sql.Id("run-sql-query")
                         .Action("Query", "Admin", "OrchardCore.Queries")
                         .Permission(QueriesPermissions.ManageSqlQueries)
                         .LocalNav()
                    )
                )
            );

        return ValueTask.CompletedTask;
    }
}

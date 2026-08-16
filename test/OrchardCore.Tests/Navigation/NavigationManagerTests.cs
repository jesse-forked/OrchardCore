using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OrchardCore.Environment.Shell;
using OrchardCore.Navigation;

namespace OrchardCore.Tests.Navigation;

public class NavigationManagerTests
{
    #region Test factories

    private sealed class FakeNavigationProvider : INavigationProvider
    {
        private readonly Action<NavigationBuilder> _build;

        public FakeNavigationProvider(Action<NavigationBuilder> build) => _build = build;

        public ValueTask BuildNavigationAsync(string name, NavigationBuilder builder)
        {
            _build(builder);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingLogger : ILogger<NavigationManager>
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (logLevel == LogLevel.Error)
            {
                Messages.Add(formatter(state, exception));
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }

    private static NavigationManager CreateManager(
        IEnumerable<INavigationProvider> providers,
        bool requireMenuItemId,
        RecordingLogger logger = null)
    {
        // ActionContext.HttpContext.User is left null in these tests (see CreateActionContext),
        // which makes AuthorizeAsync bypass the authorization service entirely, so a real
        // implementation is unnecessary here.
        return new NavigationManager(
            providers,
            logger ?? new RecordingLogger(),
            new ShellSettings(),
            Mock.Of<IUrlHelperFactory>(),
            Mock.Of<IAuthorizationService>(),
            Options.Create(new NavigationOptions { RequireMenuItemId = requireMenuItemId }));
    }

    private static ActionContext CreateActionContext()
    {
        var httpContext = new DefaultHttpContext
        {
            User = null,
        };

        return new ActionContext
        {
            HttpContext = httpContext,
            RouteData = new RouteData(),
            ActionDescriptor = new ActionDescriptor(),
        };
    }

    private static NavigationBuilder AddItem(NavigationBuilder builder, string caption, string href, string id = null, int priority = 0) =>
        builder.Add(new LocalizedString(caption, caption), position: null, itemBuilder: item =>
        {
            item.Url(href);
            if (id != null)
            {
                item.Id(id);
            }
        }, classes: null, priority: priority);

    #endregion

    // -----------------------------------------------------------------------
    // RequireMenuItemId = false (default)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task BuildMenuAsync_WhenRequireMenuItemIdIsFalse_IncludesItemsWithoutId()
    {
        var provider = new FakeNavigationProvider(builder => AddItem(builder, "Item", "/item"));
        var manager = CreateManager([provider], requireMenuItemId: false);

        var result = await manager.BuildMenuAsync("admin", CreateActionContext());

        Assert.Single(result);
        Assert.Equal("/item", result.First().Href);
    }

    // -----------------------------------------------------------------------
    // RequireMenuItemId = true
    // -----------------------------------------------------------------------

    [Fact]
    public async Task BuildMenuAsync_WhenRequireMenuItemIdIsTrue_ExcludesItemsWithoutId()
    {
        var provider = new FakeNavigationProvider(builder =>
        {
            AddItem(builder, "NoId", "/no-id");
            AddItem(builder, "HasId", "/has-id", id: "test.has-id");
        });
        var manager = CreateManager([provider], requireMenuItemId: true);

        var result = await manager.BuildMenuAsync("admin", CreateActionContext());

        var item = Assert.Single(result);
        Assert.Equal("test.has-id", item.Id);
    }

    [Fact]
    public async Task BuildMenuAsync_WhenRequireMenuItemIdIsTrue_LogsErrorForExcludedItem()
    {
        var provider = new FakeNavigationProvider(builder => AddItem(builder, "NoId", "/no-id"));
        var logger = new RecordingLogger();
        var manager = CreateManager([provider], requireMenuItemId: true, logger);

        await manager.BuildMenuAsync("admin", CreateActionContext());

        Assert.Contains(logger.Messages, m => m.Contains("NoId") && m.Contains("admin"));
    }

    [Fact]
    public async Task BuildMenuAsync_WhenRequireMenuItemIdIsTrue_ExcludesItemsWithoutIdAtAnyDepth()
    {
        var provider = new FakeNavigationProvider(builder =>
        {
            builder.Add(new LocalizedString("Parent", "Parent"), parent =>
            {
                parent.Id("test.parent");
                parent.Url("/parent");
                parent.Add(new LocalizedString("Child", "Child"), child =>
                {
                    child.Url("/parent/child");
                    // Intentionally no Id on the child.
                });
            });
        });
        var manager = CreateManager([provider], requireMenuItemId: true);

        var result = await manager.BuildMenuAsync("admin", CreateActionContext());

        var parent = Assert.Single(result);
        Assert.Equal("test.parent", parent.Id);
        Assert.Empty(parent.Items);
    }

    [Fact]
    public async Task BuildMenuAsync_WhenRequireMenuItemIdIsTrue_MergesByIdEvenWhenCaptionsDiffer()
    {
        // Different captions (e.g. different cultures) but the same Id must still merge, since
        // matching is by Id, not by the translated caption, once RequireMenuItemId is enabled.
        var lowPriorityProvider = new FakeNavigationProvider(builder => AddItem(builder, "Compartido", "/low", id: "test.shared", priority: 0));
        var highPriorityProvider = new FakeNavigationProvider(builder => AddItem(builder, "Shared", "/high", id: "test.shared", priority: 1));
        var manager = CreateManager([lowPriorityProvider, highPriorityProvider], requireMenuItemId: true);

        var result = await manager.BuildMenuAsync("admin", CreateActionContext());

        var item = Assert.Single(result);
        Assert.Equal("test.shared", item.Id);
        Assert.Equal("/high", item.Href);
    }

    [Fact]
    public async Task BuildMenuAsync_WhenRequireMenuItemIdIsTrue_DoesNotMergeItemsWithDifferentIds()
    {
        // Same caption, different Ids: under RequireMenuItemId, matching is by Id, so these are
        // treated as two distinct items rather than merged, even though they share a caption.
        var providerA = new FakeNavigationProvider(builder => AddItem(builder, "Shared", "/a", id: "test.a"));
        var providerB = new FakeNavigationProvider(builder => AddItem(builder, "Shared", "/b", id: "test.b"));
        var manager = CreateManager([providerA, providerB], requireMenuItemId: true);

        var result = await manager.BuildMenuAsync("admin", CreateActionContext());

        Assert.Equal(2, result.Count());
        Assert.Contains(result, i => i.Id == "test.a" && i.Href == "/a");
        Assert.Contains(result, i => i.Id == "test.b" && i.Href == "/b");
    }

    [Fact]
    public async Task BuildMenuAsync_WhenRequireMenuItemIdIsTrue_DoesNotMergeItemsWithoutIds()
    {
        // Same caption, neither item has an Id: under RequireMenuItemId, matching by caption no
        // longer applies, so these stay unmerged and are both excluded for lacking an Id.
        var providerA = new FakeNavigationProvider(builder => AddItem(builder, "Shared", "/a"));
        var providerB = new FakeNavigationProvider(builder => AddItem(builder, "Shared", "/b"));
        var manager = CreateManager([providerA, providerB], requireMenuItemId: true);

        var result = await manager.BuildMenuAsync("admin", CreateActionContext());

        Assert.Empty(result);
    }

    [Fact]
    public async Task BuildMenuAsync_WhenRequireMenuItemIdIsFalse_StillMergesByCaption()
    {
        // Default (current OrchardCore 3.0.1) behavior is preserved: matching is by caption, so
        // items sharing a caption merge even without an Id, and priority/tie-break rules apply
        // exactly as before.
        var lowPriorityProvider = new FakeNavigationProvider(builder => AddItem(builder, "Shared", "/low", priority: 0));
        var highPriorityProvider = new FakeNavigationProvider(builder => AddItem(builder, "Shared", "/high", priority: 1));
        var manager = CreateManager([lowPriorityProvider, highPriorityProvider], requireMenuItemId: false);

        var result = await manager.BuildMenuAsync("admin", CreateActionContext());

        var item = Assert.Single(result);
        Assert.Equal("/high", item.Href);
    }

    [Fact]
    public async Task BuildMenuAsync_WhenRequireMenuItemIdIsTrue_DoesNotThrowOnEmptyMenu()
    {
        var manager = CreateManager([], requireMenuItemId: true);

        var result = await manager.BuildMenuAsync("admin", CreateActionContext());

        Assert.Empty(result);
    }
}

# Stable identity for admin-menu-node menu items

PR body material. Two independent fixes, two branches off `2689c6e12b`:

- `AdminNodeUniqueId` (`df51adfdb6`, pushed) — set `MenuItem.Id` from `AdminNode.UniqueId` in the four builders. **This is the PR to open now.** Commit message is change-only; the reasoning below is the PR description.
- `MergeMenuName` (`e90589069c`, local only) — copy `MenuName` in `NavigationManager.Merge`'s copy blocks. Held back for a later PR.

The `Crest` integration branch carries both changes as the original combined commit `9bc5ffc19f` and is what the local build and the submodule pins in fruitful.orchard / crest.host use.

## The problem (UniqueId PR)

An `AdminNode` carries a `UniqueId`, assigned at creation and persisted with the node — `NodeController` already edits nodes by it. But the admin node navigation builders copy `MenuName`, `Url`, `Target`, `Priority`, `Position`, permissions and icon classes onto the built `MenuItem`, and not `UniqueId`, so the built item has a null `Id` and nothing identifying which node it came from.

That leaves the caption as the only handle. For an `INavigationProvider` item the caption is an `S["..."]` literal — stable across cultures. For an admin menu node, both halves of the `LocalizedString` are the text the admin typed: rename the node and the only thing identifying it changes.

## The change — five one-line insertions

| File | Line added |
|---|---|
| `LinkAdminNodeNavigationBuilder.cs` | `itemBuilder.Id(node.UniqueId);` |
| `PlaceholderAdminNodeNavigationBuilder.cs` | `itemBuilder.Id(node.UniqueId);` |
| `ContentTypesAdminNodeNavigationBuilder.cs` | `itemBuilder.Id($"{node.UniqueId}-{ctd.Name}");` |
| `ListsAdminNodeNavigationBuilder.cs` (parent item) | `listTypeMenu.Id(_node.UniqueId);` |
| `ListsAdminNodeNavigationBuilder.cs` (per content item) | `itemBuilder.Id($"{_node.UniqueId}-{ci.ContentItemId}");` |

`ContentTypesAdminNode` and `ListsAdminNode` expand one node into many items, so their Ids are qualified (type name / content item id) to stay unique per built item while remaining stable when display names change.

Additive: `MenuItem.Id` was previously null for these items, and `Merge` continues to match on `Text.Name` regardless of whether `Id` is set. The shape-alternate machinery already consumes `Id` (`NavigationItemText_Id__{Id}` → `NavigationItemText-{id}.Id.cshtml`), so nodes become targetable by templates the same way provider items already are.

## The held-back companion (MergeMenuName branch)

`NavigationManager.Merge`'s copy blocks copy 13 properties of the authoritative side onto the survivor but omit `MenuName`, so a merged item can end up with the winner's caption and `Id` but the loser's menu affiliation — an accident of provider registration order. Fix is `source.MenuName = cursor.MenuName;` in both copy blocks.

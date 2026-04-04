# Path Convention Splitting

This note explains how `ItemTreePath` and `ImageTreeFile` derive tree folders from `PathConventionType`.

The important rule is that the split is prefix-based.

- `Topdir` is the first `tLen` characters of `ItemId`
- `Subdir` is the first `tLen + sLen` characters of `ItemId`
- `Subdir` is cumulative, not a separate slice after `Topdir`

`ItemTreePath.GetSplit(...)` is the single source of truth for the prefix lengths:

- `PathConventionType.ItemIdTree3x3` -> `(3, 3)`
- `PathConventionType.ItemIdTree8x2` -> `(8, 2)`
- `PathConventionType.CanonicalByName` -> `(ItemId.Length, 0)` when `ItemId` is known

## Core Interpretation

For `ItemId = "1234567890"`:

| Convention | GetSplit | Topdir | Subdir | Resulting folder path |
| --- | --- | --- | --- | --- |
| `ItemIdTree3x3` | `(3, 3)` | `123` | `123456` | `/tmp/root/123/123456/` |
| `ItemIdTree8x2` | `(8, 2)` | `12345678` | `1234567890` | `/tmp/root/12345678/1234567890/` |
| `CanonicalByName` | `(10, 0)` | `1234567890` | `` | `/tmp/root/1234567890/` |

The second folder is not the next fragment.

These expectations are wrong and should fail:

```csharp
Assert.Equal(new RaiPath("/tmp/root/123/456/").ToString(), sut.Path.ToString());
Assert.Equal(new RaiPath("/tmp/root/12345678/90/").ToString(), sut.Path.ToString());
```

These expectations reflect the implemented behavior and should pass:

```csharp
Assert.Equal(new RaiPath("/tmp/root/123/123456/").ToString(), sut.Path.ToString());
Assert.Equal(new RaiPath("/tmp/root/12345678/1234567890/").ToString(), sut.Path.ToString());
```

## Why It Works This Way

The goal is that every folder name mirrors the beginning of the `ItemId`.

That gives two useful properties:

- `Subdir` always starts with `Topdir`
- any folder already tells you which identifier prefix you are inside

This makes the tree easy to inspect manually and easy to reason about during drill-down.

## ItemTreePath

`ItemTreePath` applies the convention directly to its exposed path.

Example:

```csharp
var sut = new ItemTreePath("/tmp/root/", "1234567890", PathConventionType.ItemIdTree3x3);

Assert.Equal("123", sut.Topdir);
Assert.Equal("123456", sut.Subdir);
Assert.Equal(new RaiPath("/tmp/root/123/123456/").ToString(), sut.Path.ToString());
```

So for `ItemTreePath`, `Path` is already the partitioned directory path.

## ImageTreeFile

`ImageTreeFile` uses the same split logic, but applies it to the directory part of the file location.

For a file named `1234567890.webp`:

- `ItemId` is `1234567890`
- `Ext` is `webp`
- `Sku` is just an alias for `ItemId`

With root `/tmp/root/` and `PathConventionType.ItemIdTree8x2`:

- `Topdir` becomes `12345678`
- `Subdir` becomes `1234567890`
- `SubdirRoot` becomes `/tmp/root/12345678/1234567890/`
- `FullName` becomes `/tmp/root/12345678/1234567890/1234567890.webp`

Important distinction:

- `ItemTreePath.Path` returns the split directory path
- `ImageTreeFile.Path` is the unsplit root path
- `ImageTreeFile.SubdirRoot` and `ImageTreeFile.FullName` reflect the split tree

## CanonicalByName

`CanonicalByName` is also prefix-based, but the entire `ItemId` becomes the single folder name.

For `ItemId = "1234567890"`:

- `Topdir` = `1234567890`
- `Subdir` = ``
- resulting folder path = `/tmp/root/1234567890/`

When `GetSplit(PathConventionType.CanonicalByName)` is called without an `itemId`, it returns `(0, 0)` because there is no identifier length to derive.

## Edge Cases

- If `ItemId` is shorter than the requested split length, the available prefix is used.
- Exact directory segment `con` is rewritten to `C0N` to avoid the Windows reserved device name problem.
- Existing embedded tree segments are normalized away before re-applying the convention, so repeated application does not duplicate folders.
# RaiImage

Classes to manage image files in directory trees across local and cloud-backed folders on Windows, macOS, and Linux.

## 3.11.1

- Coordinated patch release: carries forward `WordCase` as the supported replacement for the retired `CamelCase` helper and aligns fallback package references on `OsLibCore 3.11.1` and `RaiUtils 3.11.1`.
- Refreshes the live hierarchy diagram so it no longer advertises the removed `CamelCase` type.
- `ImageFile.EasyFileName(...)` now converts separated and compact trailing digits into `ImageNumber` while keeping pure numeric names as item ids.
- `WordCase` now preserves all-uppercase tokens in PascalCase output so names such as `SD-State-Sony-149` normalize as expected.
- The structured-name flow now stays aligned with the `iorg` CLI when it stages source images into `ImageTreeFile` layouts.
- Keeps the current packaged cloud-provider claim of `OneDrive`, `GoogleDrive`, and `Dropbox`.
- This README is configured to ship inside the RaiImage NuGet package.

## cloud storage compatibility

RaiImage is designed to work with OsLib cloud-root discovery and the current packaged support claim covers:

- Dropbox
- OneDrive
- Google Drive

For cloud-root configuration and environment/setup details, see the OsLib cloud storage discovery guide and keep the same `RAIkeep.json5` cloud-root contract across packages.

## namespace

RaiImage

## classes

<details>
<summary>StringHelper: helper methods for title casing and word splitting.</summary>

- StringHelper: `ToTitle`, `WordSplit`, `CamelSplit`
</details>

<details>
<summary>WordCase: converts between PascalCase, lower camelCase, snake_case, kebab-case, and token arrays.</summary>

- WordCase: `Array`, `String`, `PascalCase`, `LowerCamelCase`, `SnakeCase`, `KebabCase`
- The older `CamelCase` class is retired; use `WordCase` for new and migrated code.
</details>

<details>
<summary>ColorInfo: ImageMagick-compatible color descriptor with optional named-color lookup.</summary>

- ColorInfo: `Get`, `NamedColors`, `Code`, `Name`, `Count`, `Color`
</details>

<details>
<summary>DyeDelta: snapshot of hue/brightness/saturation deltas between two colors.</summary>

- DyeDelta: constructor-based delta capture for `Phi`, `DeltaB`, and `DeltaS`
</details>

<details>
<summary>Dye: color-wheel and brightness/saturation delta calculator.</summary>

- Dye: `Phi`, `DeltaB`, `DeltaSa`, `DeltaSb`, `DeltaS`
</details>

<details>
<summary>Extensions and Size: image-size parser and size value helpers.</summary>

- Extensions: `Parse`
- Size: `ToString`, `nosize`, `noSize`, `HSEmidsize`, `HSEfullsize`
</details>

<details>
<summary>ImageNamingConvention and INamingConvention: file-name convention model for parsing and composition.</summary>

- ImageNamingConvention: `Legacy`, `ItemTemplate`, `Structured`
- INamingConvention: `NamingConvention`, `ApplyNamingConvention`
</details>

<details>
<summary>ImageFile: image filename parser/composer on top of `RaiFile`.</summary>

- ImageFile: `Sku`, `Color`, `ImageNumber`, `NameExt`, `TileTemplate`, `TileNumber`, `NameWithExtension`, `FullName`, `ShortName`
- ImageFile: `ApplyNamingConvention`, `FromFile`, `BlankToCamelCase`, `EasyFileName`, `SetImageNumber`, `ExtendToFirstExistingFile`
</details>

<details>
<summary>ItemTreePath: root path plus tree split convention for item-based directory partitioning.</summary>

- ItemTreePath: `Convention`, `RootPath`, `ItemId`, `Topdir`, `Subdir`, `TopdirRoot`, `SubdirRoot`, `Path`, `FullPath`
- ItemTreePath: `ConventionSplit`, `ApplyPathConvention`, `ToString`
</details>

<details>
<summary>ImageTreeFile: `ImageFile` variant with tree-based path partitioning.</summary>

- ImageTreeFile: `Convention`, `Topdir`, `Subdir`, `TopdirRoot`, `SubdirRoot`
- ImageTreeFile: `ApplyPathConvention`, `mkdir`, `CopyTo`, `MoveToTree`, `rmdir`
- Split behavior is driven by `PathConventionType`; `Subdir` is cumulative, for example `3x3 => 123/123456` and `8x2 => 12345678/1234567890`. See [PATH_CONVENTION_SPLITTING.md](PATH_CONVENTION_SPLITTING.md).
</details>

<details>
<summary>ImageMagickCommand: typed CLI wrapper around ImageMagick subcommands.</summary>

- ImageMagickCommand: `CandidateExecutables`, `BuildArguments`, `RunSubcommand`, `RunSubcommandAsync`
</details>

<details>
<summary>ImageMagick: facade for ImageMagick and related optimization tools.</summary>

- ImageMagick: `ImPath`, `MagickCommand`, `OptiPngCommand`, `JpegTranCommand`, `JpegTranOptions`, `Message`
- ImageMagick: `Convert`, `Mogrify`, `Composite`, `Identify`, `EmptyForm`, `CreateHistogram`, `Histogram`, `OptiPng`, `JpegTran`, `GetSize`, `CreateTiles`
</details>

<details>
<summary>ImageTypes: parsed list of image extensions with a reusable default set.</summary>

- ImageTypes: `Default`, `Array`, `String`
</details>

<details>
<summary>Pane: one `WxH` viewport value with string and `Size` conversions.</summary>

- Pane: `DefaultPane`, `String`, `Size`
</details>

<details>
<summary>Panes: pair-like container for zoom/control pane definitions.</summary>

- Panes: `Count`, `String`, indexer, `ZoomPort`, `ControlPort`
</details>

<details>
<summary>Src: parser for the `src=` parameter used in HDitem-style image URLs.</summary>

- Src: `HasMultipleSkus`, `Skus`, `Sku`, `Subscriber`, `ImageNumber`, `Image`, `ImageWithExtension`, `String`, `Param`
</details>

<details>
<summary>Tmp: parser for the `tmp=` template/overlay parameter.</summary>

- Tmp: `Template`, `Overlays`, `String`, `Param`
</details>

<details>
<summary>IservUrl: URI decomposition into protocol, host, app, page, and path components.</summary>

- IservUrl: `Subscriber`, `Protocol`, `Host`, `Port`, `Path`, `App`, `Page`, `Uri`
</details>

<details>
<summary>ServiceUrl: service-oriented specialization layer on top of `IservUrl`.</summary>

- ServiceUrl: `init(Uri, bool)` and inherited `IservUrl` decomposition members
</details>

<details>
<summary>ImageUrl: HDitem-aware image URL parser with `Src` and `Tmp` extraction.</summary>

- ImageUrl: `Src`, `Tmp`, `Url`, `isHDitemLink`
</details>

<details>
<summary>TwoSizes: comparable pair of small/large sizes with a ranking score.</summary>

- TwoSizes: `Rating`, `SmallRect`, `LargeRect`, `CompareTo`, `Equals`
</details>

## example

```csharp
var count = ImageTreeFile.MoveToTree(
            fromDir: p["from"],
            toDirRoot: p["to"],
            splitMode: PathConventionType.ItemIdTree8x2,
            filter: p["filter"],
            remove: p["remove"]);
Console.WriteLine($"{count} files moved.");
```

## nuget

https://www.nuget.org/packages/RaiImage/

## diagram

- Class hierarchy: [RaiImage-Hierarchy.puml](RaiImage-Hierarchy.puml) | [RaiImage-Type-Overview.svg](RaiImage-Type-Overview.svg)
- Focused class diagram: [RaiImageCD.puml](RaiImageCD.puml) | [RaiImageCD.svg](RaiImageCD.svg)
- Supported operations use cases: [RaiImage-Operations-UseCases.puml](RaiImage-Operations-UseCases.puml) | [RaiImageOperationsUseCases.svg](RaiImageOperationsUseCases.svg)
- Background removal activity: [RaiImage-BackgroundRemoval-Activity.puml](RaiImage-BackgroundRemoval-Activity.puml) | [RaiImageBackgroundRemovalActivity.svg](RaiImageBackgroundRemovalActivity.svg)
- Tiling activity: [RaiImage-Tiling-Activity.puml](RaiImage-Tiling-Activity.puml) | [RaiImageTilingActivity.svg](RaiImageTilingActivity.svg)
- Optimization and recovery activity: [RaiImage-Optimization-Activity.puml](RaiImage-Optimization-Activity.puml) | [RaiImageOptimizationActivity.svg](RaiImageOptimizationActivity.svg)
- CLI render (if PlantUML is installed): `plantuml RaiImage-Hierarchy.puml RaiImageCD.puml RaiImage-Operations-UseCases.puml RaiImage-BackgroundRemoval-Activity.puml RaiImage-Tiling-Activity.puml RaiImage-Optimization-Activity.puml`

## detailed api

- Path-convention splitting note: [PATH_CONVENTION_SPLITTING.md](PATH_CONVENTION_SPLITTING.md)
- Foldable class and method-level documentation: [API.md](API.md)

## migration and release docs

- Migration guide: [MIGRATION_3.2.0.md](MIGRATION_3.2.0.md)
- Architecture alignment: [ARCHITECTURE-ALIGNMENT.md](ARCHITECTURE-ALIGNMENT.md)
- Testing guide: [TESTING.md](TESTING.md)
- Release notes: [RELEASE_NOTES_3.11.1.md](RELEASE_NOTES_3.11.1.md)

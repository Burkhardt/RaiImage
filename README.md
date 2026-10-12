# RaiImage

## 4.5.9

CR060 keeps intermediate numeric tokens in image stems while treating only the final separated number as the image sequence.

Latest release notes: [RaiImage 4.5.9 release notes](https://github.com/Burkhardt/RAIkeep/blob/main/doc/releases/v4.x/RaiImage_RELEASE_NOTES_4.x.md#459).

## Previous Versions

For release notes and changelogs of earlier releases, see the [RaiImage 4.x Release Notes](https://github.com/Burkhardt/RAIkeep/blob/main/doc/releases/v4.x/RaiImage_RELEASE_NOTES_4.x.md).

## Installation

### .NET CLI

```bash
dotnet add package RaiImage --version 4.5.9
```

### PackageReference

```xml
<PackageReference Include="RaiImage" Version="4.5.9" />
```

RaiImage manages image files in directory trees across local and cloud-backed folders on Windows, macOS, and Linux.

## Structured filename compatibility (CR060)

CR060 retains intermediate numeric tokens: `Customer-Order-Sheet-26-10.jpg`
becomes base `CustomerOrderSheet26`, image number 10, and Structured filename
`CustomerOrderSheet26_010.jpg`. A single trailing number remains the sequence
number: `Customer-Order-Sheet-26.jpg` becomes `CustomerOrderSheet_026.jpg`. Separated uppercase words
are PascalCased: `Order_SHEET_001.jpg` becomes `OrderSheet_001.jpg`.

Structured numbers now use D3 minimum width; Legacy keeps D2. Existing two-digit
names still parse, but their recomposed Structured path changes. No stored files
are renamed automatically. Migrate existing paths and references together before
reimporting into an older tree. Values above 999 retain every digit; D3 does not
promise lexicographic ordering across the next width boundary. Numbered diagram siblings and `ItemTreeTextFile` also use D3 so source, manifest,
and SVG names stay aligned. Include these numbered paths in migration.

## cloud storage compatibility

RaiImage is designed to work with OsLib cloud-root discovery and the current packaged support claim covers:

- Dropbox
- OneDrive
- GoogleDrive
- ICloudDrive

For cloud-root configuration and environment/setup details, see the OsLib cloud storage discovery guide and keep the same `RAIkeep.json5` cloud-root contract across packages.

## namespace

RaiImage

## classes

### StringHelper: deprecated binary compatibility facade for pre-4.2.6 static callers.

- Canonical extension methods now live in `RaiUtils.StringHelper`.
- The RaiImage facade methods are intentionally ordinary static methods, preventing duplicate extension discovery.

### WordCase: deprecated binary compatibility facade over `RaiUtils.WordCase`.

- Use `RaiUtils.WordCase` for new and recompiled code.

### ColorInfo: ImageMagick-compatible color descriptor with optional named-color lookup.

- ColorInfo: `Get`, `NamedColors`, `Code`, `Name`, `Count`, `Color`

### DyeDelta: snapshot of hue/brightness/saturation deltas between two colors.

- DyeDelta: constructor-based delta capture for `Phi`, `DeltaB`, and `DeltaS`

### Dye: color-wheel and brightness/saturation delta calculator.

- Dye: `Phi`, `DeltaB`, `DeltaSa`, `DeltaSb`, `DeltaS`

### Extensions and Size: image-size parser and size value helpers.

- Extensions: `Parse`
- Size: `ToString`, `nosize`, `noSize`, `HSEmidsize`, `HSEfullsize`

### ImageNamingConvention and INamingConvention: file-name convention model for parsing and composition.

- ImageNamingConvention: `Legacy`, `ItemTemplate`, `Structured`
- INamingConvention: `NamingConvention`, `ApplyNamingConvention`

### ImageFile: image filename parser/composer on top of `RaiFile`.

- ImageFile: `Sku`, `Color`, `ImageNumber`, `NameExt`, `TileTemplate`, `TileNumber`, `NameWithExtension`, `FullName`, `ShortName`
- ImageFile: `ApplyNamingConvention`, `FromFile`, `BlankToCamelCase`, `EasyFileName`, `SetImageNumber`, `ExtendToFirstExistingFile`

### ItemTreePath: root path plus tree split convention for item-based directory partitioning.

- ItemTreePath: `Convention`, `RootPath`, `ItemId`, `Topdir`, `Subdir`, `TopdirRoot`, `SubdirRoot`, `Path`, `FullPath`
- ItemTreePath: `ConventionSplit`, `ApplyPathConvention`, `SelectFiles`, destination-oriented `mv`, `PruneEmptyDirectories`, `ToString`

### ImageTreeFile: `ImageFile` variant with tree-based path partitioning.

- ImageTreeFile: `Convention`, `Topdir`, `Subdir`, `TopdirRoot`, `SubdirRoot`
- ImageTreeFile: ItemTreePath-aware constructors, `SelectFirstExistingFile`, `ApplyPathConvention`, `mkdir`, `CopyTo`, `MoveToTree`, `rmdir`, `RenderPlantUml`
- Split behavior is driven by `PathConventionType`; `Subdir` is cumulative, for example `3x3 => 123/123456` and `8x2 => 12345678/1234567890`. See [PATH_CONVENTION_SPLITTING.md](https://github.com/Burkhardt/RaiImage/blob/main/PATH_CONVENTION_SPLITTING.md).

### ItemTreeTextFile: truthful text-file placement in an ItemTree bucket.

- ItemTreeTextFile: `SubscriberRoot`, `ItemPath`, `ItemId`, `NameExt`, `Convention`, `SubdirRoot`, `CreateSibling`
- It derives from OsLib `TextFile`, not `ImageFile`, while sharing the same subscriber root, item id, `ItemTreePath`, and `PathConventionType` placement as an `ImageTreeFile`.
- A resolved PlantUML config uses `NameExt = "config"` and `Ext = "puml"`, producing names such as `ScheduleRehearsal_config.puml` beside `.raid`, clean `.puml`, and rendered `.svg` artifacts.

### ImageMagickCommand: typed CLI wrapper around ImageMagick subcommands.

- ImageMagickCommand: `CandidateExecutables`, `BuildArguments`, `RunSubcommand`, `RunSubcommandAsync`
- OptiPngCommand: `BuildArguments`, `Optimize`, `OptimizeAsync`
- JpegTranCommand: `BuildArguments`, `Transform`, `TransformAsync`

### ImageMagick: facade for ImageMagick and related optimization tools.

- ImageMagick: `ImPath`, `MagickCommand`, `OptiPngCommand`, `JpegTranCommand`, `JpegTranOptions`, `Message`; every external image-tool call delegates to its typed wrapper
- ImageMagick: `Convert`, `Mogrify`, `Composite`, `Identify`, `EmptyForm`, `CreateHistogram`, `Histogram`, `OptiPng`, `JpegTran`, `GetSize`, `CreateTiles`

### PlantUmlCommand, PlantUml, and PlantUmlRenderResult: subscriber-aware PlantUML rendering support.

- PlantUmlCommand: `CandidateExecutables`, `BuildSvgArguments`, `RenderSvg`, `RenderSvgAsync`, including optional `-config` injection
- PlantUML `.jar` commands run Java in headless mode for CI, server, and remote-terminal compatibility.
- PlantUml: `PlantUmlPath`, `CommandName`, `JavaCommand`, `Message`, `RenderSvg`
- PlantUmlRenderResult: compatibility `Source`/`Config` image-shaped handles, typed `SourceArtifact`/`ConfigArtifact` text files, and `Svg`; all use the same subscriber `ItemTreePath`

### ImageTypes: parsed list of image extensions with a reusable default set.

- ImageTypes: `Default`, `Array`, `String`

### Pane: one `WxH` viewport value with string and `Size` conversions.

- Pane: `DefaultPane`, `String`, `Size`

### Panes: pair-like container for zoom/control pane definitions.

- Panes: `Count`, `String`, indexer, `ZoomPort`, `ControlPort`

### Src: parser for the `src=` parameter used in HDitem-style image URLs.

- Src: `HasMultipleSkus`, `Skus`, `Sku`, `Subscriber`, `ImageNumber`, `Image`, `ImageWithExtension`, `String`, `Param`

### Tmp: parser for the `tmp=` template/overlay parameter.

- Tmp: `Template`, `Overlays`, `String`, `Param`

### IservUrl: URI decomposition into protocol, host, app, page, and path components.

- IservUrl: `Subscriber`, `Protocol`, `Host`, `Port`, `Path`, `App`, `Page`, `Uri`

### ServiceUrl: service-oriented specialization layer on top of `IservUrl`.

- ServiceUrl: `init(Uri, bool)` and inherited `IservUrl` decomposition members

### ImageUrl: HDitem-aware image URL parser with `Src` and `Tmp` extraction.

- ImageUrl: `Src`, `Tmp`, `Url`, `isHDitemLink`

### TwoSizes: comparable pair of small/large sizes with a ranking score.

- TwoSizes: `Rating`, `SmallRect`, `LargeRect`, `CompareTo`, `Equals`

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

- Path-convention splitting note: [PATH_CONVENTION_SPLITTING.md](https://github.com/Burkhardt/RaiImage/blob/main/PATH_CONVENTION_SPLITTING.md)
- Foldable class and method-level documentation: [API.md](https://github.com/Burkhardt/RaiImage/blob/main/API.md)

## migration and release docs

- Migration guide: [MIGRATION_3.2.0.md](https://github.com/Burkhardt/RaiImage/blob/main/MIGRATION_3.2.0.md)
- Architecture alignment: [ARCHITECTURE-ALIGNMENT.md](https://github.com/Burkhardt/RaiImage/blob/main/ARCHITECTURE-ALIGNMENT.md)
- Testing guide: [TESTING.md](https://github.com/Burkhardt/RaiImage/blob/main/TESTING.md)

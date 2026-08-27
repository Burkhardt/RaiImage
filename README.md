# RaiImage

RaiImage change requests and release notes are centralized in the RAIkeep [`doc/`](https://github.com/Burkhardt/RAIkeep/tree/main/doc) directory under `RaiImage_...` filenames; they are not stored separately in this child repository.

Classes to manage image files in directory trees across local and cloud-backed folders on Windows, macOS, and Linux.

## 4.2.3

- Participates unchanged in the coordinated CR015 package line.
- Implements CR010's truthful `ImageTreeTextFile` placement beside rendered ImageTree images and preserves RaiDiagram's supported PlantUML rendering boundary.
- Adds resolved `_config.puml` persistence using `NameExt = "config"`, typed source/config handles, and PlantUML `-config` invocation.
- Aligns fallback dependencies on `OsLibCore 4.2.3` and `RaiUtils 4.2.3`.
- Adds `RaiImageIOException` and `RaiImageNotFoundException` for image-domain failures.
- Missing lookup directories throw `RaiPathNotFoundException`; missing images in an existing location throw `RaiImageNotFoundException`.
- Missing ImageMagick or PlantUML executables throw `ToolNotFoundException`.
- CR014 routes ImageMagick, PlantUML, OptiPNG, and JPEGTran execution through typed RaiImage wrappers; optimizer paths are passed as discrete process arguments.
- Image files accept `IAsyncEnumerable<byte[]>` ingestion through the OsLib file boundary.
- Refreshes the live hierarchy diagram so it no longer advertises the removed `CamelCase` type.
- `ImageFile.EasyFileName(...)` now converts separated and compact trailing digits into `ImageNumber` while keeping pure numeric names as item ids.
- `WordCase` now preserves all-uppercase tokens in PascalCase output so names such as `SD-State-Sony-149` normalize as expected.
- The structured-name flow now stays aligned with the `iorg` CLI when it stages source images into `ImageTreeFile` layouts.
- Keeps the current packaged cloud-provider claim of `OneDrive`, `GoogleDrive`, `ICloudDrive`, and `Dropbox`.
- This README is configured to ship inside the RaiImage NuGet package.

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

### StringHelper: helper methods for title casing and word splitting.

- StringHelper: `ToTitle`, `WordSplit`, `CamelSplit`

### WordCase: converts between PascalCase, lower camelCase, snake_case, kebab-case, and token arrays.

- WordCase: `Array`, `String`, `PascalCase`, `LowerCamelCase`, `SnakeCase`, `KebabCase`
- The older `CamelCase` class is retired; use `WordCase` for new and migrated code.

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
- ItemTreePath: `ConventionSplit`, `ApplyPathConvention`, `ToString`

### ImageTreeFile: `ImageFile` variant with tree-based path partitioning.

- ImageTreeFile: `Convention`, `Topdir`, `Subdir`, `TopdirRoot`, `SubdirRoot`
- ImageTreeFile: `ApplyPathConvention`, `mkdir`, `CopyTo`, `MoveToTree`, `rmdir`, `RenderPlantUml`
- Split behavior is driven by `PathConventionType`; `Subdir` is cumulative, for example `3x3 => 123/123456` and `8x2 => 12345678/1234567890`. See [PATH_CONVENTION_SPLITTING.md](https://github.com/Burkhardt/RaiImage/blob/main/PATH_CONVENTION_SPLITTING.md).

### ImageTreeTextFile: truthful text-file placement in an ImageTree item bucket.

- ImageTreeTextFile: `SubscriberRoot`, `ItemPath`, `ItemId`, `NameExt`, `Convention`, `SubdirRoot`, `CreateSibling`
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
- Release notes: [RaiImage_RELEASE_NOTES_4.2.3.md](https://github.com/Burkhardt/RAIkeep/blob/main/doc/RaiImage_RELEASE_NOTES_4.2.3.md)

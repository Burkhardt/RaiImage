# RaiImage

Classes to manage image files in directory trees 
across local and cloud-backed folders 
on Windows, macOS, and Linux.

## 3.6.1

- Patch: aligns fallback package references with `OsLibCore 3.6.1` and `RaiUtils 3.6.1` in the correct NuGet publish order.

## cloud storage compatibility

RaiImage is designed to work well with OsLib cloud-root discovery and the current packaged support claim covers:

- Dropbox
- OneDrive
- Google Drive

For cloud-root configuration and environment/setup details, see:

- [OsLib cloud storage discovery guide](https://github.com/Burkhardt/OsLib/blob/main/CLOUD_STORAGE_DISCOVERY.md)

## namespace

RaiImage

## classes

<details>
<summary>StringHelper: helper methods for title casing and camel-case splitting.</summary>

- StringHelper: `ToTitle`, `CamelSplit`
</details>

<details>
<summary>CamelCase: converts between camel/pascal strings and token arrays.</summary>

- CamelCase: `Array`, `String`
</details>

<details>
<summary>ColorInfo: ImageMagick-compatible color model with optional name lookup.</summary>

- ColorInfo: `Get`, `Code`, `Name`, `Count`, `NamedColors`
</details>

<details>
<summary>Dye and DyeDelta: color delta calculations for hue, brightness, and saturation.</summary>

- Dye: `Phi`, `DeltaB`, `DeltaS`
- DyeDelta
</details>

<details>
<summary>Size and Extensions: image size value and parser helpers.</summary>

- Size: `Width`, `Height`, `HSEmidsize`, `HSEfullsize`
- Extensions: `Parse`
</details>

<details>
<summary>ImageFile: parse/compose image names and manage image file identity.</summary>

- ImageFile: `Sku`, `Color`, `ImageNumber`, `NameExt`, `TileTemplate`, `TileNumber`, `FromFile`, `ExtendToFirstExistingFile`, `EasyFileName`
</details>

<details>
<summary>ImageTreeFile: ImageFile variant with tree-based path partitioning.</summary>

- ImageTreeFile: `Topdir`, `Subdir`, `TopdirRoot`, `SubdirRoot`, `MoveToTree`, `CopyTo`
</details>

<details>
<summary>ImageMagick: wrapper around ImageMagick and selected optimization tools.</summary>

- ImageMagick: `Convert`, `Mogrify`, `Composite`, `Identify`, `CreateTiles`, `GetSize`, `CreateHistogram`, `OptiPng`, `JpegTran`
</details>

<details>
<summary>ImageTypes, Pane, Panes: typed models for extensions and viewport dimensions.</summary>

- ImageTypes: `Array`, `String`
- Pane: `Size`
- Panes: `ZoomPort`, `ControlPort`
</details>

<details>
<summary>Src, Tmp: query parameter models for image source and template selection.</summary>

- Src: `Sku`, `Subscriber`, `ImageNumber`, `Param`
- Tmp: `Template`, `Overlays`, `Param`
</details>

<details>
<summary>IservUrl, ServiceUrl, ImageUrl: URL decomposition and image-link semantics.</summary>

- IservUrl: `Protocol`, `Host`, `Port`, `Path`, `App`, `Page`
- ServiceUrl
- ImageUrl: `Src`, `Tmp`, `isHDitemLink`
</details>

<details>
<summary>TwoSizes: comparable pair of small/large sizes with rating.</summary>

- TwoSizes: `Rating`, `SmallRect`, `LargeRect`, `CompareTo`
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

- Class hierarchy: [RaiImage-Hierarchy.puml](RaiImage-Hierarchy.puml) | [RaiImage-Hierarchy.svg](RaiImage-Hierarchy.svg)
- Focused class diagram: [RaiImageCD.puml](RaiImageCD.puml) | [RaiImageCD.svg](RaiImageCD.svg)
- Supported operations use cases: [RaiImage-Operations-UseCases.puml](RaiImage-Operations-UseCases.puml) | [RaiImage-Operations-UseCases.svg](RaiImage-Operations-UseCases.svg)
- Background removal activity: [RaiImage-BackgroundRemoval-Activity.puml](RaiImage-BackgroundRemoval-Activity.puml) | [RaiImage-BackgroundRemoval-Activity.svg](RaiImage-BackgroundRemoval-Activity.svg)
- Tiling activity: [RaiImage-Tiling-Activity.puml](RaiImage-Tiling-Activity.puml) | [RaiImage-Tiling-Activity.svg](RaiImage-Tiling-Activity.svg)
- Optimization and recovery activity: [RaiImage-Optimization-Activity.puml](RaiImage-Optimization-Activity.puml) | [RaiImage-Optimization-Activity.svg](RaiImage-Optimization-Activity.svg)
- CLI render (if PlantUML is installed): `plantuml RaiImage-Hierarchy.puml RaiImageCD.puml RaiImage-Operations-UseCases.puml RaiImage-BackgroundRemoval-Activity.puml RaiImage-Tiling-Activity.puml RaiImage-Optimization-Activity.puml`
- VS Code: open the `.puml` file and use a PlantUML preview/render extension.

## detailed api

- Foldable class and method-level documentation: [API.md](API.md)

## migration and release docs

- Migration guide: [MIGRATION_3.2.0.md](MIGRATION_3.2.0.md)
- Testing guide: [TESTING.md](TESTING.md)
- Release notes: [RELEASE_NOTES_3.5.2.md](RELEASE_NOTES_3.5.2.md)

# RaiImage

	Classes to manage image files in directory trees (within dropbox or outside, windows or macOS or linux).

## 2.1.4

- Provides image-focused file models (`ImageFile`, `ImageTreeFile`) with naming and tree-path conventions.
- Includes URL/query helper classes (`Src`, `Tmp`, `ImageUrl`) used in HDitem-style scenarios.
- Adds ImageMagick wrapper support and utility classes for color, pane sizing, and naming transformations.

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

- Source options: [RaiImage-Hierarchy.puml](RaiImage-Hierarchy.puml) | [RaiImage-Hierarchy.svg](RaiImage-Hierarchy.svg)
- CLI render (if PlantUML is installed): `plantuml RaiImage-Hierarchy.puml`
- VS Code: open the `.puml` file and use a PlantUML preview/render extension.

## detailed api

- Foldable class and method-level documentation: [API.md](API.md)

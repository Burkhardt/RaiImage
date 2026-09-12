# RaiImage API Reference

This document provides a detailed, foldable API overview.

## 4.3.0 scope note

- CR022 changes `ImageMagick.JpegTran` so an established image pathname remains continuously present.
- The typed `JpegTranCommand` receives isolated local `TmpFile` input/output paths; validated result bytes are copied through OsLibCore's in-place overwrite boundary.
- Failure before result application leaves the original image unchanged, and rollback never deletes then moves a temporary file into the original pathname.
- Fallback package references align to `OsLibCore 4.3.0` and `RaiUtils 4.3.0`.
- CR025 adds numbered, archetype-aware `ItemTreeTextFile` naming and the public subscriber artifact rendering boundary used by RaiDiagram.

## Carried-forward API

- RaiImage implements accepted CR020's shared item-file architecture.
- `ItemTreePath` owns exact-ItemId file selection and aggregate destination-oriented movement across image and diagram extensions.
- `PathConventionType.Flat` joins the existing canonical, 3x3, and 8x2 layouts without changing earlier enum values.
- `ItemTreeTextFile` replaces the contradictory `ImageTreeTextFile` name.
- `ImageTreeFile` and diagram artifact construction now accept `ItemTreePath`; procedural `FromName`, `FromImageTree`, `FromItemTree`, and `FromExternalLink` factories are removed.
- `SelectFirstExistingFile(...)` provides fluent source-image selection while retaining domain-specific missing-path and missing-image failures.

- RaiImage implements accepted CR019's package placement by consuming canonical word-case behavior from RaiUtils.
- `RaiImage.WordCase` and `RaiImage.StringHelper` remain deprecated binary compatibility facades; no independent word-case implementation remains in RaiImage.
- Recompiled extension-method callers import `RaiUtils` for `WordSplit`, `CamelSplit`, `ToTitle`, and Unicode-safe `WordSeams`.
- The CR020/CR019/CR016 public API remains available on the coordinated v4.3.0 dependency line.
- ImageTree-owned logical names are canonicalized to Unicode NFC before bucket or filename derivation; caller-provided root paths are preserved.
- `ItemTreePath` and `ImageTreeFile` calculate 3x3, 8x2, and canonical-name prefixes by Unicode text elements rather than UTF-16 code units.
- `SelectFirstExistingFile(...)` and `ExtendToFirstExistingFile(...)` resolve legacy NFC, NFD, and mixed-normalization directory/file spellings by canonical equivalence through `RaiPath` and `RaiFile` enumeration.
- Ambiguous canonical-equivalent directories or source files fail with `RaiImageIOException`.
- SVG is included in `DefaultSourceExtensions`.
- `RaiImageIOException` and `RaiImageNotFoundException` provide image-domain failures; missing paths remain `RaiPathNotFoundException`, and missing external tools remain `ToolNotFoundException`.
- `WriteFromAsync(IAsyncEnumerable<byte[]>, CancellationToken)` provides stream-free asynchronous image ingestion.

## exception and ingestion boundaries

- <details>
	<summary>RaiImage exception hierarchy</summary>

	- `RaiImageIOException` is the RaiUtils-backed base for image-specific read, write, conversion, or rendering failures.
	- `RaiImageNotFoundException` inherits from `RaiImageIOException` and means an image could not be resolved inside an otherwise valid location.
	- Missing paths and tools retain the cross-package `RaiPathNotFoundException` and `ToolNotFoundException` distinctions.
	</details>
- <details>
	<summary>WriteFromAsync(chunks, cancellationToken)</summary>

	- Accepts `IAsyncEnumerable&lt;byte[]&gt;` and writes through the RaiFile boundary without requiring consumers to exchange raw streams.
	- Honors cancellation and preserves the destination image object's established path semantics.
	</details>

## naming and parsing helpers

- <details>
	<summary>ItemTreePath / ImageTreeFile Unicode path contract</summary>

	- Subscriber names, item ids, name extensions, route values, and typed ImageTree text artifacts use Unicode Normalization Form C.
	- Bucket widths count user-perceived Unicode text elements, keeping combining sequences and surrogate-pair/emoji clusters intact.
	- Existing decomposed or mixed-normalization trees remain readable without renaming or mutating them during lookup.
	- Newly authored ImageTree names use the canonical NFC spelling supplied to the filesystem.
	</details>

- <details>
	<summary>StringHelper: deprecated static binary compatibility facade.</summary>

	- <details>
		<summary>ToTitle(value), WordSplit(value), and CamelSplit(value).</summary>

		- Preserve pre-4.2.6 static CLR call signatures by delegating to `RaiUtils.StringHelper`.
		- They are intentionally ordinary static methods rather than extension methods, avoiding ambiguity with the canonical RaiUtils extensions.
		</details>
	</details>

- <details>
	<summary>WordCase: deprecated binary compatibility facade over `RaiUtils.WordCase`.</summary>

	- <details>
		<summary>Array / String / case properties: synchronized token and formatted forms.</summary>

		- Inherits the canonical RaiUtils implementation so existing compiled constructor and member calls continue to resolve.
		- New and recompiled code should use `RaiUtils.WordCase` directly.
		</details>
	</details>

## color and dye model

- <details>
	<summary>ColorInfo: ImageMagick-compatible color descriptor with optional name lookup.</summary>

	- <details>
		<summary>Get(nameOrHexCode): resolve color by name or hex code.</summary>

		- Uses a tab-separated color names file configured via `ColorNamesFile`.
		</details>
	- <details>
		<summary>Code / Name / Count / Color: color identity and frequency fields.</summary>

		- Supports color code handling with `#` prefix and maps to `System.Drawing.Color`.
		</details>
	- <details>
		<summary>NamedColors: exposes loaded name-to-code mapping.</summary>

		- Lazily initializes and caches dictionary data.
		</details>
	</details>

- <details>
	<summary>Dye and DyeDelta: color-wheel and brightness/saturation deltas.</summary>

	- <details>
		<summary>Dye.Phi/DeltaB/DeltaS: ImageMagick-style transform metrics.</summary>

		- Produces percent-style values used for color adjustment operations.
		</details>
	- <details>
		<summary>DyeDelta: snapshot of delta values between two dyes.</summary>

		- Computes `Phi`, `DeltaB`, and `DeltaS` at construction time.
		</details>
	</details>

## image identity and storage

- <details>
	<summary>ItemTreeTextFile and diagram artifact naming.</summary>

	- `ItemId` remains the base domain identity used for cumulative ItemTree bucketing.
	- `ItemNumber` is optional and symmetric with `ImageTreeFile.ImageNumber`; `NoItemNumber` omits it.
	- `NameExt` carries the diagram archetype, such as `UCD`, rather than becoming part of `ItemId`.
	- Filenames compose as `ItemId[_NN][_NameExt].ext`, for example `SignContract_UCD.puml` and `SignContract_02_UCD.raid`.
	- `CreateSibling(...)` preserves `ItemId`, `ItemNumber`, `NameExt`, subscriber, and bucket path while changing only the requested extension.
	</details>

- <details>
	<summary>ImageRendering.RenderPlantUmlArtifactAtSubscriber(...).</summary>

	- Renders or saves co-located `.puml`, `_config.puml`, and `.svg` artifacts under one subscriber ItemTree path.
	- Overloads accept the base `ItemId`, optional `ItemNumber`, and archetype `NameExt` independently.
	- The server-side PlantUML path remains an optional compatibility boundary; manifest building and PUML compilation do not require Java.
	</details>

- <details>
	<summary>Size and Extensions.Parse: image size value helpers.</summary>

	- <details>
		<summary>Size: width/height model with string formatting and predefined dimensions.</summary>

		- Includes `noSize`, `HSEmidsize`, and `HSEfullsize` helper values.
		</details>
	- <details>
		<summary>Extensions.Parse(value): parse "WxH" into `Size`.</summary>

		- Returns `Size.noSize` if parsing fails.
		</details>
	</details>

- <details>
	<summary>ImageFile: image filename parser/composer on top of `RaiFile`.</summary>

	- <details>
		<summary>Sku / Color / ImageNumber / NameExt / TileTemplate / TileNumber.</summary>

		- Represents structured naming segments encoded in image file names.
		</details>
	- <details>
		<summary>Name / NameWithExtension / FullName / ShortName: derived naming outputs.</summary>

		- Composes canonical name variants from parsed fields.
		</details>
	- <details>
		<summary>FromFile(clone): load image safely from disk via stream.</summary>

		- Avoids long-lived file handles and optionally clones the image payload.
		</details>
	- <details>
		<summary>ExtendToFirstExistingFile(extensions, colorInfo): resolve existing image variant.</summary>

		- Searches the file system for the first matching extension/color combination.
		</details>
	- <details>
		<summary>EasyFileName(pic, renameFile): normalize input names to predictable image naming.</summary>

		- Pads short identifiers, sets defaults, and can optionally rename on disk.
		</details>
	</details>

	- <details>
		<summary>ImageTreeFile: `ImageFile` with tree-based directory partitioning.</summary>

	- <details>
		<summary>Topdir / Subdir / TopdirRoot / SubdirRoot: partition path components.</summary>

		- Derives directory segments from `ItemId` or `Sku` via `PathConventionType`; `Subdir` is cumulative, not a separate slice (`3x3 => 123/123456`, `8x2 => 12345678/1234567890`).
		- Examples and rationale: [PATH_CONVENTION_SPLITTING.md](https://github.com/Burkhardt/RaiImage/blob/main/PATH_CONVENTION_SPLITTING.md).
		</details>
	- <details>
		<summary>Path and Sku overrides: keep path and partition segments synchronized.</summary>

		- Prevents duplicated tree segments when path or sku values change.
		</details>
	- <details>
		<summary>MoveToTree(...): move flat files into tree structure.</summary>

		- Builds destination tree from file names and moves files into partitioned folders.
		</details>
	- <details>
		<summary>ImageTreeFile(rootPath, name, ...): parse a rooted short name without needing a source file extension.</summary>

		- Supports route values such as `AfricanPicnic_04`, `AfricanPicnic_04_Small`, `GageElementary`, and `GageElementary_Huge`.
		- Can auto-infer `ImageNamingConvention` from the supplied name or accept it explicitly.
		</details>
	- <details>
		<summary>ImageTreeFile(ItemTreePath, nameExt, ext, naming): construct within an existing item home.</summary>

		- Keeps image objects on the same subscriber, ItemId, and path convention as related diagram artifacts.
		- `SelectFirstExistingFile(...)` mutates and returns the same object after selecting the first matching source extension; missing images throw `RaiImageNotFoundException`.
		</details>
	- <details>
		<summary>CopyTo(destDirs), mkdir(), rmdir(): tree-aware file/folder operations.</summary>

		- Supports multi-target copy and depth-based tree cleanup.
		</details>
	- <details>
		<summary>CreateSiblingWithExtension(ext): create another ImageTree handle in the same item placement.</summary>

		- Preserves the item, naming convention, and tree path while changing only the artifact extension.
		</details>
	- <details>
		<summary>InferSourceNamingConvention(itemId): expose RaiImage's own naming inference for callers that need to stay in lockstep.</summary>

		- Returns `Structured` when the name carries a numeric image-number segment; otherwise returns `Legacy`.
		</details>
	- <details>
		<summary>RenderPlantUml(...): persist PlantUML source and render sibling SVG inside the subscriber tree.</summary>

		- Writes the source as `.puml`, optionally persists a sibling `_config.puml` using `NameExt = "config"` and `Ext = "puml"`, invokes the local PlantUML CLI with `-config`, and keeps all artifacts in one subscriber `ItemTreePath`.
		</details>
	</details>

- <details>
	<summary>ItemTreePath: one subscriber-local ItemId tree home.</summary>

	- `SelectFiles()` returns every physical `RaiFile` owned by the exact ItemId, including numbered images, derivatives, SVG, PUML, config PUML, and RAID files.
	- Similar ItemIds sharing the same bucket are excluded by the filename ownership rule.
	- `destination.mv(source)` moves the complete selected family, optionally changing subscriber, ItemId, and path convention while preserving suffixes and extensions.
	- The move preflights collisions, attempts rollback after a partial failure, and prunes empty source buckets.
	- `Flat` maps the item home directly to the subscriber root; the same move operation migrates between Flat, 3x3, and 8x2.
	</details>

- <details>
	<summary>ItemTreeTextFile: text content placed by the existing ImageTree contract.</summary>

	- Derives from OsLib `TextFile`, not `ImageFile`, and carries `ItemPath`, `SubscriberRoot`, `ItemId`, `NameExt`, `Convention`, and `SubdirRoot`.
	- `CreateSibling(nameExt, ext)` retains the subscriber, item id, convention, and item bucket while producing a truthful text artifact type.
	- A config artifact uses `NameExt = "config"` and `Ext = "puml"`, producing `_config.puml` rather than a compound extension.
	</details>

## imaging operations

- <details>
	<summary>ImageMagick: wrapper around ImageMagick and related optimization tools.</summary>

	- <details>
		<summary>Convert / Mogrify / Composite / Identify: command wrappers.</summary>

		- Executes external tools and captures exit code/output message.
		</details>
	- <details>
		<summary>GetSize(imageFile): read dimensions through `identify`.</summary>

		- Returns image dimensions and validates external command output.
		</details>
	- <details>
		<summary>CreateTiles(...): produce tiled image sets for deep zoom use cases.</summary>

		- Generates tile pyramids and metadata files from source images.
		</details>
	- <details>
		<summary>CreateHistogram / Histogram / OptiPng / JpegTran: optimization helpers.</summary>

		- Includes histogram generation and format-specific optimization pipelines.
		- ImageMagick subcommands delegate to `ImageMagickCommand`; PNG and JPEG optimization delegate to `OptiPngCommand` and `JpegTranCommand`.
		- `JpegTran` never moves the live image into `Os.TempDir`; tool output is validated locally and its bytes overwrite the continuously present destination through `RaiFile.cp`.
		</details>
	</details>

- <details>
	<summary>ImageMagickCommand, OptiPngCommand, and JpegTranCommand: typed image-tool boundaries.</summary>

	- `ImageMagickCommand` supports string-compatible and tokenized subcommand arguments through `RunSubcommand` and `RunSubcommandAsync`.
	- `OptiPngCommand.BuildArguments(image)` preserves the complete `RaiFile` path as one process token; `Optimize` and `OptimizeAsync` return `RaiSystemResult`.
	- `JpegTranCommand.BuildArguments(options, source, destination)` preserves every option and file path as a separate token; `Transform` and `TransformAsync` return `RaiSystemResult`.
	- The compatibility `ImageMagick` facade no longer constructs individual `RaiSystem` calls for these tools.
	</details>

- <details>
	<summary>PlantUmlCommand, PlantUml, and PlantUmlRenderResult: PlantUML CLI integration.</summary>

	- <details>
		<summary>PlantUmlCommand: typed CLI wrapper for local binary or jar execution.</summary>

		- Supports direct `plantuml` binaries and headless `.jar` execution through `java -Djava.awt.headless=true -jar`.
		- `RenderSvg(...)` invokes PlantUML with `-tsvg` against a staged `.puml` file and accepts an optional resolved config file through `-config`.
		</details>
	- <details>
		<summary>PlantUml: lightweight facade that mirrors RaiImage's existing external-tool flow.</summary>

		- Exposes `PlantUmlPath`, `CommandName`, `JavaCommand`, `Message`, and `RenderSvg(...)`.
		</details>
	- <details>
		<summary>PlantUmlRenderResult: co-located artifact handles for source, optional config, and rendered output.</summary>

		- Carries typed `.puml` source/config text artifacts, compatibility `ImageTreeFile` handles, and the generated `.svg` in one subscriber item bucket.
		</details>
	</details>

## url and viewer parameter types

- <details>
	<summary>ImageTypes, Pane, Panes: viewer/image type parameter models.</summary>

	- <details>
		<summary>ImageTypes: parse and format extension lists.</summary>

		- Stores extension arrays and comma-separated representations.
		</details>
	- <details>
		<summary>Pane / Panes: viewport dimensions and dual-pane composition.</summary>

		- Supports parsing and formatting of `WxH` viewport definitions.
		</details>
	</details>

- <details>
	<summary>Src and Tmp: HDitem-style query parameter models.</summary>

	- <details>
		<summary>Src: parse source image path, subscriber, sku, and image number details.</summary>

		- Handles single/multiple sku cases and provides `src=` serialization helper.
		</details>
	- <details>
		<summary>Tmp: parse template and overlays from combined template value.</summary>

		- Splits camel segments into base template plus overlay list.
		</details>
	</details>

- <details>
	<summary>IservUrl, ServiceUrl, ImageUrl: URL decomposition and HDitem link semantics.</summary>

	- <details>
		<summary>IservUrl: scheme/host/port/path/app/page decomposition.</summary>

		- Wraps `UriBuilder` and exposes path/application/page convenience properties.
		</details>
	- <details>
		<summary>ImageUrl: query extraction into `Src` and `Tmp` models.</summary>

		- Detects HDitem links and provides normalized access to image request inputs.
		</details>
	- <details>
		<summary>ServiceUrl: service-url specialization layer.</summary>

		- Extends `IservUrl` for service-specific usage points.
		</details>
	</details>

## selection/ranking

- <details>
	<summary>TwoSizes: pair of candidate sizes with comparability support.</summary>

	- <details>
		<summary>Rating / SmallRect / LargeRect and `IComparable` behavior.</summary>

		- Supports ranking and equality checks for two-size candidates.
		</details>
	</details>

# RaiImage API Reference

This document provides a detailed, foldable API overview.

## 4.2.2 scope note

- RaiImage publishes a coordinated `4.2.2` release with fallback package references aligned to `OsLibCore 4.2.2` and `RaiUtils 4.2.2`.
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
	<summary>StringHelper: convenience methods for title/camel handling.</summary>

	- <details>
		<summary>ToTitle(value): normalize word casing with first letter uppercase.</summary>

		- Converts `abc` to `Abc` and lowercases the remaining characters.
		</details>
	- <details>
		<summary>WordSplit(value): split mixed case/separator tokens.</summary>

		- Uses `WordCase` tokenization rules and returns a token array.
		- `CamelSplit(value)` remains as a compatibility alias.
		</details>
	</details>

- <details>
	<summary>WordCase: bidirectional word-case representation.</summary>

	- <details>
		<summary>Array / String / case properties: synchronized token and formatted forms.</summary>

		- `Array` stores parsed word tokens; `String` returns `PascalCase` for legacy callers.
		- Use `PascalCase`, `LowerCamelCase`, `SnakeCase`, or `KebabCase` for explicit output.
		- The old `CamelCase` class is retired; use `WordCase` instead.
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
		<summary>FromName(rootPath, name, ...): parse a rooted short name without needing a source file extension.</summary>

		- Supports route values such as `AfricanPicnic_04`, `AfricanPicnic_04_Small`, `GageElementary`, and `GageElementary_Huge`.
		- Can auto-infer `ImageNamingConvention` from the supplied name or accept it explicitly.
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
	<summary>ImageTreeTextFile: text content placed by the existing ImageTree contract.</summary>

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

# RaiImage API Reference

This document provides a detailed, foldable API overview.

## 3.7.7 scope note

- RaiImage aligns with the `3.7.7` `RAIkeep` package line.
- Patch release: fallback package references updated to `OsLibCore 3.7.7` and `RaiUtils 3.7.7`.

## naming and parsing helpers

- <details>
	<summary>StringHelper: convenience methods for title/camel handling.</summary>

	- <details>
		<summary>ToTitle(value): normalize word casing with first letter uppercase.</summary>

		- Converts `abc` to `Abc` and lowercases the remaining characters.
		</details>
	- <details>
		<summary>CamelSplit(value): split camel/pascal case tokens.</summary>

		- Uses `CamelCase` tokenization rules and returns a token array.
		</details>
	</details>

- <details>
	<summary>CamelCase: bidirectional camel/pascal representation.</summary>

	- <details>
		<summary>Array / String: synchronized token and joined forms.</summary>

		- `Array` lazily tokenizes `String`; setting either side refreshes the other view.
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
		- Examples and rationale: [PATH_CONVENTION_SPLITTING.md](PATH_CONVENTION_SPLITTING.md).
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
		<summary>CopyTo(destDirs), mkdir(), rmdir(): tree-aware file/folder operations.</summary>

		- Supports multi-target copy and depth-based tree cleanup.
		</details>
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

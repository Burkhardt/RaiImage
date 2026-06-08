# HDitem 2017 ImageServer URL Format and ImageTreeFile Format

This document consolidates the implemented request formats and file-name/path grammar from code and in-repo docs.

## 1. ImageServer URL Format

General shape:

- `[protocol]://[host]/[app]/[page].aspx?[query]`
- Typical app values: `iserv`, `picture`

Core URL parsing helpers are in:

- `HDitem.Image.Base/ImageSizes.cs` (`IservUrl`, `Src`, `Tmp`, `ImageUrl`)

## 2. Shared Query Components

### 2.1 `src`

Common pattern:

- `src=<subscriber>/<image-or-sku>`

Examples:

- `src=pic/300004_01`
- `src=pic/300004`
- `src=retpic/104212,355381` (multi-SKU use case)

Behavior from `Src`:

- Splits subscriber from payload at first `/`
- Provides `Sku`, `Image`, `ImageNumber`
- Supports multiple SKUs via spaces, `%20`, or extra `/` segments (`HasMultipleSkus`, `Skus`)

### 2.2 `tmp`

`tmp` can represent a base template plus overlays in lower camel/Pascal word case.

Examples:

- `tmp=Gallery`
- `tmp=HugenewSoldoutl`
- `tmp=jsonp`
- `tmp=fsi`

`Tmp` class behavior:

- First word-case token -> `Template`
- Remaining tokens -> `Overlays`

### 2.3 Output format switches

Depending on endpoint:

- `tmp=XML`, `tmp=JSON`, `tmp=JSONP`, `tmp=HTML`
- Some endpoints also inspect `Request.ContentType`

### 2.4 JSONP helpers

Common optional params:

- `callback=<functionName>`
- `catch=<catchBody>`

Used to wrap callback execution in client-side try/catch for JSONP responses.

---

## 3. Endpoint-Specific Formats

## 3.1 `HDitem.aspx`

Primary image rendering endpoint.

Required in template mode:

- `src`
- `tmp`

Typical:

- `/iserv/HDitem.aspx?src=pic/300004_01&tmp=Gallery`

Special handling:

- If `tmp` is `fsi` or `jsonp` and `cmd=view`, request is forwarded to `DisplayXml.aspx`
- If `tmp` is `fsi` or `jsonp` without `cmd`, rectangle/tile mode is used (`5x5tile`)

Template override params accepted by `ItemRequest.Override(...)`:

- `adaptiveResize`
- `customFirst`
- `customLast`
- `density`
- `ext`
- `height`
- `quality`
- `strip`
- `unsharp`
- `width`
- `compress`

Additional params consumed in image operations and unique-id generation include:

- `text`, `color`, `font`, `size`, `scale`, `angle`, `colorize`, `mask`
- Crop/placement fields in item processing: `left`, `top`, `right`, `bottom`

## 3.2 `Sizes.aspx`

Returns zoom tree(s) for one image or all images of an SKU.

Parameters:

- `src=<subscriber>/<sku_or_image>`
- `opt=<uni|tryuni|flex>` (default resolves to flex behavior)
- `ViewerPane=<WxH>` or `ViewerPane=<WxH>,<WxH>`
- `tmp=<XML|JSON|JSONP|HTML>` (code path effectively JSON* vs XML)
- `callback` (optional)
- `catch` (optional)

Notable behavior:

- `ViewerPane` defaults to `0x0` if missing
- If `src` has no image number, endpoint returns trees for all images of SKU
- JSON paths serialize `ZoomTreeV32`; XML paths serialize `ZoomTreeData`

## 3.3 `ViewerInfo.aspx`

Combines control zoom tree, show trees, and image names for viewer bootstrap.

Parameters:

- `src=<subscriber>/<image>` (can include comma-separated additional SKUs)
- `opt=<uni|tryuni|flex>`
- `ViewerPane=<zoomWxH>,<controlWxH>`
- `callback` (mandatory in validation)
- `catch` (optional)
- `tmp` (`html` handled specially, else jsonp)
- `beautify` (optional bool)

Validation rules in code require:

- valid subscriber and image
- image number >= 1
- at least two panes
- non-empty callback

## 3.4 `Count.aspx`

Returns image count or image name list for a SKU/search pattern.

Parameters:

- `src=<subscriber>/<skuOrSearchPattern>`
- `tmp=<HTML|XML|JSON|JSONP>`
- `list=<true|false>`
- `skip=<n>` (default `0`)
- `take=<n>` (default `100`, capped at `1000`)
- `refresh=<true|false>` (default `false`)
- `callback` (optional)
- `catch` (optional)

Notable behavior:

- `src` search segment can include wildcard `*`
- content type can influence JSON/XML branching

## 3.5 `Jzoom.aspx`

Returns JavaScript viewer bootstrap code.

Parameters:

- `src` (required)
- `tmp` (required)
- `div` (required)
- `var` (required)
- `set` (optional, defaults to `HDitemZoom`)

---

## 4. Complete ImageTreeFile Format

Implemented by:

- `HDitem.Image.Base/ImageFile.cs` (`ImageFile`, `ImageTreeFile`)

Tests:

- `ImageServerTest/ImageTreeFileTest.cs`

## 4.1 Canonical logical structure

`FullName = Path + Name + ["." + Ext]`

Where `Name` is composed as:

- `Sku`
- optional `"_" + ColorHex6`
- optional `"_" + ImageNumber(2-digit when rendered)`
- optional `"_" + NameExt`
- optional `"," + TileTemplate`
- optional `"-" + TileNumber`

Equivalent pattern:

- `<Sku>[_<Color6Hex>][_<ImageNo>][_<NameExt>][,<TileTemplate>[-<TileNumber>]][.<Ext>]`

## 4.2 Parsed fields

### 4.2.1 `Sku`

- First underscore-separated token of main basename section

### 4.2.2 `Color`

- Optional second token if exactly 6 hex chars and valid color
- Stored as `#RRGGBB`

### 4.2.3 `ImageNumber`

- Parsed numeric token
- Missing or invalid -> `NoImageNumber` (`-1`)
- Rendered with zero padding (`D2`) when composing `Name`

### 4.2.4 `NameExt`

- Optional descriptive segment
- Blanks normalized via `BlankToCamelCase`
- If there are more underscore parts than expected, parser keeps the recognized first NameExt position and discards extra suffix semantics

### 4.2.5 Tile section

- Optional comma section after underscore section
- Format: `<TileTemplate>` or `<TileTemplate>-<TileNumber>`
- Tile number parsing keeps leading digits only

### 4.2.6 `Ext`

- Optional file extension

## 4.3 Hierarchical directory derivation

`ImageTreeFile.Path` is hierarchical by SKU:

- `topdir = first 3 chars of Sku`
- `subdir = first 6 chars of Sku`
- `Path = TopdirRoot + topdir + "/" + subdir + "/"`

Special case:

- if `topdir == "con"` (case-insensitive), it is rewritten to `C0N`

Path and Sku setters remove duplicate embedded hierarchy when reassigning.

## 4.4 Concrete examples

### Minimal

- Input: `1.jpg`
- Parsed path: `1/1/`
- FullName after root assignment example: `.../1/1/1.jpg`

### Standard numbered image with name extension and tile

- Input: `308024_01_200x300,4x4tile-17.tiff`
- Sku: `308024`
- ImageNumber: `1`
- NameExt: `200x300`
- TileTemplate: `4x4tile`
- TileNumber: `17`
- FullName: `.../308/308024/308024_01_200x300,4x4tile-17.tiff`

### Colorized variant

- Input: `308024_DEAD00_01_200x300,4x4tile-17.tiff`
- Color: `#DEAD00`

### Static-style name

- Input: `BildInArbeit_List.jpg`
- NameExt recognized as `List`
- Still mapped into tree path via derived SKU/name behavior

---

## 5. Implementation Notes and Nuances

- URL docs in `ImageServer/Doc/*.aspx.html` are mostly accurate, but code is the source of truth when defaults differ.
- `Count.aspx` doc mentions default `take=10`, but code default is `100`.
- `ViewerInfo.aspx` doc says callback is a parameter; code enforces it as mandatory.
- `tmp` handling differs per endpoint:
  - `Sizes.aspx`: JSON* vs XML branch
  - `ViewerInfo.aspx`: `html` branch else JSONP branch
  - `HDitem.aspx`: template decoding and special `fsi/jsonp` behavior

---

## 6. Source Index

Primary code sources:

- `HDitem.Image.Base/ImageSizes.cs`
- `HDitem.Image.Base/ImageFile.cs`
- `ItemBase/ItemRequest.cs`
- `ImageServer/HDitem.aspx.cs`
- `ImageServer/Sizes.aspx.cs`
- `ImageServer/ViewerInfo.aspx.cs`
- `ImageServer/Count.aspx.cs`
- `ImageServer/Jzoom.aspx.cs`
- `ImageServer/DisplayXml.aspx.cs`
- `ImageServer/Size.aspx.cs`

Primary documentation pages:

- `ImageServer/Doc/Sizes.aspx.html`
- `ImageServer/Doc/ViewerInfo.aspx.html`
- `ImageServer/Doc/Count.aspx.html`

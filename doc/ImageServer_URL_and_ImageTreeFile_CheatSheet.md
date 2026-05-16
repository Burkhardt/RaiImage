# ImageServer URL + ImageTreeFile Cheat Sheet

Quick reference for common requests and filename/path rules.

## URL Pattern

`http(s)://<host>/<app>/<page>.aspx?<query>`

Common apps:

- `iserv`
- `picture`

Core params seen across endpoints:

- `src=<subscriber>/<image-or-sku>`
- `tmp=<format-or-template>`
- `callback=<fn>` (JSONP)
- `catch=<code>` (JSONP try/catch body)

---

## Endpoint Cheat Sheet

## HDitem.aspx (render image)

Typical:

`/iserv/HDitem.aspx?src=pic/300004_01&tmp=Gallery`

Required (normal mode):

- `src`
- `tmp`

Special modes:

- `tmp=fsi` or `tmp=jsonp` with `cmd=view` -> forwards to `DisplayXml.aspx`
- `tmp=fsi` or `tmp=jsonp` without `cmd` -> rectangle/tile request (`5x5tile` path)

Useful overrides (template settings):

- `width`, `height`, `ext`, `quality`, `strip`, `adaptiveResize`, `unsharp`, `density`, `compress`

Other frequently used params:

- `text`, `color`, `font`, `size`, `scale`, `angle`, `colorize`, `mask`
- crop/placement: `left`, `top`, `right`, `bottom`

## Sizes.aspx (zoom tree)

Typical:

`/iserv/Sizes.aspx?src=pic/300004_01&opt=flex&ViewerPane=320x400&tmp=jsonp&callback=f&catch=(e){}`

Params:

- `src=<subscriber>/<sku-or-image>`
- `opt=<uni|tryuni|flex>`
- `ViewerPane=<WxH>` or `<WxH>,<WxH>`
- `tmp=<XML|JSON|JSONP|HTML>`
- `callback`, `catch` (optional)

Notes:

- Missing `ViewerPane` defaults to `0x0`
- SKU (no image number) returns trees for all images

## ViewerInfo.aspx (viewer bootstrap data)

Typical:

`/iserv/ViewerInfo.aspx?src=pic/300004_03&tmp=jsonp&opt=flex&ViewerPane=320x400,392x490&callback=f&catch=(e){}`

Params:

- `src=<subscriber>/<image>` (plus optional extra SKUs as comma list)
- `opt=<uni|tryuni|flex>`
- `ViewerPane=<zoomWxH>,<controlWxH>`
- `callback` (mandatory in validation)
- `catch` (optional)
- `tmp` (`html` special path, else JSONP)
- `beautify=<true|false>` (optional)

## Count.aspx (count/list images)

Typical count:

`/iserv/Count.aspx?src=pic/300004`

Typical list:

`/iserv/Count.aspx?src=pic/300004&tmp=XML&list=true`

Params:

- `src=<subscriber>/<sku-or-searchPattern>`
- `tmp=<HTML|XML|JSON|JSONP>`
- `list=<true|false>`
- `skip=<n>` default `0`
- `take=<n>` default `100`, max `1000`
- `refresh=<true|false>` default `false`
- `callback`, `catch` (optional)

Notes:

- wildcard search supported in `src` (for example `ipict/10*04`)

## Jzoom.aspx (viewer JS)

Typical:

`/iserv/Jzoom.aspx?set=HSE24PI&div=jZoom&var=zoom&src=pic/317688_01&tmp=Hugenew`

Required:

- `src`, `tmp`, `div`, `var`

Optional:

- `set` (defaults to `HDitemZoom`)

---

## ImageTreeFile Format

Canonical composed name:

`<Sku>[_<Color6Hex>][_<ImageNo>][_<NameExt>][,<TileTemplate>[-<TileNumber>]][.<Ext>]`

Examples:

- `308024_01_200x300,4x4tile-17.tiff`
- `308024_DEAD00_01_200x300,4x4tile.tiff`
- `BildInArbeit_List.jpg`

Field rules:

- `Sku`: first underscore token
- `Color`: optional 6-hex token (stored as `#RRGGBB`)
- `ImageNo`: optional numeric token (`-1` when missing internally)
- `NameExt`: optional descriptor token
- `TileTemplate`: optional after comma
- `TileNumber`: optional after dash, leading digits kept
- `Ext`: optional extension

### Path Derivation (ImageTreeFile)

Hierarchical path is SKU-based:

- `topdir = first 3 chars of Sku`
- `subdir = first 6 chars of Sku`
- `Path = <root>/<topdir>/<subdir>/`

Special case:

- `con` topdir is rewritten to `C0N`

Example:

- `308024_01_200x300,4x4tile-17.tiff` -> `.../308/308024/308024_01_200x300,4x4tile-17.tiff`

---

## Full Spec

Detailed version:

- `ImageServer/Doc/ImageServer_URL_and_ImageTreeFile_Format.md`

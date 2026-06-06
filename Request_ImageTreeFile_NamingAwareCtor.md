# Request to RAIkeep Agent — naming-aware ImageTreeFile construction from a name/ShortName

From the AfricaStage integration (cache-purge endpoint). We need to construct /
parse an `ImageTreeFile` from a route value (a **ShortName** like
`AfricanPicnic_04`, or an unnumbered `GageElementary`) rooted at a `RaiPath`,
honoring the storage naming convention — **without** a real source file on disk,
**without** a dummy extension, and **without** the caller re-implementing the
naming inference.

## Why the current API forces a workaround

Today the only naming-aware constructor is:

```csharp
public ImageTreeFile(string filename, PathConventionType, ImageNamingConvention)
```

It requires a full **file-path string with an extension**. And the component
constructor:

```csharp
public ImageTreeFile(RaiPath rootPath, string itemId, string templateName,
                     string ext, PathConventionType pathConvention = ItemIdTree8x2)
```

takes `itemId`/`templateName` as **literal components** (no parsing) and has **no
`ImageNamingConvention`** parameter, so it can't turn a combined
`"AfricanPicnic_04"` into `ItemId="AfricanPicnic" + ImageNumber=4`.

Net effect: to parse a route ShortName under `Structured`, AfricaStage has to do

```csharp
var stem  = StripKnownExtension(itemId);            // re-implements ext stripping
var nam   = InferNamingConvention(stem);            // re-implements InferSourceNamingConvention
var probe = new ImageTreeFile(ownerRoot.FullPath + stem + ".png", conv, nam);  // dummy ".png"
```

The `InferNamingConvention` copy is the real hazard: it can **drift from RaiImage's
own `InferSourceNamingConvention`**, so purge-time parsing could silently diverge
from render-time naming.

## Requested change (either of these; auto-infer preferred)

**Preferred — a parsing factory/ctor that auto-infers naming (mirrors what
`FromImageTree` already does internally):**

```csharp
// Parses `name` (ShortName or full name) into ItemId/ImageNumber/TemplateName,
// rooted at rootPath, inferring the ImageNamingConvention the same way
// FromImageTree does. No extension required.
public static ImageTreeFile FromName(
    RaiPath rootPath, string name,
    PathConventionType convention = PathConventionType.ItemIdTree8x2);
```

**Acceptable fallback — the component ctor (above) plus an `ImageNamingConvention`
that is actually applied (ApplyNamingConvention), so a combined ShortName passed
as `itemId` parses:**

```csharp
public ImageTreeFile(RaiPath rootPath, string itemId, string templateName,
                     string ext, PathConventionType pathConvention,
                     ImageNamingConvention naming);
```

…and/or simply **expose the existing inference** so callers stay in lockstep:

```csharp
public static ImageNamingConvention InferSourceNamingConvention(string itemId); // make public
```

## Acceptance (round-trip, no file needed)

Given `rootPath` and `ItemIdTree8x2`:

| input name              | ItemId          | ImageNumber | TemplateName | ShortName          |
|-------------------------|-----------------|-------------|--------------|--------------------|
| `AfricanPicnic_04`      | `AfricanPicnic` | 4           | `""`         | `AfricanPicnic_04` |
| `AfricanPicnic_04_Small`| `AfricanPicnic` | 4           | `Small`      | `AfricanPicnic_04` |
| `GageElementary`        | `GageElementary`| NoImageNumber | `""`       | `GageElementary`   |
| `GageElementary_Huge`   | `GageElementary`| NoImageNumber | `Huge`     | `GageElementary`   |

`SubdirRoot` must resolve to the same 8x2 bucket regardless of TemplateName, so a
caller can enumerate the bucket and delete derivatives (non-empty `TemplateName`)
while keeping the source (empty `TemplateName`).

## How AfricaStage will use it (so the design fits)

```csharp
var target = ImageTreeFile.FromName(ownerImagesRoot, routeShortName);  // no ext, auto-infer
foreach (var file in target.SubdirRoot.EnumerateFiles(target.ItemId + "*").ToList())
{
    var cand = ImageTreeFile.FromName(/* from file */ ...);
    if (cand.ShortName == target.ShortName && !string.IsNullOrEmpty(cand.TemplateName))
        file.rm();   // a rendered derivative of this original
}
```

This lets AfricaStage drop its `StripKnownExtension`, `InferNamingConvention`, and
the dummy `".png"` entirely, and keeps purge-time parsing identical to
render-time.

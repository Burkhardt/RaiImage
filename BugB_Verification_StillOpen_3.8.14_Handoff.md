# RaiImage Bug B — verification follow-up: STILL OPEN as of 3.8.14

Handoff for the RAIkeep Agent. Follow-up to
`BugB_ImageNumber_RenderTarget_Handoff.md`. Bug A (source resolution) is fixed
and verified; **Bug B (rendered output drops the ImageNumber) is not yet fixed**,
despite the 3.8.14 release note wording. This note records the verification so
the next attempt targets the right code.

## Verdict

- **Bug A — RESOLVED** in 3.8.11+. Confirmed at 3.8.14:
  `dotnet test RaiImage.Tests/RaiImage.Tests.csproj --filter "…ImageTreeFile…"`
  → 20 passed, 0 failed. The probe now excludes derivatives via the `NameExt`
  check and matches on `ImageNumber`.
- **Bug B — STILL OPEN** at 3.8.14. The rendered **output filename** still drops
  the `ImageNumber`, so album items collide.

## Evidence it was NOT fixed in 3.8.12 / 3.8.14

1. `git diff 7813679..HEAD --stat` (3.8.11 → 3.8.14) shows **no `.cs` changes** —
   only docs, puml, svg, csproj version bumps, and RELEASE_NOTES. The rendering
   code is byte-identical to 3.8.11.
2. `CreateRenderingTarget` ([ImageRendering.cs:400](ImageRendering.cs)) is
   unchanged and still names the output from `ItemId` + render name only:
   ```csharp
   var target = new ImageTreeFile(Path, ItemId, renderingName,
       TemplateSetting.NormalizeExtension(ext, TemplateSetting.DefaultFormat), Convention);
   ```
   No `ImageNumber` is composed into the name.
3. `FromImageTree` ([ImageRendering.cs:248](ImageRendering.cs)) still builds the
   source with the default `ImageNamingConvention.Legacy` and exposes no way to
   request `Structured`.
4. The 20 green tests all exercise **source resolution**
   (`ExtendToFirstExistingFile` / `FromImageTree`). **None** exercise
   `ApplyTemplate` output naming for a numbered item, so Bug B is uncovered.

## Misleading release note (please correct)

`RELEASE_NOTES_3.8.14.md` says *"Keeps the image-number-preserving rendering
behavior from the prior patch line."* That phrase describes the **probe**
(which matches on `ImageNumber`), not the rendered **output filename**. As
written it implies Bug B is handled; it is not. Recommend rewording to scope it
to source resolution.

## Reproduction (rendering path, not resolution)

```
GET /img/Dr2RAI/AfricanPicnic_01?tmp=Huge  -> writes AfricanPicnic_Huge.webp
GET /img/Dr2RAI/AfricanPicnic_02?tmp=Huge  -> writes AfricanPicnic_Huge.webp  (overwrites _01)
```

## Fix required (unchanged from the first handoff)

1. Let `FromImageTree` / `ApplyTemplate` accept (or default to)
   `ImageNamingConvention.Structured`, so the source carries the parsed
   `ImageNumber`.
2. Make `CreateRenderingTarget` compose `ItemId_ImageNumber_renderingName` when
   an `ImageNumber` is present (stay `ItemId_renderingName` when it is
   `NoImageNumber`).

## Acceptance test to add (must be a RENDER test, not a resolution test)

In `ImageRenderingTests` (uses the fake-magick harness): render
`AfricanPicnic_01` and `AfricanPicnic_02` with the same template and assert the
two output files are **distinct** (`AfricanPicnic_01_Huge.webp` ≠
`AfricanPicnic_02_Huge.webp`), and that a no-number item (`GageElementary`)
still renders `GageElementary_Huge.webp`.

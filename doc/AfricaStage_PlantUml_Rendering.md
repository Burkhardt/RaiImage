# AfricaStage Agent Instructions: Requesting SVG from a PlantUML String

This note explains how an AfricaStage-side agent should use the new RaiImage PlantUML API.

## Goal

Given:

- a `subscriber`
- an `itemId`
- a PlantUML diagram as a `string`

the agent should ask RaiImage to:

1. store the PlantUML source as a `.puml` file
2. render the matching `.svg` file
3. keep both files under the subscriber's existing `ItemTreePath` / `ImageTreeFile` hierarchy

## Important design choice

For PlantUML-backed SVG output, do not use RaiImage templates or overlays.

Why:

- SVG is already resolution-independent
- the PlantUML source is the real original
- the rendered SVG is a direct derivative of that source
- templates and overlays would add image-processing semantics that do not belong to this flow

So the intended flow is:

`PlantUML string -> .puml file -> PlantUML render -> .svg file`

## API to call

Use:

```csharp
PlantUmlRenderResult result = ImageTreeFile.RenderPlantUml(
    imageTreeRoot,
    subscriber,
    itemId,
    plantUmlContent);
```

Relevant API elements:

- `ImageTreeFile.RenderPlantUml(...)`
- `PlantUmlRenderResult.Source`
- `PlantUmlRenderResult.Svg`

## What the call does

`ImageTreeFile.RenderPlantUml(...)` will:

1. resolve the subscriber root as `imageTreeRoot / subscriber`
2. derive the correct `ImageTreeFile` / `ItemTreePath` location from the passed `itemId`
3. create and save the PlantUML source as a `.puml` file in that hierarchy
4. call the local `plantuml` executable through RaiImage's CLI wrapper
5. generate the sibling `.svg` file in the same hierarchy
6. return both file handles

## Returned files

The result contains two `ImageTreeFile` instances:

- `result.Source`: the saved `.puml` file
- `result.Svg`: the rendered `.svg` file

These are not temporary files.

They are intended to live inside the normal ImageServer file tree for the given subscriber.

That means both files belong under:

- the given `subscriber`
- the `ItemTreePath` / `ImageTreeFile` location derived from the given `itemId`

In other words, the `.puml` and `.svg` are peer files in the same tree slot.

## Expected usage pattern in AfricaStage

Typical flow:

1. Receive or compose a PlantUML string.
2. Decide the `subscriber`.
3. Decide the `itemId` that should own the diagram.
4. Call `ImageTreeFile.RenderPlantUml(...)`.
5. Keep `result.Source` as the editable/original diagram source.
6. Serve or reference `result.Svg` as the rendered output.

## Example

```csharp
using OsLib;
using RaiImage;

RaiPath imageTreeRoot = new RaiPath("/path/to/images/");
string subscriber = "AfricaStage";
string itemId = "SchoolLandscapeDiagram_01";
string plantUmlContent = """
@startuml
title School Landscape
actor Teacher
actor Student
Teacher --> Student : teaches
@enduml
""";

PlantUmlRenderResult result = ImageTreeFile.RenderPlantUml(
    imageTreeRoot,
    subscriber,
    itemId,
    plantUmlContent);

ImageTreeFile pumlFile = result.Source;
ImageTreeFile svgFile = result.Svg;
```

## Operational expectation

PlantUML is a required system dependency for this flow.

RaiImage now fails fast if PlantUML is not installed or not resolvable on the machine.

So the AfricaStage agent should assume:

- PlantUML must be installed
- missing PlantUML is a configuration/runtime error
- this is not a soft fallback case

If PlantUML is missing, RaiImage throws immediately with a clear error instead of silently degrading.

## Guidance for caller behavior

The AfricaStage agent should:

- treat the `.puml` file as the authoritative source artifact
- treat the `.svg` file as the authoritative rendered artifact
- not route PlantUML diagrams through `ApplyTemplate(...)`
- not route PlantUML diagrams through `ApplyOverlay(...)`
- not expect raster-style image post-processing for this API

## Summary

Use `ImageTreeFile.RenderPlantUml(...)` when AfricaStage has a PlantUML string and wants a persistent SVG under a subscriber-owned ImageServer tree location.

The result is:

- one stored `.puml` source file
- one stored `.svg` render file
- both located in the normal `ItemTreePath` / `ImageTreeFile` hierarchy for the given subscriber and item

# RaiImage 3.11.3 Release Notes

## Summary

- Releases `RaiImage` version `3.11.3`.
- Adds first-class PlantUML rendering support through `ImageTreeFile.RenderPlantUml(...)` with subscriber-tree persistence of `.puml` source and sibling `.svg` output.
- Adds `PlantUmlCommand`, `PlantUml`, and `PlantUmlRenderResult` for CLI integration, including direct binary and `java -jar` execution paths.
- Adds regression coverage for PlantUML rendering and jar execution flows in `RaiImage.Tests`.
- Keeps `WordCase` as the supported replacement for the retired `CamelCase` helper.
- Aligns fallback package references to `OsLibCore 3.11.3` and `RaiUtils 3.11.3`.

## Validation

- Run `dotnet test RaiImage/RaiImage.slnx --no-restore --nologo -v minimal` from the RAIkeep workspace.

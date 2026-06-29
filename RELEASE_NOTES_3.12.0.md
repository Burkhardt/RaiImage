# RaiImage 3.12.0 Release Notes

## Summary

- Releases `RaiImage` version `3.12.0`.
- Carries forward first-class PlantUML rendering support through `ImageTreeFile.RenderPlantUml(...)` with subscriber-tree persistence of `.puml` source and sibling `.svg` output.
- Carries forward `PlantUmlCommand`, `PlantUml`, and `PlantUmlRenderResult` for CLI integration, including direct binary and `java -jar` execution paths, from `3.11.4`.
- Keeps the PlantUML rendering and jar execution regression coverage in `RaiImage.Tests`.
- Keeps `WordCase` as the supported replacement for the retired `CamelCase` helper.
- Aligns fallback package references to `OsLibCore 3.12.0` and `RaiUtils 3.12.0`.

## Validation

- Run `dotnet test RaiImage/RaiImage.slnx --no-restore --nologo -v minimal` from the RAIkeep workspace.

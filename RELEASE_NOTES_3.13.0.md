# RaiImage 3.13.0 Release Notes

## Summary

- Releases `RaiImage` version `3.13.0`.
- Carries forward `WordCase` as the supported replacement for the retired `CamelCase` helper.
- Keeps the smarter trailing-digit filename normalization and uppercase-token handling that stay aligned with the `iorg` CLI tree flow.
- Aligns fallback package references to `OsLibCore 3.13.0` and `RaiUtils 3.13.0`.

## Validation

- `dotnet test RaiImage/RaiImage.slnx --nologo -v minimal`
- Regenerated the tracked PlantUML-backed SVG diagrams for the `3.13.0` release markers.

# RaiImage 3.10.0 Release Notes

## Summary

- Releases `RaiImage` version `3.10.0`.
- Carries forward separated and compact trailing digits as `ImageNumber` during `ImageFile.EasyFileName(...)` normalization.
- Carries forward all-uppercase token preservation in `WordCase` PascalCase output for acronym-heavy item ids.
- Aligns fallback package references to `OsLibCore 3.10.0` and `RaiUtils 3.10.0`.
- Refreshes markdown and PlantUML release markers for the coordinated minor line.
- No public API changes from `3.9.1`.

## Validation

- `dotnet test RaiImage.Tests/RaiImage.Tests.csproj --nologo -v minimal`
- NuGet publishing remains wired through the tag-triggered `publish-nuget.yml` workflow.

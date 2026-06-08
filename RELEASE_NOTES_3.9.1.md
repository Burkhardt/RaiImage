# RaiImage 3.9.1 Release Notes

## Summary

- Releases `RaiImage` version `3.9.1`.
- Preserves separated and compact trailing digits as `ImageNumber` during `ImageFile.EasyFileName(...)` normalization.
- Preserves all-uppercase tokens in `WordCase` PascalCase output so acronym-heavy item ids normalize predictably.
- Aligns fallback package references to `OsLibCore 3.9.1` and `RaiUtils 3.9.1`.
- Refreshes markdown and PlantUML release markers for the coordinated patch line.

## Validation

- `dotnet test RaiImage.Tests/RaiImage.Tests.csproj --nologo -v minimal`
- NuGet publishing is handled by tag-triggered `publish-nuget.yml`.

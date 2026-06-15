# RaiImage 3.10.1 Release Notes

## Summary

- Releases `RaiImage` version `3.10.1`.
- Carries forward separated and compact trailing digits as `ImageNumber` during `ImageFile.EasyFileName(...)` normalization.
- Carries forward all-uppercase token preservation in `WordCase` PascalCase output for acronym-heavy item ids.
- Aligns fallback package references to `OsLibCore 3.10.1` and `RaiUtils 3.10.1`.
- Refreshes current markdown and PlantUML release markers for the coordinated package line.
- No public API changes from `3.10.0`.

## Validation

- `dotnet test RaiImage.Tests/RaiImage.Tests.csproj --nologo -v minimal`
- NuGet publishing remains wired through the parent sequential release chain and the tag-triggered `publish-nuget.yml` workflow.

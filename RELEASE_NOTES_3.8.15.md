# RaiImage 3.8.15 Release Notes

## Summary

- Releases `RaiImage` version `3.8.15`.
- Fixes numbered render-target filenames so `AfricanPicnic_01` and `AfricanPicnic_02` render to distinct outputs such as `AfricanPicnic_01_Huge.webp` and `AfricanPicnic_02_Huge.webp`.
- Adds render regression coverage for numbered and unnumbered image items.
- Aligns fallback package references to `OsLibCore 3.8.15` and `RaiUtils 3.8.15`.
- Refreshes markdown and PlantUML release markers for the coordinated chain.

## Validation

- `dotnet test RaiImage.Tests/RaiImage.Tests.csproj --nologo -v minimal`
- NuGet publishing is handled by tag-triggered `publish-nuget.yml`.

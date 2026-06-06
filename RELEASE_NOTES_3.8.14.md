# RaiImage 3.8.14 Release Notes

## Summary

- Releases `RaiImage` version `3.8.14`.
- Aligns fallback package references to `OsLibCore 3.8.14` and `RaiUtils 3.8.14`.
- Refreshes markdown and PlantUML release markers for the coordinated chain.
- Keeps the image-number-aware source resolution behavior from the prior patch line.

## Validation

- `dotnet test RaiImage.Tests/RaiImage.Tests.csproj --nologo -v minimal`
- NuGet publishing is handled by tag-triggered `publish-nuget.yml`.

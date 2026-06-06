# RaiImage 3.9.0 Release Notes

## Summary

- Releases `RaiImage` version `3.9.0`.
- Adds naming-aware `ImageTreeFile.FromName(...)` factories for rooted short-name parsing without a dummy extension.
- Makes `ImageTreeFile.InferSourceNamingConvention(...)` public so caller-side parsing can stay aligned with RaiImage's own inference.
- Applies the naming-aware component constructor when creating tree-based render targets and aligns fallback package references to `OsLibCore 3.9.0` and `RaiUtils 3.9.0`.
- Refreshes markdown and PlantUML release markers for the coordinated minor line.

## Validation

- `dotnet test RaiImage.Tests/RaiImage.Tests.csproj --nologo -v minimal`
- NuGet publishing is handled by tag-triggered `publish-nuget.yml`.

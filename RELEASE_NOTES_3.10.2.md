# RaiImage 3.10.2 Release Notes

## Summary

- Releases `RaiImage` version `3.10.2`.
- Carries forward current filename-normalization behavior and acronym-preserving word-case behavior from `3.10.1`.
- Aligns fallback package references to `OsLibCore 3.10.2` and `RaiUtils 3.10.2`.
- Refreshes current markdown and PlantUML release markers for the coordinated package line.

## Validation

- `dotnet test RaiImage.Tests/RaiImage.Tests.csproj --nologo -v minimal`
- NuGet publishing remains wired through the parent sequential release chain and the tag-triggered `publish-nuget.yml` workflow.

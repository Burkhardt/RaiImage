# RaiImage 3.8.0 Release Notes

## Summary

- Coordinated release for `RaiImage` version `3.8.0`.
- Aligns fallback package references with `OsLibCore 3.8.0` and `RaiUtils 3.8.0`.
- Keeps the packaged README in the NuGet payload.
- No public RaiImage API surface changes in this release.

## Documentation

- Updated `README.md`, `API.md`, `TESTING.md`, and `ARCHITECTURE-ALIGNMENT.md` for the `3.8.0` package line.
- Refreshed the active RaiImage PlantUML headers so the current diagrams are labeled consistently with the live release.

## Validation

- `dotnet pack RaiImage.csproj --nologo -v minimal`
- Result: package builds successfully and the `.nupkg` contains `README.md` and `HardCastle.png`.

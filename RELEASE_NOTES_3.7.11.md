# RaiImage 3.7.11 Release Notes

## Summary

- Patch release for `RaiImage` version `3.7.11`.
- Aligns fallback package references with `OsLibCore 3.7.11` and `RaiUtils 3.7.11`.
- No public RaiImage API surface changes in this patch.

## Documentation

- Updated `README.md`, `API.md`, `TESTING.md`, and `ARCHITECTURE-ALIGNMENT.md` for the `3.7.11` package line.
- Refreshed the active RaiImage PlantUML headers so the current diagrams are labeled consistently with the live release.
- Regenerated the rendered SVG diagram artifacts from the updated PlantUML sources.

## Validation

- `dotnet test RaiImage.slnx --nologo -v minimal`
- Result: 54 passed, 0 failed, 0 skipped.

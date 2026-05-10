# RaiImage 3.7.9 Release Notes

## Summary

- Patch release for `RaiImage` version `3.7.9`.
- Aligns fallback package references with `OsLibCore 3.7.9` and `RaiUtils 3.7.9`.
- No public RaiImage API surface changes in this patch.

## Documentation

- Updated `README.md`, `API.md`, `TESTING.md`, and `ARCHITECTURE-ALIGNMENT.md` for the `3.7.9` package line.
- Refreshed the active RaiImage PlantUML headers so the current diagrams are labeled consistently with the live release.

## Validation

- `dotnet test RAIkeep.slnx --nologo -v minimal`
- Result: 220 passed, 0 failed, 0 skipped.

# RaiImage 3.2.0 Release Notes

## Highlights

- Version bumped to `3.2.0`.
- ImageMagick wrapper moved to modern CLI invocation style (`magick` + subcommands).
- Vulnerability warnings addressed in package dependencies.
- Project-reference-first development flow enabled for local sibling repos.
- Initial `RaiImage.Tests` xUnit suite added.

## Dependency and security updates

- `System.Drawing.Common` upgraded to `10.0.4`.
- Direct `Newtonsoft.Json` reference set to `13.0.4`.
- Removed redundant `System.Security.AccessControl` package reference that caused `NU1510`.

## Build and analyzer improvements

- Added platform guards for Windows-only API paths to resolve `CA1416` warnings.
- Removed hardcoded Windows executable paths from ImageMagick defaults.

## Compatibility

This release includes intentional breaking changes in command invocation assumptions.
See `MIGRATION_3.2.0.md` for upgrade guidance.

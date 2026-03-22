# RaiImage 3.5.2 Release Notes

## Highlights

- Version bumped to `3.5.2`.
- RaiImage documentation now aligns with the shared `3.5.2` package line used across `RAIkeep`.
- Fallback package references now align with `OsLibCore 3.5.2` and `RaiUtils 3.5.2`.
- The fallback utility package id is now `RaiUtils` rather than `RaiUtilsCore`.

## Cross-Package Alignment

- RaiImage remains compatible with the shared OsLib configuration model based on `osconfig.json`.
- Documentation continues to align with JsonPit's `Id`-based identifier contract across the package stack.
- RaiImage continues to consume the current OsLib `RaiPath`-based directory APIs without intentional behavioral changes in this patch.

## Validation

- RaiImage should be validated with the local `RaiImage` test suite and package build before publishing.
# RaiImage 3.5.0 Release Notes

## Highlights

- Version bumped to `3.5.0`.
- RaiImage documentation now aligns with the shared `3.5.0` package line used across `RAIkeep`.
- The documented cloud-backed support claim now focuses on `OneDrive`, `GoogleDrive`, and `Dropbox`.
- RaiImage package metadata and fallback package references were aligned with the shared `3.5.0` package line.

## Cross-Package Alignment

- RaiImage remains compatible with the shared OsLib configuration model based on `osconfig.json`.
- Documentation now explicitly aligns with JsonPit's `Id`-based identifier contract across the package stack.
- RaiImage continues to consume the updated OsLib `RaiPath`-based directory APIs without behavioral regressions in the workspace test run.

## Validation

- RaiImage remained green in the full workspace validation run for `3.5.0`.
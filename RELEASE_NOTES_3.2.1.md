# RaiImage 3.2.1 Release Notes

## Highlights

- Version bumped to `3.2.1`.
- `ImageMagick` was extracted from `ImageFile.cs` into its own source file.
- RaiImage no longer depends on `Os.winInternal` for the Windows retry path.
- Fallback package references now align with `OsLibCore 3.2.1` and `RaiUtilsCore 3.2.1`.

## Compatibility

- Public package version is now aligned with the rest of the current RAIkeep library set.
- Existing `magick`-based command invocation behavior remains in place.

## Validation

- Solution validation remained green after the refactor and version alignment.
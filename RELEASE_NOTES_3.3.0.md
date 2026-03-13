# RaiImage 3.3.0 Release Notes

## Highlights

- Version bumped to `3.3.0`.
- `ImageMagick` was extracted from `ImageFile.cs` into its own source file.
- RaiImage no longer depends on `Os.winInternal` for the Windows retry path.
- Fallback package references now align with `OsLibCore 3.3.0` and `RaiUtilsCore 3.3.0`.

## Compatibility

- Public package version is now aligned with the rest of the current RAIkeep library set.
- Existing `magick`-based command invocation behavior remains in place.
- RaiImage stays compatible with OsLib's new `Os.Config` and `osconfig.json` cloud-root model.

## Validation

- Solution validation remained green after the refactor and version alignment.
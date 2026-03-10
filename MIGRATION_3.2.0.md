# Migration Guide 3.2.0

This release introduces intentional breaking changes to modernize command execution and dependency management.

## Breaking changes

1. ImageMagick CLI style changed to ImageMagick 7+.
- Old behavior assumed legacy binaries like `Convert.exe`, `Composite.exe`, `Identify.exe`, `Mogrify.exe`.
- New behavior uses `magick` with subcommands (`magick convert`, `magick identify`, etc.).

2. Hardcoded Windows executable defaults removed.
- Removed hardcoded command path assumptions such as `C:\\bin\\...` defaults for imaging tools.
- Commands now default to tool names expected on `PATH`.

3. Zip fallback command variable removed from runtime flow.
- Zip creation now uses OsLib file utility behavior (`RaiFile.Zip`) in the conversion zip branch.

4. Local development now prefers project references.
- `RaiImage.csproj` uses local `ProjectReference` entries to OsLib and RaiUtils when sibling repos are available.
- Package references remain as fallback for external consumers.

## Required setup

## ImageMagick
- Install ImageMagick 7+.
- Ensure `magick` is available on system `PATH`, or set `ImageMagick.ImPath` to folder containing the `magick` executable.

## Optional external tools
- `optipng` for PNG optimization.
- `jpegtran` for JPEG optimization.

If missing, methods depending on those tools can fail with non-zero exit codes and tool messages.

## Code migration notes

- If your code set `ConvertCommand` or related `*.exe` command properties, migrate to:
  - `ImageMagick.MagickCommand`
  - `ImageMagick.ImPath`
- Prefer `ImageMagick.Convert/Mogrify/Composite/Identify` overloads unchanged at call sites.

## Compatibility rationale

This release intentionally favors current tooling and cross-platform behavior over old command-line compatibility.

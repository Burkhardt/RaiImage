# Testing RaiImage

## 3.7.0 scope note

- The packaged `RAIkeep` support claim for cloud-backed paths is `OneDrive`, `GoogleDrive`, and `Dropbox`.
- RaiImage test expectations should stay aligned with the same OsLib/JsonPit package line and configuration contract.
- Fallback package validation for release builds now assumes `OsLibCore 3.7.0` and `RaiUtils 3.7.0`.

## Test projects

- `RaiImage.Tests` (xUnit)

## Run tests

From repository root:

```bash
dotnet test RaiImage.slnx
```

Or only test project:

```bash
dotnet test RaiImage.Tests/RaiImage.Tests.csproj
```

## Current coverage focus

- Item tree path partitioning and normalization (`ItemTreePath`)
- Filename normalization defaults (`ImageFile.EasyFileName`)
- ImageMagick wrapper constructor behavior with and without configured executable paths

## Planned coverage expansion

- `ImageFile.Parse` edge cases for SKU/color/image-number combinations
- `ImageTreeFile.MoveToTree` and `CopyTo` behavior on temporary directory trees
- `ImageMagick` commandline output behavior via mockable process abstraction
- `CreateTiles` behavior under deterministic fixture images
- Optimizer wrappers (`OptiPng`, `JpegTran`) using optional tool-availability probes

## Platform notes

- Some operations depend on external binaries (`magick`, `optipng`, `jpegtran`).
- Unit tests should avoid requiring these by default unless marked as integration tests.
- For CI portability, keep external-tool tests optional or gated.

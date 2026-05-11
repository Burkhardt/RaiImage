# Release Notes 3.8.1

## Summary

- Changes `ImageTreeFile.mkdir()` from member hiding to a true override of `RaiFile.mkdir()`.
- Ensures base-class operations such as `WriteFromAsync(...)` dispatch to image-tree directory creation logic.
- Aligns fallback package references with `OsLibCore 3.8.1` and `RaiUtils 3.8.1`.

## Validation

- `dotnet test RAIkeep.slnx --nologo -v minimal`
- Result: 223 passed, 0 failed, 0 skipped.
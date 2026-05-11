# Release Notes 3.8.1

## Summary

- Changes `ImageTreeFile.mkdir()` from member hiding to a true override of `RaiFile.mkdir()`.
- Ensures base-class operations such as `WriteFromAsync(...)` dispatch to image-tree directory creation logic.
- Aligns the fallback `OsLibCore` package reference with `3.8.1`.

## Validation

- `dotnet test RAIkeep.slnx --nologo -v minimal`
- Result: 223 passed, 0 failed, 0 skipped.
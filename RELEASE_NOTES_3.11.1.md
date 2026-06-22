# RaiImage 3.11.1 Release Notes

## Summary

- Releases `RaiImage` version `3.11.1`.
- Carries forward `WordCase` as the supported replacement for the retired `CamelCase` helper.
- Preserves the current filename-normalization flow used by `ImgSeeder`/`iorg`.
- Aligns fallback package references to `OsLibCore 3.11.1` and `RaiUtils 3.11.1`.

## Validation

- Run `dotnet test RaiImage/RaiImage.slnx --no-restore --nologo -v minimal` from the RAIkeep workspace.

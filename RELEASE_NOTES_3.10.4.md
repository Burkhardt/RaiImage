# RaiImage 3.10.4 Release Notes

## Summary

- Releases `RaiImage` version `3.10.4`.
- Documents `WordCase` as the supported replacement for the retired `CamelCase` helper.
- Refreshes the live hierarchy diagram source so current API docs no longer advertise the removed `CamelCase` type.
- Keeps fallback package references on `OsLibCore 3.10.2` and `RaiUtils 3.10.2`.

## Validation

- Run `dotnet test RaiImage/RaiImage.slnx --no-restore --nologo -v minimal` from the RAIkeep workspace.

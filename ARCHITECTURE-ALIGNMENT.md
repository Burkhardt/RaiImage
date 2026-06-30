# Architecture Alignment Note (2026-03-04)

This repository aligns with private internal architecture decisions maintained outside this public repository.

## 3.11.3 release alignment

- Coordinated release: publishes RaiImage `3.11.3` with fallback dependencies aligned to the current `3.11.3` OsLib/RaiUtils package line.
- The packaged support claim for cloud-backed paths is `OneDrive`, `GoogleDrive`, `ICloudDrive`, and `Dropbox`.
- Cross-package wording now reflects JsonPit's `Id`-based identifier contract.
- The aligned fallback dependencies remain `OsLibCore 3.11.3` and `RaiUtils 3.11.3` in the current package line.
- The active RaiImage patch behavior is smarter filename normalization for trailing image numbers and uppercase tokens used by structured tree workflows.
- `WordCase` is the supported word-case helper; the old `CamelCase` class is retired and should not appear in current diagrams.

## intent for RaiImage

- Continue with a merge-based re-architecture.
- Preserve mature RaiImage behavior while integrating useful modern improvements.
- Keep RaiImage as owner of image-domain semantics (`ImageFile`, `ImageTreeFile`, `ItemTreePath`).

## practical migration stance

- Compare legacy RaiImage behavior with newer variants explicitly.
- Merge in increments with tests and documented behavior decisions.
- Avoid all-at-once replacement that risks regressions.

## compatibility stance

- RaiImage compatibility is pragmatic and value-driven.
- Compatibility burden is intentionally stronger in OsLib than in RaiImage.

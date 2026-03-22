# Architecture Alignment Note (2026-03-04)

This repository aligns with private internal architecture decisions maintained outside this public repository.

## 3.5.0 release alignment

- RaiImage documentation now aligns with the `3.5.0` `RAIkeep` package line.
- The packaged support claim for cloud-backed paths is `OneDrive`, `GoogleDrive`, and `Dropbox`.
- Cross-package wording now reflects JsonPit's `Id`-based identifier contract.

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

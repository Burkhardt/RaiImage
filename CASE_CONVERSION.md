# Case conversion moved to RaiUtils

Beginning with RAIkeep 4.2.6, the canonical `WordCase`, `StringHelper`,
`WordSplit`, `CamelSplit`, and `ToTitle` implementation lives in RaiUtils.
RaiUtils also provides CR019's lossless Unicode-safe `WordSeams()` API.

See the current documentation:
[RaiUtils/CASE_CONVERSION.md](https://github.com/Burkhardt/RaiUtils/blob/main/CASE_CONVERSION.md).

RaiImage retains only deprecated compatibility facades for callers compiled
against earlier versions. Recompiled source should import `RaiUtils`; the old
RaiImage static facade methods are intentionally no longer extension methods.

# Case Conversion Helper

`WordCase` is the RaiImage word-case conversion helper. It converts between PascalCase, lower camelCase, snake_case, and kebab-case.

## API

Create an instance from any supported input form:

```csharp
var name = new WordCase("San-Diego-State-09.24-212");
```

Read the desired output form from a property:

```csharp
name.PascalCase;      // SanDiegoState0924212
name.LowerCamelCase;  // sanDiegoState0924212
name.SnakeCase;       // san_diego_state_09_24_212
name.KebabCase;       // san-diego-state-09-24-212
name.DashCase;        // san-diego-state-09-24-212
```

`KebabCase` is the common name for dash-separated lower-case words. `DashCase` is kept as a readable alias.

Use `LowerCamelCase` for lower camelCase output. `CamelCaseString` remains as a compatibility alias for older callers.

## Input Detection

The string constructor detects input automatically. Do not call explicit `FromPascalCase`, `FromCamelCase`, or `FromSnakeCase` methods; those are intentionally not part of the API.

Supported inputs include:

```text
San-Diego-State-09.24-212
nomsa-concert-167
Mixed_Snake.AndPascal-and-kebabCase
NomsaConcert167
nomsaConcert167
nomsa_concert_167
nomsa-concert-167
```

The splitter treats any non-letter, non-digit character as a separator, then also splits each segment on camel/Pascal word boundaries. This means separators can be mixed in one input.

## Separator Semantics

Separators are normalization hints, not preserved punctuation.

For example:

```csharp
var name = new WordCase("San-Diego-State-09.24-212");
```

Produces:

```text
PascalCase:     SanDiegoState0924212
LowerCamelCase: sanDiegoState0924212
SnakeCase:      san_diego_state_09_24_212
KebabCase:      san-diego-state-09-24-212
```

The dot in `09.24` is intentionally lost in PascalCase and lower camelCase. When converting to snake_case or kebab-case, it becomes `_` or `-` respectively. This loss is by design.

## File Extensions

Do not pass file extensions to `WordCase` when converting image names.

Extension handling belongs in image/file-level code such as `ImageFile`. For example, convert `nomsa-concert-167`, not `nomsa-concert-167.jpg`.

## Legacy Compatibility

The following older entry points still exist:

```csharp
new WordCase("nomsa-concert-167").String;          // NomsaConcert167
new WordCase("nomsa-concert-167").CamelCaseString; // nomsaConcert167
"nomsa-concert-167".WordSplit();                  // ["nomsa", "concert", "167"]
"nomsa-concert-167".CamelSplit();                 // compatibility alias
```

`String` defaults to `PascalCase` for legacy callers.

The old `CamelCase` class is retired. Treat it as obsolete API documentation and migrate callers to `WordCase`.

## Tests

The behavior is covered in `RaiImage.Tests/WordCaseTests.cs`.

Run the focused tests with:

```bash
dotnet test RaiImage/RaiImage.Tests/RaiImage.Tests.csproj --filter FullyQualifiedName~WordCaseTests
```

The broader RaiImage suite should also remain green:

```bash
dotnet test RaiImage/RaiImage.Tests/RaiImage.Tests.csproj --no-build
```

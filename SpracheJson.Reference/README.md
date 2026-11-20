# SpracheJson test reference

This project compiles the unmodified C# sources in `Upstream/` from
[IanWold/SpracheJson commit aa8092aa4f5a7f1675200ac908d20c06e285aec4](https://github.com/IanWold/SpracheJson/tree/aa8092aa4f5a7f1675200ac908d20c06e285aec4).
The original MIT license is in `LICENSE.md`; the original README is preserved
in `UPSTREAM-README.md`.

Only the build wrapper differs: it targets this repository's `net11.0` instead
of upstream's `net6.0`, is not packable, and uses `VersionOverride` to retain
upstream's Sprache `3.0.0-develop-00049` without changing the benchmark's central
Sprache `2.3.1`. Implicit usings and nullable settings are unchanged.
The parser, models, mapping, and serialization logic are not repaired or adapted.

This is an oracle for the JSON sample's synchronization tests, not a dependency
of the Kotowari library or the benchmark. The legacy SpracheJSON `1.0.1`
benchmark remains separate from this source snapshot.

`SpracheJson.Reference.Tests` runs the same test source as `JsonParserのテスト`.
The shared source aliases the parser and model types for each project; inputs,
expected values, and assertions are maintained once. Both suites are included
in `Kotowari.sln` and run with the solution's normal test command.

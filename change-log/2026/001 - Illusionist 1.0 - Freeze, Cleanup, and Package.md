# 001 - Illusionist 1.0 - Freeze, Cleanup, and Package

**Date:** September 25, 2026
**Status:** Complete (stage A; stage B, the shared MCP/REST service, is a separate, not-yet-started
design)
**Impact:** Library freeze, cleanup, packaging -- no generator behavior change

## Overview

A downstream consumer consumes Illusionist from source (vendored as a submodule) in over a dozen
files: training data, calibration stimuli, chart identity, and the symbol fingerprint. The owner
asked for Illusionist to become a shared, agent-facing tool so that no session has to host it
locally. This change-log entry covers stage A: freezing today's generator output as golden
fixtures, documenting the reproducibility contract, cleaning up the repository, fixing a
standalone-build defect, and packaging `Illusionist.Core` 1.0.0. **The generator's own code
(`BrownianBridgeBarSeries` and its `Generator`/`Factory` partials) was not touched at any point in
this work.**

## Golden fixtures (frozen first, before any other change)

Added `tests/Golden/`: `GoldenBarSeriesCase.cs`, `GoldenBarSeriesCases.cs`,
`GoldenBarSeriesGenerator.cs`, `GoldenBarFormatter.cs`, `GeneratorGoldenTests.cs`,
`ReproducibilityScopeTests.cs`, and six committed fixture files under `Fixtures/`. These pin both
of `BrownianBridgeBarSeries`'s entry points (bare-seed `Generator`, symbol-hashed `Factory`) at the
exact parameters a downstream consumer's own regeneration path, window-geometry presets, and
cross-process stability check exercise, plus a spread of seeds and symbols. Added
`.gitattributes` pinning the fixture CSVs to `text eol=lf`, since
`core.autocrlf=true` with no prior `.gitattributes` would otherwise rewrite them to CRLF on
checkout and silently break the byte-for-byte comparison.

## The reproducibility key and its scope

Documented in the README: the full key is `(generator@version, seedMode, seed, symbol, drift,
volatility, anchor, timeframe, bar count or range)`; the current algorithm is `brownian-bridge@1`.
The byte-identity promise is scoped honestly to one .NET major version family on x64;
`ReproducibilityScopeTests` reports the running host's own `RuntimeInformation` beside a golden
result so a future ARM64 or new-.NET-major host can actually prove the scope rather than assume
it.

## Cleanup

- Removed `DebugPriceTest.cs` and `TestAlignment.cs` from the repository root -- unreferenced,
  uncompiled manual debug scratch files predating the current `Bar<T>`/`ISchedule` API (neither
  even compiled against it) and holding no assertion worth preserving.
- Removed `archive/Core/Catalog/SeededBarSeries*.cs` (the pre-Brownian-bridge sine-wave
  generator, superseded by `3f4bfa2`) -- confirmed by repo-wide grep to have zero references
  anywhere outside itself; its history remains in git.
- Added a real `README.md`, replacing the placeholder `# template`.

## Fixed: `dotnet build Illusionist.sln` standalone (NU1008, plus a latent CLI compile defect it was masking)

`Illusionist.CLI.csproj` pinned `Spectre.Console`/`Spectre.Console.Cli` with an inline `Version=`
attribute under Central Package Management, which CPM rejects outright (NU1008) -- fixed by moving
both versions into `Directory.Packages.props`. Because NU1008 has always failed restore before the
CLI project ever reached compilation, fixing it surfaced a second, independent, pre-existing defect
in `GenerateCommand.cs`: a missing `using Electrified.TimeSeries;` (so `BarInterval` didn't
resolve) and two uses of a bare `IBarSeriesFactory` where the type is generic
(`IBarSeriesFactory<T>`), plus direct `bar.Open`/`.High`/`.Low`/`.Close` access on a `Bar<OHLC>`
that only exposes those through `.Data` -- all dating to before `Bar<T>` was genericized
(`3f4bfa2`) and never caught because the project never got past restore. Fixed by minimal,
mechanical type corrections; no behavior changed, since this code path was unreachable in any
build until now.

## Known, unfixed, out of scope: the `Electrified.TimeSeries` `ProjectReference` escape

A fully standalone clone of this repository (no sibling `Electrified.TimeSeries` checkout present)
still cannot build `Illusionist.Core.csproj`, independently of the NU1008 fix above. This is
tracked, understood, owner-accepted debt and explicitly out of this task's scope -- see the
README's own "Known standalone-build limitation" section.

## Packaging

`Illusionist.Core` packs as `Illusionist.Core` 1.0.0, targeting `net10.0`, to
a local package feed. A downstream consumer's own switch from the submodule
source to this package is a separate, later piece of work.

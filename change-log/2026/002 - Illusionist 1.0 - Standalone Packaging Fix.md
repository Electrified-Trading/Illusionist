# 002 - Illusionist 1.0 - Standalone Packaging Fix

**Date:** September 25, 2026
**Status:** Complete (stage A, following an earlier independent review)
**Impact:** Packaging correctness -- no generator behavior change

## Overview

An earlier independent review found that packaging entry 001 (bundling `Electrified.TimeSeries`'s
build output directly into `Illusionist.Core.1.0.0.nupkg`) leaked a downstream consumer's own
internal extension code -- a type that physically lived inside the vendored
`Electrified.TimeSeries` source folder but declared that consumer's own namespace -- into a package
whose README explicitly promises "there is nothing in it to leak." Worse: referencing both
`Illusionist.Core` and the real `Electrified.TimeSeries` package together in one project caused the
.NET SDK's asset-conflict resolution to silently keep only the higher-versioned copy and drop the
bundled one -- a real future divergence between the vendored snapshot and the published package
would fail at runtime (TypeLoad/MissingMethod), not at build time.

## The fix

`Illusionist.Core.csproj`, `Illusionist.CLI.csproj`, and `Illusionist.Tests.csproj` now resolve
`Electrified.TimeSeries` one of two ways, chosen once in the new root `Directory.Build.props`:

- **Sibling mode** (the sibling checkout exists next to this repo -- how a consumer that vendors
  this repository next to an Electrified.TimeSeries source folder resolves it today): the exact
  `ProjectReference` this project has always had, unchanged.
- **Package mode** (no sibling present, or `dotnet pack`, or forced with
  `-p:IllusionistUsePackageReferences=true`): a real `PackageReference` on the published
  `Electrified.TimeSeries` package, pinned to `1.0.3-c1.2` in `Directory.Packages.props` -- verified
  to export the same public surface (`Bar`, `Bar<T>`, `BarInterval`, `OHLC`, `DateRange`,
  `SymbolTimeframe`) without the internal-namespace leakage described above, and to produce
  golden-identical output.

Added a repo `NuGet.config` adding the local feed (`%LOCALAPPDATA%\Electrified\nuget-local`)
alongside nuget.org, so package mode can restore standalone. The bundling workaround
(`CopyProjectReferencesToPackage` / `TargetsForTfmSpecificBuildOutput`) is removed;
`Illusionist.Core.1.0.0.nupkg` now declares a real
`<dependency id="Electrified.TimeSeries" version="1.0.3-c1.2" />` and contains only
`Illusionist.Core.dll`/`.xml`.

**A side effect worth naming plainly**: package mode resolves the standalone half of a known
packaging gap -- `dotnet build Illusionist.sln` now succeeds at 0 warnings / 0 errors directly in
this repository, with no reconstructed sibling needed, because Electrified.TimeSeries now resolves
as a real, restorable package when no sibling is present. The other half (whether a downstream
consumer ever switches its own vendored copy for the published package) is unchanged -- still an
owner decision, for the type-identity reasons already recorded.

## Also (non-blocking)

README now states `DefaultEquitiesSchedule`'s holiday calendar is a fixed, version-scoped constant
covering 2024-2025 only.

## Follow-up (a second review round)

- The new root `Directory.Build.props` now imports any `Directory.Build.props` above it (`GetPathOfFileAbove`). Built as part of a consuming application, that application's own settings (AnalysisLevel, EnforceCodeStyleInBuild, WarningsAsErrors NU1603, SourceLink/symbols) apply to Illusionist again, exactly as before this change. Standalone, nothing exists above it.
- `Illusionist.Core` is versioned **1.0.0-rc.1** while its `Electrified.TimeSeries` dependency is prerelease (1.0.3-c1.2). The NU5104 downgrade is removed rather than kept as a suppression. The `1.0.0` package is withdrawn from the local feed; it was never consumed.

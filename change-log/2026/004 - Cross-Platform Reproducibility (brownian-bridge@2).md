# 004 - Cross-Platform Reproducibility (brownian-bridge@2)

**Date:** September 27, 2026
**Status:** Complete
**Impact:** New generator version. No change to `brownian-bridge@1`'s behavior, registration or
fixtures.

## The finding

Running the service image on Ubuntu 24.04 (x64, .NET 10.0.12), `--self-check` exited 1: 8 of 10
golden cases matched, but `bare-reference-seed12345` and `symbol-hashed-synth-seed12345` did not
(`symbol-hashed-aapl-seed12345` matched; every seed-1 case matched). `BrownianBridgeBarSeries`'s
Gaussian draw is Box-Muller (`Math.Sqrt(-2*Math.Log(u1)) * Math.Cos(2*Math.PI*u2)`), and its price
reconstruction uses `Math.Exp`. `Math.Sqrt` is IEEE-754-mandated correctly rounded on every
conforming platform; `Math.Log`, `Math.Exp` and `Math.Cos` are not -- they call the platform's own C
runtime (UCRT on Windows, glibc on Linux), and a one-ulp difference in a single draw propagates
through the Brownian bridge into a different bar. The golden fixtures were generated and are only
verified on x64 Windows.

## What changed

- **`brownian-bridge@1` is untouched.** Its files, its output, its registration, its fixtures: byte
  for byte identical to before this change (proven by its own existing golden tests, unmodified,
  still passing).
- **`Illusionist.Core.Numerics.DeterministicMath`** (`src/Illusionist.Core/Numerics/`): a
  from-scratch `Log`, `Exp` and `Cos`, built only from `+`, `-`, `*`, `/`, `Math.Sqrt` and exact bit
  manipulation (`BitConverter.DoubleToInt64Bits`/`Int64BitsToDouble`), so every step is
  IEEE-754-mandated to round identically on every conforming platform. Follows `fdlibm`'s (Sun
  Microsystems, 1993) design -- exponent/mantissa decomposition for `Log`, `ln2`-reduction for
  `Exp`, argument reduction plus odd/even polynomial kernels for `Cos` -- but does not reuse its
  numeric constants: every coefficient is an exact rational (`1.0 / (2*j+1)`, `1.0 / n!`, always
  correctly rounded by IEEE division) rather than a memorized minimax-fitted digit string, and `pi`
  and `ln2` are derived once via Machin's formula in exact `System.Numerics.BigInteger` arithmetic
  rather than reusing `fdlibm`'s own hardcoded lookup tables (`two_over_pi[]` in particular -- 396
  words this port's author had no way to verify against a reference in this environment).
  `Cos`'s argument reduction has two paths: a fast Cody-Waite reduction (plain `double` arithmetic)
  for `|x| < 65536`, comfortably covering the generator's real range of `(0, 2*pi)` with margin for
  a "large angle" test sweep; a general `BigInteger`-based Payne-Hanek-style reduction for anything
  larger, correct up to `double.MaxValue`.
- **A real numerical bug, found and fixed during this work, not merely worked around:** a first cut
  of the Cody-Waite reduction (two terms: a truncated `Hi` plus one `Lo` double) measured a ~5e-11
  relative error at `Cos(Math.PI / 2)` against `Math.Cos` -- close to an odd multiple of pi/2, the
  true result is many orders of magnitude smaller than the input, and a single `Lo` term (~53 bits
  relative to its own already-tiny magnitude) was not enough precision to survive that cancellation.
  Carrying a second correction term (`Mid` + `Tail`, both full-precision doubles from the same
  underlying `BigInteger` remainder) fixed it; the same defect and fix applied to `Exp`'s and
  `Log`'s `ln2` reduction, which has the identical shape (multiplying a truncated `Hi` by a count
  that can reach into the thousands). This is exactly why the sweep test compares against
  `Math.Cos`/`Math.Log`/`Math.Exp` over a wide range rather than only a handful of hand-picked
  points -- a spot check at `x = 1.0` would not have found it.
- **`brownian-bridge@2`** (`BrownianBridgeBarSeriesV2` in Core, `BrownianBridgeV2` in the service):
  a deliberate duplicate of `BrownianBridgeBarSeries`/`BrownianBridgeV1`, not a shared
  implementation -- the only lines that differ are the three that call `DeterministicMath` instead
  of `Math`. Registered in `GeneratorCatalog.All` alongside `@1`.
- **The readiness gate now understands a reference platform.** `IGeneratorVersion.ReferencePlatform`
  (nullable, default `null`) lets a version declare the one host platform its golden cases are
  proven on; `@1` declares `windows-x64`, `@2` declares none (proven everywhere by construction).
  `GoldenSelfCheck` computes, per version, whether its cases count toward `SelfCheckReport.IsReady`
  on this host (`HostPlatform.Current` from `RuntimeInformation`) -- a case is still run and its real
  `Match` reported honestly, but a reference-platform version's mismatch elsewhere no longer blocks
  every other version's readiness. `@1` responses served from a non-reference host now carry a
  `platform` warning (`SeriesEnvelope.PlatformScopeWarning`), the same non-breaking channel as the
  existing `calendar`/`extreme_prices` warnings -- never in the rendered bytes.
- **`@2`'s own golden fixtures**, generated on this Windows host, mirroring `@1`'s exact six
  geometries plus the same four extended readiness cases (range mode, JSON, non-default drift, a
  different anchor date) -- 20 total cases across both versions.
- **README**: Reproducibility, self-check and Known limitations sections now describe both versions
  honestly; a License-section note on `fdlibm`'s permission notice and its MIT compatibility.

## Tests

- `tests/Numerics/DeterministicMathTests.*.cs`: a wide sweep (200,000 points each) comparing `Log`,
  `Exp` and `Cos` against this host's own `Math.*` (not a certified reference, the only independent
  implementation available here) -- measured max ULP distance 2 for `Log` and `Cos`, 1 for `Exp`;
  edge cases (zero, both subnormal extremes, NaN, both infinities, the overflow and gradual-underflow
  boundaries, `Cos` at `1e6` through `1e300` and at `double.MaxValue`); and a fixed exact-bits table
  (hex `ulong` bit patterns, not `Math.*`-derived) that is the real cross-platform guard, since the
  sweep is only a quality check against one host's own library.
- **Mutation check**: reverting `DeterministicMath.Cos`'s `KernelCos`/`KernelSin` calls to
  `Math.Cos`/`Math.Sin` (a patch applied, tests run, then reverted with `git checkout --`) makes the
  exact-bits table fail immediately, even on this same Windows machine -- proving the table would
  catch a regression a same-host golden run alone would not.
- `tests/Golden/GeneratorGoldenTestsV2.cs`, `tests/Illusionist.Service.Tests/GoldenManifestTestsV2.cs`,
  `McpGoldenTestsV2.cs`: the same three-layer proof `@1` already had (Core-level fixture match,
  service-manifest-to-fixture consistency, a real MCP round trip), for `@2`.
- `@1`'s own existing tests (`GeneratorGoldenTests`, `GoldenManifestTests`, `McpGoldenTests`,
  `ReproducibilityScopeTests`) pass unmodified -- the evidence that `@1` did not change.

## Verified

- `dotnet build Illusionist.sln -c Release`: 0 warnings, 0 errors (five projects).
- `dotnet test` (both `Illusionist.Tests` and `Illusionist.Service.Tests`, TRX-logged, judged by exit
  code and TRX outcome counts): **148/148** and **118/118**, exit code 0 both times.
- `dotnet run --project src/Illusionist.Service -- --self-check` on this Windows x64 host: 20/20
  cases match (both `@1` and `@2`), `status: ready`, exit 0.
- **Not run here (no Docker in this environment):** the Linux self-check this change exists to fix.
  On the operator's Ubuntu host: `docker build --platform linux/amd64 -t illusionist:local .` then
  `docker run --rm illusionist:local --self-check`. Expect `brownian-bridge@2`'s 10 cases to match
  (their `countsTowardReadiness` is `true` everywhere); `brownian-bridge@1`'s 10 cases are expected to
  still show the same 2 mismatches measured before this change, now with `countsTowardReadiness:
  false` and `referencePlatform: "windows-x64"` -- and the overall `status` should read `ready`
  because only `@2`'s cases (plus `@1`'s 8 that do match) gate it. Exit code 0 is the pass signal, not
  "every case matches."

## UNVERIFIED

- That .NET's JIT never contracts `a * b + c` into a fused multiply-add for ordinary arithmetic
  (stated in `DeterministicMath`'s own class remarks, reasoned from .NET Core 3.0's documented move
  to strict IEEE-754 floating-point semantics) -- no network access in this environment to cite the
  exact specification. `Math.FusedMultiplyAdd` is never called anywhere in this file regardless.
- Whether `System.Numerics.BigInteger`'s built-in explicit conversion to `double` is guaranteed
  bit-identical across every .NET platform. It is pure managed code (not a native call), which is
  the relevant property here, but this was not verified against a specification either.

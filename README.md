# Illusionist

Illusionist generates deterministic, market-like synthetic OHLCV bar series. It never reads real
market data and carries no credentials or network access -- every bar is a pure function of a
handful of numeric parameters, which is what makes it safe to expose as a shared tool: there is
nothing in it to leak.

## What it generates

`Illusionist.Core.Catalog.BrownianBridgeBarSeries` builds one sample path of a Geometric Brownian
Motion price process via a Brownian bridge: Gaussian increments are fixed at power-of-two
"checkpoint" times from a deterministic hash, then bisected down to the exact query timestamp via
midpoint displacement -- the standard, exact method for simulating one Wiener-process sample path.
Both steps are O(log elapsed seconds from the anchor); the generator holds no state between calls,
so any bar at any timestamp can be requested directly, in any order, with no memory of prior
requests. High/Low are a deliberate simplification (a volatility-scaled perturbation around the
bar's open/midpoint/close) rather than sampled from the exact bridge-extremum distribution -- see
`BrownianBridgeBarSeries.Generator`'s own remarks.

## Reproducibility: the key

The current algorithm is named `brownian-bridge@1` (the generator as it exists at commit
`c3bccfb`; a future change to the algorithm that alters output gets a new version number). Given
the same key, the generator produces byte-identical bars on every call, in every process, forever
(see [Scope](#the-scope-of-byte-identical-stated-honestly), below, for exactly how far that
promise extends). The full key is:

```
(generator@version, seedMode, seed, symbol, drift, volatility, anchor, timeframe, bar count or range)
```

- **generator@version** -- `brownian-bridge@1` today.
- **seedMode** -- `bare` or `symbol-hashed` (see [Two entry points](#two-independent-entry-points), below). Every other field's meaning depends on which mode is in play.
- **seed** -- the caller's `int`.
- **symbol** -- only meaningful in `symbol-hashed` mode; absent (plays no role at all) in `bare` mode.
- **drift**, **volatility** -- annualized GBM parameters.
- **anchor** -- the generator's reference timestamp and starting price (`BarAnchor`).
- **timeframe** -- the bar interval and schedule (`ISchedule` + `BarInterval`; today, always the daily `DefaultEquitiesSchedule`).
- **bar count or range** -- how many bars, or which timestamp range, is requested.

Every parameter that changes output is in this tuple. `tests/Golden/GoldenBarSeriesCases.cs` pins
a spread of these values -- read from a downstream consumer's own usage, not invented -- against
committed fixture bytes in `tests/Golden/Fixtures/`; `tests/Golden/GeneratorGoldenTests.cs` is the
comparison itself.

## Two independent entry points

`BrownianBridgeBarSeries` exposes two ways to reach the same underlying `Generator`, and they are
**not interchangeable** -- each is its own reproducibility key, per the `seedMode` field above:

- **`BrownianBridgeBarSeries.Generator`, direct (`bare` seed mode).** Constructed with a plain
  `int` seed; a symbol is never involved. This is the only path a downstream consumer's own
  production regeneration path uses.
- **`BrownianBridgeBarSeries` / `BrownianBridgeBarSeries.Factory` (`symbol-hashed` seed mode).**
  Combines the caller's seed with a DJB2 hash of the symbol
  (`BrownianBridgeBarSeries.GetDeterministicHashCode`) to build the generator's real internal
  seed. This path has its own history: earlier versions combined the seed with
  `string.GetHashCode()`, which .NET randomizes per process by default, so the same
  `(symbol, seed)` pair produced a different path on every process launch. DJB2 fixed that; the
  fix is covered by a genuine cross-process test (spawning real, separate OS processes, since an
  in-process assertion cannot observe per-process hash randomization) in a downstream consumer's
  own cross-process stability check, and by this repo's own golden fixtures
  (`symbol-hashed-synth-seed12345` / `symbol-hashed-aapl-seed12345`).

## The scope of "byte-identical", stated honestly

The generator builds every bar from double-precision transcendentals (`Math.Exp`, `Math.Log`,
`Math.Cos`, `Math.Sqrt` -- see `BrownianBridgeBarSeries.Generator.GaussianAt`/`PriceAt`). IEEE-754
guarantees those operations' inputs and outputs, not that two different runtimes compute the same
intermediate rounding on the way there -- and "runtime" here is not just CPU architecture or .NET
version. Those transcendental functions are calls into the platform's own C math library
(`libm`), not managed .NET code: Windows uses UCRT, Linux uses glibc, and the two are separate
implementations that are not guaranteed to round identically at the last bit for the same double
input. The golden fixtures in this repo are therefore only known to hold **on the exact axis they
were generated and verified on**: one .NET major version family, x64, **Windows/UCRT**. Any of
three things changing -- CPU architecture (ARM64 in particular), .NET major version, or the
operating system / C math library (Linux's glibc vs. Windows' UCRT, even on the same x64 CPU and
.NET version) -- makes byte-identity **UNVERIFIED** until a golden run actually passes there.
`tests/Golden/ReproducibilityScopeTests.cs` exists to make that provable rather than assumed: it
prints the running process's own `RuntimeInformation` (architecture, framework) beside a real
golden comparison's pass/fail, so running that one test on a new host is the actual proof
procedure for that host. The service (below) ships to a Linux container
(`mcr.microsoft.com/dotnet/aspnet:10.0`, glibc) built and verified so far only on Windows: **Linux
x64 byte-identity is UNVERIFIED** until that container's own golden self-check has actually run and
passed -- which is exactly what the self-check's startup gate and `--self-check` command exist to
prove per host, rather than assume from "same CPU architecture."

## Statistical guarantees

Structural correctness (`High >= Open`, determinism, well-formed OHLC) is necessary but not
sufficient -- an earlier generator passed 53 structural tests for a year while emitting
a series with none of the statistical properties of a random walk. `tests/Statistics/` holds a
dedicated battery against that failure mode, each assertion targeting one specific known defect
and each tolerance derived from the statistic's own sampling distribution under a true random
walk, fixed before any corrected generator existed:

- **Continuity** -- consecutive bars chain (the next bar's open equals the prior bar's close).
- **Autocorrelation** -- a random walk's returns are uncorrelated: lag-1 autocorrelation ~= 0 (the
  prior generator measured -0.50, the signature of differenced i.i.d. noise, not a price path).
- **Variance ratio** -- Lo & MacKinlay's VR(k) holds ~= 1 at every horizon for a true random walk
  (the prior generator decayed as ~1/k).
- **Parameter liveness** -- `volatility: 0` must produce zero-variance returns, and increasing
  volatility must increase realized return variance (the prior generator's `volatility` parameter
  did nothing).

## Usage

CLI:

```
dotnet run --project src/Illusionist.CLI -- generate --symbol AAPL --seed 12345 --interval 1d --drift 0.0001 --volatility 0.01
```

Library, bare-seed (never combines a symbol into the seed):

```csharp
var schedule = new DefaultEquitiesScheduleFactory().GetSchedule(BarInterval.Day(1));
var anchor = new BarAnchor(new DateTime(2023, 3, 1, 9, 30, 0), 100m);
var generator = new BrownianBridgeBarSeries.Generator(seed: 12345, schedule, drift: 0.0001, volatility: 0.01, anchor);

var bar = generator.GetBarAt(anchor.Timestamp);
```

Library, symbol-hashed:

```csharp
var factory = new BrownianBridgeBarSeries.Factory(symbol: "AAPL", seed: 12345, drift: 0.0001, volatility: 0.01);
var series = factory.GetSeries(schedule, anchor);
var bars = series.GetBars(anchor.Timestamp).Take(30);
```

## Known limitations

- **`DefaultEquitiesSchedule`'s holiday calendar covers 2024-2025 only.** It is a fixed,
  version-scoped constant (a `HashSet<DateOnly>` literal); outside those two years, holidays are
  not skipped -- the schedule still correctly excludes weekends, but a date that is a real U.S.
  market holiday in, say, 2026 or 2023 is treated as a valid trading day.
- **The `Electrified.TimeSeries` reference, resolved two ways.** `Illusionist.Core.csproj` (and
  `Illusionist.CLI.csproj`/`Illusionist.Tests.csproj`) reference `Electrified.TimeSeries` either as
  a `ProjectReference` to a sibling checkout (sibling mode -- how a consumer that vendors this
  repository next to an Electrified.TimeSeries source folder resolves it today, unchanged) or as a
  real `PackageReference` on the published `Electrified.TimeSeries` package (package mode -- used
  standalone, and always for `dotnet pack`; see `Directory.Build.props`). This means a fully
  standalone clone now builds and packs correctly (resolving the standalone half of a known
  packaging gap); the other half -- whether a downstream consumer ever switches its own vendored
  copy for the published package -- remains an owner decision (type identity: any consumer mixing
  the vendored copy and the published package in the same process must not observe two
  non-identical definitions of `Bar<T>`/`OHLC`, which is exactly why package mode never bundles the
  vendored copy's own DLL).

## Packaging

`Illusionist.Core` packs as a NuGet package (`Illusionist.Core`, targeting `net10.0`). See
`change-log/` for version history.

## Service

`src/Illusionist.Service` is a stateless ASP.NET Core application (`net10.0`) that exposes the
generator above as both an MCP server and a plain REST API. It reads no configuration secrets and
carries no credentials; every instance behaves identically given the same request.

### Endpoints

- MCP: `POST /mcp` (streamable HTTP, stateless). Three tools: `illusionist_generators`,
  `illusionist_describe`, `illusionist_series`. Register a running instance with an MCP client, e.g.:
  ```
  claude mcp add --transport http illusionist https://<your-host>/mcp
  ```
- REST twin: `GET /v1/generators`, `GET /v1/generators/{id}@{version}`, `GET /v1/series?...`,
  `GET /healthz`, `GET /`.

`illusionist_series`/`GET /v1/series` take `generator` and `seed` (required) plus `seedMode`,
`symbol`, `drift`, `volatility`, `anchorDate`, `anchorPrice`, `timeframe`, `count` (or `from`/`to`),
and `format` (`csv` or `json`). Every other field defaults to the golden reference geometry, so
`generator=brownian-bridge@1&seed=1` alone reproduces fixture `bare-reference-seed1` byte for byte.
Results carry a small JSON envelope; a body over 16 KiB comes back as a re-derivable
`illusionist://series?...` resource link/REST path instead of inline bytes -- reading it regenerates
the identical bytes on demand. At most 20,000 bars per call. Unknown or misspelled parameters, and
values outside their documented ranges, are rejected with a deterministic error taxonomy (a stable
`code`, a message, and the offending `parameter`) shared identically across MCP and REST.

The envelope can also carry non-fatal `warnings` -- the request still succeeds and the rendered
bytes are exactly the same either way, warnings or not:

- `calendar: ...` whenever any bar falls outside 2024-2025, since the holiday calendar only covers
  those two years there (weekends are still excluded correctly; a real holiday outside that window
  is not).
- `extreme_prices: ...` whenever the series' highest high exceeds 1,000x `anchorPrice`, or its
  lowest low falls below 1/1,000 of it. Every parameter individually stays inside its documented,
  legal range (drift up to 0.5/yr, volatility up to 1.0/yr, up to 20,000 bars) -- a well-formed
  request can still walk the price to an implausible extreme (a $100 anchor can reach into the
  trillions over enough bars at the legal drift ceiling) without erroring, so this warning is the
  signal, not a rejection. The bounds themselves are deliberately not tightened by this warning;
  that is an owner decision, not this service's to make unilaterally.

### Readiness: the golden self-check

Before Kestrel starts listening, and again on every `GET /healthz` call, the service regenerates ten
readiness cases -- the six golden fixtures above, plus four more that each isolate one axis those
six never vary (a range-mode extent, a JSON body, a non-default drift, a different anchor date) --
and compares their SHA-256 against pinned hashes. A host that cannot reproduce them stays up (so the
failing report stays readable at `/healthz`) but refuses every series-bearing request with
`not_ready` until it passes. Run the same check without starting the web host:

```
dotnet Illusionist.Service.dll --self-check
```

Prints the report JSON and exits 0 on pass, 1 on fail -- Ops's one-line proof that a given host
reproduces this repository's readiness cases.

### Running

```
docker build --platform linux/amd64 -t illusionist:local .
docker run -d --rm -p 8080:8080 illusionist:local
```

Environment variables:

- `ASPNETCORE_HTTP_PORTS` -- the listening port (8080 in the published image).
- `Illusionist__PublicBaseUrl` -- optional, an absolute `http(s)` URL with no trailing slash, used
  to make the envelope's `rest` field and REST's own links absolute. Startup fails fast with a
  clear message if set to anything else. Unset, links are relative (`/v1/series?...`).
- `Logging__LogLevel__Default` -- the standard ASP.NET Core setting.

There is no TLS, no token and no volume: the service is stateless and LAN-only, with TLS expected
to terminate in front of it (e.g. an nginx vhost with a wildcard certificate). Logs are structured
JSON, one object per line, on stdout.

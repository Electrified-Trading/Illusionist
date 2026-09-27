<p align="center">
  <img src="logo.png" alt="Illusionist" width="180">
</p>

<p align="center"><strong>Deterministic, market-like synthetic price data, as a library, a CLI and a network service.</strong></p>

---

Illusionist generates realistic OHLCV bar series (open, high, low, close, volume) that behave like a
real random-walk market, **without ever touching real market data**. Every bar is a pure function of
a handful of numbers. The same request always returns the same bytes, so you can share a result by
sharing its parameters.

Use it to test charting, backtesting and scanning code, to train or evaluate models without leaking
real data, or to give an AI agent an unlimited supply of plausible charts. It holds no credentials
or data files and makes no outbound calls, so it is safe to expose as a shared tool: there is
nothing to leak.

## Illusionist Quick start

### As an agent tool (MCP)

Run the service (see [Running the service](#running-the-service)), then register it with any MCP
client, for example Claude Code:

```bash
claude mcp add --transport http illusionist https://<your-host>/mcp
```

The service exposes three tools:

| Tool | What it does |
|---|---|
| `illusionist_generators` | Lists the available generators and their versions |
| `illusionist_describe` | Explains one generator: its parameters, their ranges, and exactly what it guarantees |
| `illusionist_series` | Generates a bar series |

### Over HTTP

The same three operations are available as a plain REST API that returns byte-identical results:

```bash
curl "http://localhost:8080/v1/series?generator=brownian-bridge@1&seed=1&count=60"
```

Other routes: `GET /v1/generators`, `GET /v1/generators/{id}@{version}`, `GET /healthz`, `GET /`.

### From code or the command line

```bash
dotnet run --project src/Illusionist.CLI -- generate --symbol AAPL --seed 12345 --interval 1d --drift 0.0001 --volatility 0.01
```

```csharp
var schedule = new DefaultEquitiesScheduleFactory().GetSchedule(BarInterval.Day(1));
var anchor = new BarAnchor(new DateTime(2023, 3, 1, 9, 30, 0), 100m);

// Bare seed: the seed alone picks the path.
var generator = new BrownianBridgeBarSeries.Generator(seed: 12345, schedule, drift: 0.0001, volatility: 0.01, anchor);
var bar = generator.GetBarAt(anchor.Timestamp);

// Symbol-hashed: one seed gives a different path per symbol.
var factory = new BrownianBridgeBarSeries.Factory(symbol: "AAPL", seed: 12345, drift: 0.0001, volatility: 0.01);
var bars = factory.GetSeries(schedule, anchor).GetBars(anchor.Timestamp).Take(30);
```

## Series parameters

`illusionist_series` and `GET /v1/series` take the same parameters. Only `generator` and `seed` are
required; the defaults reproduce the reference series exactly.

| Parameter | Default | Range | Meaning |
|---|---|---|---|
| `generator` | *(required)* | `brownian-bridge@1`, `brownian-bridge@2` | Generator and version. Always explicit: a version's output never changes. `@2` is the same model as `@1`, computed with a platform-independent math core (see [Reproducibility](#reproducibility)); prefer it unless you specifically need `@1`'s pinned bytes. |
| `seed` | *(required)* | any 32-bit integer | Different seeds give independent paths. |
| `seedMode` | `bare` | `bare`, `symbol-hashed` | `bare`: the seed alone picks the path. `symbol-hashed`: the seed is combined with a hash of `symbol`. |
| `symbol` | | | A label hashed into the seed (`symbol-hashed` only). Not a lookup: the output has nothing to do with the real instrument. |
| `drift` | `0.0001` | −0.5 to 0.5 | Annualized log drift (0.08 ≈ +8%/yr). |
| `volatility` | `0.01` | 0 to 1 | Annualized volatility; typical stocks are 0.2–0.5. The default is the reference geometry and looks nearly flat. |
| `anchorDate` | `2023-03-01` | 1900-01-01 to 2199-12-31 | Date of the first bar (09:30), where the path starts at `anchorPrice`. Must be a weekday, and not a 2024–2025 U.S. market holiday. |
| `anchorPrice` | `100` | 0.01 to 100,000 | Price at the anchor bar's open. |
| `timeframe` | `1d` | `1d` | Bar interval. `brownian-bridge@1` supports daily bars only. |
| `count` | `60` | 1 to 20,000 | Number of bars starting at the anchor. Use `count` or `from`/`to`, not both. |
| `from`, `to` | | | A date range (inclusive); bars fall on every trading day in it, before or after the anchor. |
| `format` | `csv` | `csv`, `json` | `csv`: `Index,TimestampTicks,Open,High,Low,Close,Volume`. `json`: rows of `[timestamp, open, high, low, close, volume]`. |

Results carry a small JSON envelope. A body over 16 KiB comes back as a re-derivable
`illusionist://series?...` resource link (or REST path) instead of inline bytes; reading it
regenerates the identical bytes on demand. Unknown parameters and out-of-range values are rejected
with a stable error `code`, a message and the offending `parameter`, identically over MCP and REST.

Two non-fatal **warnings** can appear in the envelope. The bytes are the same either way:
- `calendar` — some bars fall outside 2024–2025, the years the holiday calendar covers (see
  [Known limitations](#known-limitations)).
- `extreme_prices` — the series climbed above 1,000× or fell below 1/1,000 of `anchorPrice`. Every
  parameter can be inside its legal range and still walk the price somewhere implausible over enough
  bars; the warning says so rather than refusing the request.

## Reproducibility

The same request returns byte-identical output on every call, in every process. A request is fully
described by:

```
(generator@version, seedMode, seed, symbol, drift, volatility, anchor, timeframe, bar count or range)
```

Every parameter that changes the output is in that tuple. When the algorithm changes in a way that
alters output, it gets a new version number, so old results stay reproducible forever. Committed
golden fixtures (`tests/Golden/`) pin each version's output byte for byte.

**How far "byte-identical" reaches, stated honestly -- and why there are two versions.**
`brownian-bridge@1`'s Gaussian draws use `Math.Exp`, `Math.Log` and `Math.Cos`, which call the
platform's own C math library: UCRT on Windows, glibc on Linux. Different libraries are not
guaranteed to round identically at the last bit, and in practice do not: two of `@1`'s ten golden
cases measured differently on x64 Linux than on the x64 Windows host they were pinned on. So `@1`'s
golden fixtures are **verified on x64 Windows with .NET 10 only** -- its declared *reference
platform* -- and unverified anywhere else.

`brownian-bridge@2` is the identical model (same parameters, same algorithm) with its transcendental
math replaced by [`DeterministicMath`](src/Illusionist.Core/Numerics), a from-scratch `log`/`exp`/`cos`
built only from `+`, `-`, `*`, `/`, `sqrt` (IEEE-754-mandated correctly rounded on every conforming
platform) and exact bit manipulation -- never a call into the platform's own math library. `@2` has
no reference platform: it is expected to reproduce its own golden fixtures on any host, any CPU
architecture, any .NET major version, and its own self-check cases are the proof for a given host,
the same as `@1`'s. `@1` is never edited to fix this -- an existing version's output never changes --
so it stays exactly as published and `@2` is a new, independent registration. Prefer `@2` unless you
specifically depend on `@1`'s pinned bytes.

### The self-check

On startup, and on every `GET /healthz`, the service regenerates ten readiness cases per registered
version and compares them with pinned SHA-256 hashes:
- the six golden fixtures;
- four more cases that each vary one thing the six never do (a date range, a JSON body, a
  non-default drift, a different anchor date).

A version with a declared reference platform (only `@1`, today) is held to its own cases **only on
that platform**: elsewhere, a mismatch is reported honestly in the JSON but does not block
readiness, and any `@1` series response served from a non-reference host carries a `platform`
warning saying so. A version with no reference platform (`@2`) is held to its cases on every host.
The overall `status` is `ready` only when every case that is being held counts as a pass; a host that
cannot reproduce them stays up, so its failure report is readable at `/healthz`, but it refuses
series requests with `not_ready`. To check a host without starting the web server:

```bash
dotnet Illusionist.Service.dll --self-check
```

It prints the report as JSON (each case's `referencePlatform` and `countsTowardReadiness` fields
say whether it is being held on this host) and exits 0 on a pass, 1 on a failure.

## Running the service

```bash
docker build --platform linux/amd64 -t illusionist:local .
docker run --rm illusionist:local --self-check        # prove this host reproduces the fixtures
docker run -d --rm -p 8080:8080 illusionist:local     # then run it
```

The service is stateless: no volume, no token, no secrets, and any number of replicas. Put TLS in
front of it (for example, a reverse proxy). Logs are structured JSON on stdout.

| Environment variable | Purpose |
|---|---|
| `ASPNETCORE_HTTP_PORTS` | Listening port (8080 in the image). |
| `Illusionist__PublicBaseUrl` | Optional absolute `http(s)` URL, no trailing slash, used to make links in results absolute. Startup fails fast if it is malformed. |
| `Logging__LogLevel__Default` | Standard ASP.NET Core log level. |

## How it works

`brownian-bridge@1` and `@2` simulate a **geometric Brownian motion** price path. Gaussian increments
are fixed at power-of-two "checkpoint" times from a deterministic hash, then bisected down to the
exact bar time by midpoint displacement: the standard, exact way to sample one path of a Wiener
process. Each bar costs O(log elapsed time) and needs no state, so any bar at any time can be
requested directly, in any order. Highs and lows are a deliberate simplification: a volatility-scaled
spread around the bar's open, midpoint and close, not a draw from the exact extremum distribution.

The two versions differ only in how the Gaussian draw's `log`, `exp` and `cos` are computed: `@1`
calls `System.Math`; `@2` calls its own `DeterministicMath`, a from-scratch implementation built only
from IEEE-754-exact primitives (see [Reproducibility](#reproducibility)). `DeterministicMath` follows
the public-domain-adjacent `fdlibm` (Sun Microsystems, 1993) design most platform math libraries are
themselves derived from, but derives its own constants (`pi`, `ln2`) from exact
`System.Numerics.BigInteger` arithmetic (Machin's formula) rather than reusing `fdlibm`'s own
hardcoded tables.

Structural checks (well-formed OHLC, determinism) are not enough on their own. An earlier generator
passed dozens of them while producing nothing like a random walk. `tests/Statistics/` checks the
statistics directly:
- **Continuity:** each bar opens at the previous close.
- **No autocorrelation:** returns are uncorrelated from bar to bar.
- **Variance ratio:** it holds near 1 at every horizon, as a random walk requires.
- **Parameter liveness:** zero volatility gives zero variance, and more volatility gives more.

## Building and testing

```bash
dotnet build Illusionist.sln -c Release
dotnet test Illusionist.sln
```

Requires the .NET 10 SDK. `Electrified.TimeSeries` restores from nuget.org; if a sibling
`Electrified.TimeSeries` source checkout sits next to this repository, it is referenced as source
instead (see `Directory.Build.props`).

| Folder | Contents |
|---|---|
| `src/Illusionist.Core` | The generators and bar model |
| `src/Illusionist.Service` | The MCP and REST service, and the self-check |
| `src/Illusionist.CLI` | The command-line tool |
| `tests/` | Unit, golden-fixture, statistical and service tests |

## Known limitations

- **The holiday calendar covers 2024–2025 only.** Outside those years, weekends are still skipped,
  but real U.S. market holidays are treated as trading days.
- **Daily bars only**, in both `brownian-bridge@1` and `@2`.
- **`brownian-bridge@1`'s byte-identity beyond x64 Windows / .NET 10 is unverified** until the
  self-check passes on that host (see [Reproducibility](#reproducibility)); on such a host its
  series responses carry a `platform` warning and its own golden cases do not gate readiness.
  `brownian-bridge@2` has no such limitation by construction, though "by construction" is only as
  good as this repository's own tests -- its self-check still gates readiness on every host.

## License

MIT. See [LICENSE](LICENSE).

`src/Illusionist.Core/Numerics/DeterministicMath` follows the design of `fdlibm` ("Freely
Distributable LIBM", Copyright (C) 1993 by Sun Microsystems, Inc.), whose own notice permits use and
redistribution provided it is preserved -- compatible with this repository's MIT license, and
reproduced in full in that folder's own class remarks. No `fdlibm` source or numeric constant is
copied; only its algorithmic structure is followed (see [How it works](#how-it-works)).

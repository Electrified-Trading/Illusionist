# 003 - Illusionist Service

**Date:** September 26, 2026
**Status:** Complete
**Impact:** New capability -- a stateless ASP.NET Core service exposing the existing generator as
both an MCP server and a REST API. No change to `Illusionist.Core`'s generator behavior.

## Overview

Illusionist is an application, not just a library: `src/Illusionist.Service` (net10.0,
`Microsoft.NET.Sdk.Web`) wraps `BrownianBridgeBarSeries` behind three MCP tools
(`illusionist_generators`, `illusionist_describe`, `illusionist_series`) registered via the
`ModelContextProtocol` C# SDK's low-level handlers, and an identical REST twin
(`/v1/generators`, `/v1/generators/{id}@{version}`, `/v1/series`, `/healthz`, `/`). One parser
(`SeriesQuery` for canonical query mechanics, `SeriesKeyParser` for ordered semantic validation)
backs every surface, so a query string, a resource URI and an MCP tool call all validate and render
identically.

## Reproducibility as a first-class citizen

- **The key.** `(generator@version, seedMode, seed, symbol?, drift, volatility, anchorDate,
  anchorPrice, timeframe, count|from/to, format)` canonicalizes to one fixed-order query string;
  every request comes back with its own canonical `illusionist://series?...` URI, which regenerates
  the identical bytes on demand and is never itself stored.
- **The registry.** `GeneratorCatalog`/`GeneratorRegistry` is an explicit, exact-match lookup by
  `id@version` -- there is no "latest" alias, so an old version stays callable forever and an
  output-changing algorithm always gets a new version number.
- **The golden self-check.** Before Kestrel starts listening, and again on every `/healthz` call,
  the service regenerates all six golden fixtures through its own generate-and-render path and
  compares SHA-256 against the committed hashes. A host that cannot reproduce them stays up (so the
  failing report stays readable) but refuses every series-bearing request with `not_ready` until it
  passes. `dotnet Illusionist.Service.dll --self-check` runs the same check without starting the web
  host, for Ops to run once per host.

## Deterministic error taxonomy

Sixteen stable error codes (`unknown_parameter`, `invalid_generator_ref`, `out_of_range`,
`too_many_bars`, `price_out_of_range`, ...), each with an exact message template and HTTP status,
shared verbatim across REST responses, MCP tool error results, and MCP resource-read protocol
exceptions. Validation runs in one fixed field order, so the first genuine problem in that order is
always the one reported, never an arbitrary one among several.

## Also

- `Directory.Packages.props` gains `ModelContextProtocol`/`ModelContextProtocol.AspNetCore` 2.2.0
  and `Microsoft.AspNetCore.Mvc.Testing` 10.0.10 (held at the existing `Microsoft.Extensions`
  10.0.10 pin).
- `tests/Illusionist.Tests.csproj` excludes `Illusionist.Service.Tests/**` from its own implicit
  glob, so the two test projects' sources and `obj/` never collide.
- `tests/Illusionist.Service.Tests` links (not copies) the existing golden case table from
  `tests/Golden/`, so a fixture or case-table edit cannot silently drift between the two projects.
- A `Dockerfile`/`.dockerignore` at the repository root; `docker build --platform linux/amd64` was
  not run in this environment (no Docker daemon available), so the image build itself is UNVERIFIED
  here -- only the Dockerfile's text and the `dotnet publish` command it wraps were exercised.

## Verified

- `dotnet build Illusionist.sln -c Release`: 0 warnings, 0 errors (four projects).
- `dotnet test Illusionist.sln -c Release --no-build`: `Illusionist.Tests` 72/72 (unchanged),
  `Illusionist.Service.Tests` 99/99.
- `dotnet run --project src/Illusionist.Service -- --self-check` on this Windows x64 host: 6/6
  golden cases match, exit 0.
- A running instance, hit manually with `curl` and a real `ModelContextProtocol` client: REST and
  MCP both reproduce the golden fixtures byte for byte, and the canonical `Illusionist-Uri` header
  matches the service design's own worked example character for character.

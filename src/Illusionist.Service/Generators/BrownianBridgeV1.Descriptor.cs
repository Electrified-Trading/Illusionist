namespace Illusionist.Service.Generators;

public sealed partial class BrownianBridgeV1
{
	/// <inheritdoc />
	public GeneratorDescriptor Descriptor { get; } = new(
		Ref: new GeneratorRef("brownian-bridge", 1),
		Summary: "A Brownian-bridge Geometric Brownian Motion price path: deterministic, market-like, never real market data.",
		Timeframes: ["1d"],
		Parameters:
		[
			new ParameterSpec("generator", "string", Default: null, Minimum: null, Maximum: null, Enum: null, Pattern: null,
				Required: true,
				Description: "Generator and version, e.g. 'brownian-bridge@1'. Always explicit: a version's output never changes."),

			new ParameterSpec("seedMode", "string", Default: "\"bare\"", Minimum: null, Maximum: null,
				Enum: ["bare", "symbol-hashed"], Pattern: null, Required: false,
				Description: "'bare': the seed alone picks the path. 'symbol-hashed': the seed is combined with a hash of symbol, so one seed gives a different path per symbol."),

			new ParameterSpec("seed", "integer", Default: null, Minimum: null, Maximum: null, Enum: null, Pattern: null,
				Required: true,
				Description: "Any 32-bit integer; different seeds give independent paths."),

			new ParameterSpec("symbol", "string", Default: null, Minimum: null, Maximum: null, Enum: null,
				Pattern: "^[A-Za-z0-9._-]{1,32}$", Required: false,
				Description: "A label hashed into the seed (symbol-hashed mode only). Not a lookup: output has nothing to do with the real instrument."),

			new ParameterSpec("drift", "number", Default: "0.0001", Minimum: "-0.5", Maximum: "0.5", Enum: null, Pattern: null,
				Required: false,
				Description: "Annualized log drift (0.08 approximately +8%/yr)."),

			new ParameterSpec("volatility", "number", Default: "0.01", Minimum: "0", Maximum: "1", Enum: null, Pattern: null,
				Required: false,
				Description: "Annualized volatility; typical stocks 0.2-0.5. The default 0.01 is the reference geometry and looks nearly flat."),

			new ParameterSpec("anchorDate", "string", Default: "\"2023-03-01\"", Minimum: "\"1900-01-01\"", Maximum: "\"2199-12-31\"",
				Enum: null, Pattern: null, Required: false,
				Description: "Date of the first bar (09:30), where the path starts at anchorPrice. Must be a weekday that is not a 2024-2025 U.S. market holiday."),

			new ParameterSpec("anchorPrice", "number", Default: "100", Minimum: "0.01", Maximum: "100000", Enum: null, Pattern: null,
				Required: false,
				Description: "Price at the anchor bar's open."),

			new ParameterSpec("timeframe", "string", Default: "\"1d\"", Minimum: null, Maximum: null, Enum: ["1d"], Pattern: null,
				Required: false,
				Description: "Bar interval. brownian-bridge@1 supports '1d' only."),

			new ParameterSpec("count", "integer", Default: "60", Minimum: "1", Maximum: "20000", Enum: null, Pattern: null,
				Required: false,
				Description: "Number of bars starting at the anchor. Use count or from/to, not both."),

			new ParameterSpec("from", "string", Default: null, Minimum: null, Maximum: null, Enum: null, Pattern: null,
				Required: false,
				Description: "First date of a range (inclusive). Bars fall on every trading day in [from, to], before or after the anchor."),

			new ParameterSpec("to", "string", Default: null, Minimum: null, Maximum: null, Enum: null, Pattern: null,
				Required: false,
				Description: "Last date of the range (inclusive)."),

			new ParameterSpec("format", "string", Default: "\"csv\"", Minimum: null, Maximum: null, Enum: ["csv", "json"], Pattern: null,
				Required: false,
				Description: "'csv': Index,TimestampTicks,Open,High,Low,Close,Volume (the golden layout; TimestampTicks are .NET ticks). 'json': rows of [timestamp, open, high, low, close, volume] with ISO timestamps."),
		]);

	/// <inheritdoc />
	public IReadOnlyList<string> Guarantees { get; } =
	[
		"Same key, same bytes: identical arguments return byte-identical output on every call, in every process, on any host whose golden self-check passes -- proven directly for the drift, anchor date, extent shape (count and range) and body format (csv and json) values the self-check's own cases exercise; other combinations run the identical code path but are not separately proven on that host (see Scope).",
		"Stateless random access: a bar depends only on the key and its own timestamp, so range and count requests agree on every date they share.",
		"Bars chain: every bar's open equals the previous bar's close.",
		"No real market data: the symbol is only a hash input.",
		"Versions are frozen: a different algorithm gets a new version; this one never changes.",
	];

	/// <inheritdoc />
	public string Scope { get; } =
		"Byte-identity is proven per host by the golden self-check: 10 cases compared by SHA-256 -- the 6 pinned to this repository's committed fixtures (all csv, count mode, drift 0.0001, anchor 2023-03-01), plus 4 that each isolate one axis those six never vary: one range-mode extent, one json body, one non-default drift, and one different anchor date. That spans both extent shapes, both body formats, two drift magnitudes and two anchor dates -- not every legal combination of every parameter. A host that passes all 10 could still diverge on a combination none of them reaches (a platform math-library rounding difference that only appears at another elapsed-time magnitude, for instance). ARM64, other .NET major versions, other operating systems (Linux's glibc vs. Windows' UCRT compute the same transcendental functions differently at the last bit -- see the README) and any host whose check fails are unverified.";

	/// <inheritdoc />
	public IReadOnlyList<string> Limitations { get; } =
	[
		"The holiday calendar covers 2024-2025 only.",
		"High and Low are a volatility-scaled perturbation around open, midpoint and close, not exact bridge extrema.",
		"Drift and volatility accrue per elapsed wall-clock second over 365.25-day years, including nights and weekends.",
		"Timestamps are exchange-local wall-clock times (09:30 daily) with no UTC offset.",
	];
}

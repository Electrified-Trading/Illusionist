namespace Illusionist.Service.Contracts;

/// <summary>
/// The full reproducibility key: given the same <see cref="SeriesKey"/>, a generator produces
/// byte-identical output on every call, in every process, on any host whose golden self-check
/// passes. <c>SeriesKeyParser</c> is the only place that constructs one from untrusted input, and
/// it normalizes <see cref="AnchorPrice"/>'s decimal scale (e.g. <c>100.00</c> becomes <c>100</c>)
/// before storing it here, because decimal scale otherwise leaks into output bytes.
/// </summary>
/// <param name="Generator">The generator and version, e.g. <c>brownian-bridge@1</c>.</param>
/// <param name="SeedMode">Whether <see cref="Symbol"/> participates in the seed.</param>
/// <param name="Seed">The caller's 32-bit seed.</param>
/// <param name="Symbol">The symbol hashed into the seed; only set when <see cref="SeedMode"/> is <see cref="Contracts.SeedMode.SymbolHashed"/>.</param>
/// <param name="Drift">Annualized log drift.</param>
/// <param name="Volatility">Annualized volatility.</param>
/// <param name="AnchorDate">The first bar's date.</param>
/// <param name="AnchorPrice">The anchor bar's open, normalized to minimal decimal scale.</param>
/// <param name="Timeframe">The bar interval, e.g. <c>1d</c>.</param>
/// <param name="Extent">How many bars, or which date range, the request spans.</param>
/// <param name="Format">The rendered body's wire layout.</param>
public sealed record SeriesKey(
	GeneratorRef Generator,
	SeedMode SeedMode,
	int Seed,
	string? Symbol,
	double Drift,
	double Volatility,
	DateOnly AnchorDate,
	decimal AnchorPrice,
	string Timeframe,
	SeriesExtent Extent,
	SeriesFormat Format);

namespace Illusionist.Service.Generators;

/// <summary>
/// One golden fixture case a generator version must reproduce byte for byte: the parameters that
/// select it, and the committed fixture's SHA-256. All of <see cref="BrownianBridgeV1"/>'s cases
/// share <c>anchorDate=2023-03-01</c>, <c>timeframe=1d</c> and <c>format=csv</c> (see
/// <see cref="BrownianBridgeV1.GoldenCases"/>), so those fields are not repeated here.
/// </summary>
/// <param name="Name">The fixture's name (matches a file under <c>tests/Golden/Fixtures/</c>).</param>
/// <param name="SeedMode">Which entry point this case exercises.</param>
/// <param name="Seed">The generator's seed.</param>
/// <param name="Symbol">The symbol, when <paramref name="SeedMode"/> is <see cref="Contracts.SeedMode.SymbolHashed"/>; otherwise <see langword="null"/>.</param>
/// <param name="AnchorDate">The anchor bar's date.</param>
/// <param name="AnchorPrice">The anchor bar's open.</param>
/// <param name="Drift">Annualized GBM drift.</param>
/// <param name="Volatility">Annualized GBM volatility.</param>
/// <param name="Timeframe">The bar interval.</param>
/// <param name="Count">The number of bars this case generates in count mode. Ignored when <paramref name="RangeTo"/> is set.</param>
/// <param name="Format">The rendered body's wire layout.</param>
/// <param name="Sha256">The pinned lowercase hex SHA-256 of this case's exact UTF-8 bytes -- for the original six, the committed fixture's own hash; for any case added since, a hash frozen the same way (computed once, from the real generate-and-render path, and never regenerated).</param>
/// <param name="RangeTo">
/// When set, this case pins a range-mode key: <c>from</c> is <paramref name="AnchorDate"/> and
/// <c>to</c> is this value, instead of a <paramref name="Count"/>-mode key. <see langword="null"/>
/// (the default) keeps count mode, so every pre-existing case is unaffected.
/// </param>
public sealed record GoldenCase(
	string Name,
	SeedMode SeedMode,
	int Seed,
	string? Symbol,
	DateOnly AnchorDate,
	decimal AnchorPrice,
	double Drift,
	double Volatility,
	string Timeframe,
	int Count,
	SeriesFormat Format,
	string Sha256,
	DateOnly? RangeTo = null)
{
	/// <summary>Builds the exact <see cref="SeriesKey"/> this case pins -- count mode, or range mode when <see cref="RangeTo"/> is set.</summary>
	public SeriesKey ToKey(GeneratorRef generatorRef)
	{
		SeriesExtent extent = RangeTo is { } to
			? new SeriesExtent.Range(AnchorDate, to)
			: new SeriesExtent.Count(Count);

		return new(generatorRef, SeedMode, Seed, Symbol, Drift, Volatility, AnchorDate, AnchorPrice, Timeframe, extent, Format);
	}
}

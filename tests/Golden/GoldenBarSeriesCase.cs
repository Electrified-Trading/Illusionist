namespace Illusionist.Tests.Golden;

/// <summary>
/// Identifies which of <see cref="Illusionist.Core.Catalog.BrownianBridgeBarSeries"/>'s two
/// independent entry points a golden case exercises. Both are load-bearing for a downstream
/// consumer today (see <see cref="GoldenBarSeriesCases"/> for the exact call sites) and neither
/// can stand in for the other: <see cref="BareSeed"/> never touches a symbol at all, and
/// <see cref="SymbolHashed"/>'s DJB2 hashing of the symbol is a separate code path with its own
/// history of process-instability defects.
/// </summary>
internal enum GoldenGenerationPath
{
	/// <summary>
	/// <see cref="Illusionist.Core.Catalog.BrownianBridgeBarSeries.Generator"/> constructed
	/// directly with a bare <see cref="int"/> seed -- never combined with a symbol.
	/// </summary>
	BareSeed,

	/// <summary>
	/// <see cref="Illusionist.Core.Catalog.BrownianBridgeBarSeries.Factory"/>, which combines
	/// the seed with a DJB2 hash of <see cref="GoldenBarSeriesCase.Symbol"/>.
	/// </summary>
	SymbolHashed,
}

/// <summary>
/// One golden fixture case: the exact generation parameters that reproduce it, plus the name of
/// the committed fixture file that pins its output bytes. See <see cref="GoldenBarSeriesCases"/>
/// for the full, documented list and <see cref="GeneratorGoldenTests"/> for the comparison
/// itself.
/// </summary>
/// <param name="FixtureName">The fixture file's name, without its `.csv` extension.</param>
/// <param name="Path">Which <see cref="Illusionist.Core.Catalog.BrownianBridgeBarSeries"/> entry point this case exercises.</param>
/// <param name="Seed">The generator's seed -- combined with a symbol hash only when <paramref name="Path"/> is <see cref="GoldenGenerationPath.SymbolHashed"/>.</param>
/// <param name="Symbol">The symbol, when <paramref name="Path"/> is <see cref="GoldenGenerationPath.SymbolHashed"/>; otherwise <see langword="null"/> -- a bare-seed case never has one.</param>
/// <param name="AnchorPrice">The generator's anchor price. The anchor timestamp is shared by every case (see <see cref="GoldenBarSeriesCases"/>).</param>
/// <param name="Drift">Annualized GBM drift.</param>
/// <param name="Volatility">Annualized GBM volatility.</param>
/// <param name="BarCount">The number of bars this case generates.</param>
internal readonly record struct GoldenBarSeriesCase(
	string FixtureName,
	GoldenGenerationPath Path,
	int Seed,
	string? Symbol,
	decimal AnchorPrice,
	double Drift,
	double Volatility,
	int BarCount);

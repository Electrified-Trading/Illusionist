namespace Illusionist.Tests.Golden;

/// <summary>
/// <c>brownian-bridge@2</c>'s own golden case list -- the same six geometries as
/// <see cref="GoldenBarSeriesCases"/> (same seeds, anchors, drift, volatility, bar counts and
/// symbols), so the only thing this pins is whether <see cref="Illusionist.Core.Catalog.BrownianBridgeBarSeriesV2"/>
/// reproduces its own committed fixtures -- not whether it agrees with <c>@1</c>'s bytes, which it
/// is not expected to (see <see cref="Illusionist.Core.Numerics.DeterministicMath"/>'s class remarks).
/// </summary>
internal static class GoldenBarSeriesCasesV2
{
	/// <summary>The full <c>@2</c> golden case list, in fixture-file order -- see <see cref="GoldenBarSeriesCases.All"/> for why each geometry was chosen.</summary>
	public static IReadOnlyList<GoldenBarSeriesCase> All { get; } =
	[
		new("bare-reference-seed1-v2", GoldenGenerationPath.BareSeed, Seed: 1, Symbol: null,
			AnchorPrice: 100m, Drift: 0.0001, Volatility: 0.01, BarCount: 60),

		new("bare-reference-seed12345-v2", GoldenGenerationPath.BareSeed, Seed: 12345, Symbol: null,
			AnchorPrice: 100m, Drift: 0.0001, Volatility: 0.01, BarCount: 60),

		new("bare-short-high-volatility-seed1-v2", GoldenGenerationPath.BareSeed, Seed: 1, Symbol: null,
			AnchorPrice: 50m, Drift: 0.0001, Volatility: 0.03, BarCount: 25),

		new("bare-long-low-volatility-seed1-v2", GoldenGenerationPath.BareSeed, Seed: 1, Symbol: null,
			AnchorPrice: 300m, Drift: 0.0001, Volatility: 0.005, BarCount: 120),

		new("symbol-hashed-synth-seed12345-v2", GoldenGenerationPath.SymbolHashed, Seed: 12345, Symbol: "SYNTH",
			AnchorPrice: 100m, Drift: 0.0001, Volatility: 0.01, BarCount: 30),

		new("symbol-hashed-aapl-seed12345-v2", GoldenGenerationPath.SymbolHashed, Seed: 12345, Symbol: "AAPL",
			AnchorPrice: 100m, Drift: 0.0001, Volatility: 0.01, BarCount: 30),
	];
}

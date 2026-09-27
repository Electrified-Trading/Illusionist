namespace Illusionist.Tests.Golden;

/// <summary>
/// The fixed list of golden cases: the exact generation parameters a downstream consumer's own
/// synthetic-chart usage exercises, plus enough of a spread beyond that to exercise both
/// generation paths. Every value here was read from that consumer's own source, not invented --
/// see the comment on each case below for its origin.
/// </summary>
/// <remarks>
/// All cases share one anchor timestamp (2023-03-01 09:30) and one daily equities schedule --
/// the only schedule a downstream consumer's own regeneration path can resolve today. Only the
/// anchor *price*, drift, volatility, bar count, seed and (for the symbol-hashed cases) symbol
/// vary case to case -- see <see cref="GeneratorGoldenTests"/> for where the shared timestamp and
/// schedule are constructed.
/// </remarks>
internal static class GoldenBarSeriesCases
{
	/// <summary>
	/// The anchor timestamp every case shares -- a downstream consumer's own pinned reference,
	/// 2023-03-01 market open.
	/// </summary>
	public static readonly DateTime ReferenceAnchorTimestamp = new(2023, 3, 1, 9, 30, 0);

	/// <summary>
	/// The full golden case list, in fixture-file order.
	/// </summary>
	public static IReadOnlyList<GoldenBarSeriesCase> All { get; } =
	[
		// Bare-seed path (BrownianBridgeBarSeries.Generator, direct -- never combined with a
		// symbol). This is the only path a downstream consumer's own production regeneration
		// path ever calls.

		// The reference geometry: anchor 100, drift 0.0001, volatility 0.01, 60 daily bars --
		// a downstream consumer's own pinned defaults -- at an arbitrary-but-fixed seed.
		new("bare-reference-seed1", GoldenGenerationPath.BareSeed, Seed: 1, Symbol: null,
			AnchorPrice: 100m, Drift: 0.0001, Volatility: 0.01, BarCount: 60),

		// The same reference geometry at a second seed (12345 -- the same seed a downstream
		// consumer's own cross-process stability check uses), so the golden set covers more
		// than one seed against the geometry a downstream consumer exercises most.
		new("bare-reference-seed12345", GoldenGenerationPath.BareSeed, Seed: 12345, Symbol: null,
			AnchorPrice: 100m, Drift: 0.0001, Volatility: 0.01, BarCount: 60),

		// A short high-volatility window: 3x reference volatility, half the reference anchor
		// price, a quarter of the reference bar count.
		new("bare-short-high-volatility-seed1", GoldenGenerationPath.BareSeed, Seed: 1, Symbol: null,
			AnchorPrice: 50m, Drift: 0.0001, Volatility: 0.03, BarCount: 25),

		// A long low-volatility window: half the reference volatility, 3x the reference anchor
		// price, double the reference bar count.
		new("bare-long-low-volatility-seed1", GoldenGenerationPath.BareSeed, Seed: 1, Symbol: null,
			AnchorPrice: 300m, Drift: 0.0001, Volatility: 0.005, BarCount: 120),

		// Symbol-hashed path (BrownianBridgeBarSeries.Factory, which combines the seed with a
		// DJB2 hash of the symbol). A downstream consumer's own production regeneration path
		// deliberately never routes through this path, but a downstream consumer's own
		// cross-process stability check depends on it directly, through a dedicated CLI
		// command -- exactly the parameters reproduced here.

		// The symbol-hashed path's own fixed case, from a downstream consumer's cross-process
		// stability check: symbol "SYNTH", seed 12345, 30 bars, at the reference
		// drift/volatility/anchor price.
		new("symbol-hashed-synth-seed12345", GoldenGenerationPath.SymbolHashed, Seed: 12345, Symbol: "SYNTH",
			AnchorPrice: 100m, Drift: 0.0001, Volatility: 0.01, BarCount: 30),

		// The same seed and geometry under a different symbol ("AAPL") -- the same pairing a
		// downstream consumer's own cross-process stability check uses to prove the fixture
		// actually exercises the symbol-hashing path (its output must differ from
		// symbol-hashed-synth-seed12345's).
		new("symbol-hashed-aapl-seed12345", GoldenGenerationPath.SymbolHashed, Seed: 12345, Symbol: "AAPL",
			AnchorPrice: 100m, Drift: 0.0001, Volatility: 0.01, BarCount: 30),
	];
}

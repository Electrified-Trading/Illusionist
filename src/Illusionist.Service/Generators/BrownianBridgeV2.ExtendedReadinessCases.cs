namespace Illusionist.Service.Generators;

public sealed partial class BrownianBridgeV2
{
	/// <summary>
	/// Four cases beyond <see cref="GoldenCases"/>, mirroring <see cref="BrownianBridgeV1.ReadinessCases"/>'s
	/// own four exactly (same axis each isolates: a range-mode extent, the JSON body format, a
	/// non-default drift, and a non-reference anchor date) so <c>@2</c>'s readiness proof spans the
	/// same surface <c>@1</c>'s does. None is tied to a committed fixture file -- each hash below was
	/// computed once from this service's own generate-and-render path.
	/// </summary>
	private static IReadOnlyList<GoldenCase> ExtendedReadinessCases =>
	[
		new("readiness-range-seed1-v2", SeedMode.Bare, Seed: 1, Symbol: null,
			AnchorDate: GoldenAnchorDate, AnchorPrice: 100m, Drift: 0.0001, Volatility: 0.01,
			Timeframe: "1d", Count: 0, Format: SeriesFormat.Csv, RangeTo: new DateOnly(2023, 3, 10),
			Sha256: "4697424c0a185c491b33e2326cd5e1770e0fc7ad228d4be4e932cecae1cc172e"),

		new("readiness-json-seed1-v2", SeedMode.Bare, Seed: 1, Symbol: null,
			AnchorDate: GoldenAnchorDate, AnchorPrice: 100m, Drift: 0.0001, Volatility: 0.01,
			Timeframe: "1d", Count: 60, Format: SeriesFormat.Json,
			Sha256: "eb6cd16ac81055f79d7ad8633b12925b86771ea2e804b7e41a77a6e255c38005"),

		new("readiness-nondefault-drift-seed1-v2", SeedMode.Bare, Seed: 1, Symbol: null,
			AnchorDate: GoldenAnchorDate, AnchorPrice: 100m, Drift: 0.05, Volatility: 0.01,
			Timeframe: "1d", Count: 60, Format: SeriesFormat.Csv,
			Sha256: "5a6c12b6e195615566f44bfa8323e90e965aef624021b2f3e9b51dbf3189458b"),

		new("readiness-different-anchor-seed1-v2", SeedMode.Bare, Seed: 1, Symbol: null,
			AnchorDate: new DateOnly(2024, 6, 3), AnchorPrice: 100m, Drift: 0.0001, Volatility: 0.01,
			Timeframe: "1d", Count: 60, Format: SeriesFormat.Csv,
			Sha256: "f132c5ee78e0ad1344067bbc6b8ccd32639c6a3958ae523b928c36a26b05f6db"),
	];

	/// <inheritdoc />
	public IReadOnlyList<GoldenCase> ReadinessCases => [.. GoldenCases, .. ExtendedReadinessCases];
}

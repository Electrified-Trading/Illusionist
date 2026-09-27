namespace Illusionist.Service.Generators;

public sealed partial class BrownianBridgeV1
{
	/// <summary>The anchor date every golden case shares (2023-03-01, market open).</summary>
	public static readonly DateOnly GoldenAnchorDate = new(2023, 3, 1);

	/// <summary>
	/// The six fixtures under <c>tests/Golden/Fixtures/</c>, reachable through this service's own
	/// generate-and-render path. All six share <see cref="GoldenAnchorDate"/>, <c>timeframe=1d</c>
	/// and <c>format=csv</c> -- see <c>GoldenManifestTests</c> for the one-to-one check against
	/// <c>GoldenBarSeriesCases.All</c>.
	/// </summary>
	/// <remarks>
	/// A computed property, not an auto-property with an initializer: an instance field initializer
	/// here would run as part of this type's single merged constructor, in file-declaration order
	/// across every partial part -- the same cross-file ordering hazard <see cref="ExtendedReadinessCases"/>'s
	/// own remarks describe (there, a confirmed defect; here, not observed to fail today only
	/// because this file happens to sort before <c>BrownianBridgeV1._.cs</c>, whose <c>Instance</c>
	/// static initializer is what actually triggers construction). A property getter is evaluated on
	/// demand, always after the type is fully constructed, so it carries no such dependency on file
	/// order at all.
	/// </remarks>
	public IReadOnlyList<GoldenCase> GoldenCases =>
	[
		new("bare-reference-seed1", SeedMode.Bare, Seed: 1, Symbol: null,
			AnchorDate: GoldenAnchorDate, AnchorPrice: 100m, Drift: 0.0001, Volatility: 0.01,
			Timeframe: "1d", Count: 60, Format: SeriesFormat.Csv,
			Sha256: "a868f4caae48283d3f7a7fa81526448b03992601607838c82e7743f48c8b87ec"),

		new("bare-reference-seed12345", SeedMode.Bare, Seed: 12345, Symbol: null,
			AnchorDate: GoldenAnchorDate, AnchorPrice: 100m, Drift: 0.0001, Volatility: 0.01,
			Timeframe: "1d", Count: 60, Format: SeriesFormat.Csv,
			Sha256: "fc207a067780a5b4fb005eebf602e740a97c9869ceaa766ad2c7cd69fb235673"),

		new("bare-short-high-volatility-seed1", SeedMode.Bare, Seed: 1, Symbol: null,
			AnchorDate: GoldenAnchorDate, AnchorPrice: 50m, Drift: 0.0001, Volatility: 0.03,
			Timeframe: "1d", Count: 25, Format: SeriesFormat.Csv,
			Sha256: "6f4f72f5a15ca720c84a1ca847871b407de2d5bf8b130ccad20441420a993365"),

		new("bare-long-low-volatility-seed1", SeedMode.Bare, Seed: 1, Symbol: null,
			AnchorDate: GoldenAnchorDate, AnchorPrice: 300m, Drift: 0.0001, Volatility: 0.005,
			Timeframe: "1d", Count: 120, Format: SeriesFormat.Csv,
			Sha256: "476623c2e0332aa3d98de622150a71b8d7da21d1892e6660e1325fdff0ed8723"),

		new("symbol-hashed-synth-seed12345", SeedMode.SymbolHashed, Seed: 12345, Symbol: "SYNTH",
			AnchorDate: GoldenAnchorDate, AnchorPrice: 100m, Drift: 0.0001, Volatility: 0.01,
			Timeframe: "1d", Count: 30, Format: SeriesFormat.Csv,
			Sha256: "de3dae1b74beb517a6a8c904001551796a5bc7d46669e720d41a4844bc587ba7"),

		new("symbol-hashed-aapl-seed12345", SeedMode.SymbolHashed, Seed: 12345, Symbol: "AAPL",
			AnchorDate: GoldenAnchorDate, AnchorPrice: 100m, Drift: 0.0001, Volatility: 0.01,
			Timeframe: "1d", Count: 30, Format: SeriesFormat.Csv,
			Sha256: "6080a227ba481d2a2ebcd7ae1619d60d74ac068d871df8d916b33084bf012fe0"),
	];
}

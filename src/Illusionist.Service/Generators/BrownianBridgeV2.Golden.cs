namespace Illusionist.Service.Generators;

public sealed partial class BrownianBridgeV2
{
	/// <summary>The anchor date every golden case shares (2023-03-01, market open) -- same as <see cref="BrownianBridgeV1.GoldenAnchorDate"/>.</summary>
	public static readonly DateOnly GoldenAnchorDate = new(2023, 3, 1);

	/// <summary>
	/// The six fixtures under <c>tests/Golden/Fixtures/</c> (the <c>-v2</c> files), reachable
	/// through this service's own generate-and-render path -- the same six geometries as
	/// <see cref="BrownianBridgeV1.GoldenCases"/>, so the only difference between a <c>@1</c> and
	/// <c>@2</c> case with the same name (minus the <c>-v2</c> suffix) is the generator itself. A
	/// computed property, not an auto-property with an initializer: see
	/// <see cref="BrownianBridgeV1.GoldenCases"/>'s own remarks for the cross-file static
	/// initializer ordering hazard this avoids.
	/// </summary>
	public IReadOnlyList<GoldenCase> GoldenCases =>
	[
		new("bare-reference-seed1-v2", SeedMode.Bare, Seed: 1, Symbol: null,
			AnchorDate: GoldenAnchorDate, AnchorPrice: 100m, Drift: 0.0001, Volatility: 0.01,
			Timeframe: "1d", Count: 60, Format: SeriesFormat.Csv,
			Sha256: "ce69d6af95a78d3a814557e4e5878095c7edbe150b43048fdba32a8d5ce3de54"),

		new("bare-reference-seed12345-v2", SeedMode.Bare, Seed: 12345, Symbol: null,
			AnchorDate: GoldenAnchorDate, AnchorPrice: 100m, Drift: 0.0001, Volatility: 0.01,
			Timeframe: "1d", Count: 60, Format: SeriesFormat.Csv,
			Sha256: "a02a807d56d91f131b7654fcae561c2d249dea5f08ef0608c1954f2ecc134a6d"),

		new("bare-short-high-volatility-seed1-v2", SeedMode.Bare, Seed: 1, Symbol: null,
			AnchorDate: GoldenAnchorDate, AnchorPrice: 50m, Drift: 0.0001, Volatility: 0.03,
			Timeframe: "1d", Count: 25, Format: SeriesFormat.Csv,
			Sha256: "3237c1f96f9e2887c7666fd8c95ca9c854dfc8d8ff1df08bb9e1d762e62e06f3"),

		new("bare-long-low-volatility-seed1-v2", SeedMode.Bare, Seed: 1, Symbol: null,
			AnchorDate: GoldenAnchorDate, AnchorPrice: 300m, Drift: 0.0001, Volatility: 0.005,
			Timeframe: "1d", Count: 120, Format: SeriesFormat.Csv,
			Sha256: "869f41f87387a461dbb10dca23654cdd611e51df4901ec0d3d61ea515014455f"),

		new("symbol-hashed-synth-seed12345-v2", SeedMode.SymbolHashed, Seed: 12345, Symbol: "SYNTH",
			AnchorDate: GoldenAnchorDate, AnchorPrice: 100m, Drift: 0.0001, Volatility: 0.01,
			Timeframe: "1d", Count: 30, Format: SeriesFormat.Csv,
			Sha256: "5363a5c6ad26db4aa0fca1bed9e71292b89efb966dbbf60983954e7e2dec870c"),

		new("symbol-hashed-aapl-seed12345-v2", SeedMode.SymbolHashed, Seed: 12345, Symbol: "AAPL",
			AnchorDate: GoldenAnchorDate, AnchorPrice: 100m, Drift: 0.0001, Volatility: 0.01,
			Timeframe: "1d", Count: 30, Format: SeriesFormat.Csv,
			Sha256: "536bda69040d68a1e43ebf9da7455fb9bd6990dee715a6bf097aeeb39466499c"),
	];
}

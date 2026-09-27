namespace Illusionist.Service.Generators;

public sealed partial class BrownianBridgeV1
{
	/// <summary>
	/// Four cases beyond <see cref="GoldenCases"/>, each isolating one axis the six fixture-backed
	/// cases never vary: a range-mode extent, the JSON body format, a non-default drift, and a
	/// non-reference anchor date. None is tied to a committed fixture file -- each hash below was
	/// computed once from this service's own generate-and-render path and is frozen the same way
	/// the original six were, so the readiness gate's proof actually spans what
	/// <see cref="Guarantees"/> claims it does.
	/// </summary>
	/// <remarks>
	/// A computed property, not a <c>static readonly</c> field: a field initializer here would run
	/// as part of this type's single combined static constructor, in file-declaration order across
	/// every partial part -- which is not guaranteed to run after <see cref="GoldenAnchorDate"/>'s
	/// own initializer (a real defect caught by the self-check itself: <c>GoldenAnchorDate</c>
	/// evaluated to <see langword="default"/> here on first measurement, silently turning the range
	/// case's anchor into 0001-01-01). A property getter runs on demand, always after the type's
	/// static constructor has fully completed.
	/// </remarks>
	private static IReadOnlyList<GoldenCase> ExtendedReadinessCases =>
	[
		// Range mode: from is the anchor, to is nine days later -- the six fixture-backed cases
		// are all count-mode (GoldenCase.ToKey can only build Count before this addition).
		new("readiness-range-seed1", SeedMode.Bare, Seed: 1, Symbol: null,
			AnchorDate: GoldenAnchorDate, AnchorPrice: 100m, Drift: 0.0001, Volatility: 0.01,
			Timeframe: "1d", Count: 0, Format: SeriesFormat.Csv, RangeTo: new DateOnly(2023, 3, 10),
			Sha256: "127dca9076c933bdd231d27d082184af8d185a594d097ddcf50eca92b269a1c0"),

		// JSON, not CSV -- otherwise identical to bare-reference-seed1.
		new("readiness-json-seed1", SeedMode.Bare, Seed: 1, Symbol: null,
			AnchorDate: GoldenAnchorDate, AnchorPrice: 100m, Drift: 0.0001, Volatility: 0.01,
			Timeframe: "1d", Count: 60, Format: SeriesFormat.Json,
			Sha256: "96df70345019445b44de18629766e674faad1f9674039c35a7bf983f4a2b3935"),

		// A non-default (but unremarkable) drift -- every fixture-backed case uses 0.0001.
		new("readiness-nondefault-drift-seed1", SeedMode.Bare, Seed: 1, Symbol: null,
			AnchorDate: GoldenAnchorDate, AnchorPrice: 100m, Drift: 0.05, Volatility: 0.01,
			Timeframe: "1d", Count: 60, Format: SeriesFormat.Csv,
			Sha256: "74cfc6b044f56d8c1c795cf0484ffd5ab40f8034bde9a1094193a669b5d2bfcb"),

		// A different anchor date (a Monday, no 2024 holiday nearby) -- every fixture-backed case
		// anchors at 2023-03-01.
		new("readiness-different-anchor-seed1", SeedMode.Bare, Seed: 1, Symbol: null,
			AnchorDate: new DateOnly(2024, 6, 3), AnchorPrice: 100m, Drift: 0.0001, Volatility: 0.01,
			Timeframe: "1d", Count: 60, Format: SeriesFormat.Csv,
			Sha256: "5361dcf22d40bcb476dcc931d02e906349a26778f648545e3bda1a8dacafedba"),
	];

	/// <inheritdoc />
	public IReadOnlyList<GoldenCase> ReadinessCases => [.. GoldenCases, .. ExtendedReadinessCases];
}

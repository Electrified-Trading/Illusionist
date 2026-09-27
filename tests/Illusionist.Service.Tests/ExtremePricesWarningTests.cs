namespace Illusionist.Service.Tests;

/// <summary>
/// A well-formed, legally-bounded key can still walk the price arbitrarily far from
/// <c>anchorPrice</c> without erroring (drift 0.5, volatility 1.0, count 20000 turns a $100 anchor
/// into a ~$152 trillion close, an ordinary <c>SUCCESS</c>). The <c>extreme_prices</c> warning
/// (an owner decision: warn rather than tighten any bound) is the signal for that, through the
/// same non-breaking channel as the calendar-scope warning -- never in the rendered bytes, so the
/// goldens are unaffected.
/// </summary>
public sealed class ExtremePricesWarningTests
{
	private static readonly IGeneratorRegistry Registry = new GeneratorRegistry(GeneratorCatalog.All);
	private static readonly ISeriesService SeriesService = new SeriesService(Registry);

	[Fact]
	public void HighestHigh_Over1000x_WarnsWithRatio()
	{
		var outcome = SeriesService.Generate(
			SeriesQuery.ParseQueryText("generator=brownian-bridge@1&seed=1&drift=0.5&volatility=0&count=5000"), ready: true);

		var result = Assert.IsType<SeriesOutcome.Success>(outcome).Result;

		Assert.NotNull(result.Summary);
		Assert.True(result.Summary!.High > result.Key.AnchorPrice * 1000m);
		Assert.Contains(result.Warnings, w => w.StartsWith("extreme_prices: the series' highest high is", StringComparison.Ordinal));
	}

	[Fact]
	public void LowestLow_Under1000thOfAnchor_WarnsWithRatio()
	{
		var outcome = SeriesService.Generate(
			SeriesQuery.ParseQueryText("generator=brownian-bridge@1&seed=1&drift=-0.5&volatility=0&count=5000"), ready: true);

		var result = Assert.IsType<SeriesOutcome.Success>(outcome).Result;

		Assert.NotNull(result.Summary);
		Assert.True(result.Summary!.Low < result.Key.AnchorPrice / 1000m);
		Assert.Contains(result.Warnings, w => w.StartsWith("extreme_prices: the series' lowest low is", StringComparison.Ordinal));
	}

	[Fact]
	public void ReferenceGeometry_NeverWarns()
	{
		var outcome = SeriesService.Generate(
			SeriesQuery.ParseQueryText("generator=brownian-bridge@1&seed=1&count=60"), ready: true);

		var result = Assert.IsType<SeriesOutcome.Success>(outcome).Result;

		Assert.DoesNotContain(result.Warnings, w => w.StartsWith("extreme_prices", StringComparison.Ordinal));
	}

	[Fact]
	public void ExtremePrices_NeverChangesRenderedBytes()
	{
		// The warning lives in Warnings, never in Body -- the golden fixtures (which never trigger
		// this warning) stay byte-identical regardless of this feature, and any key that does
		// trigger it renders the same bytes with or without the warning being computed.
		const string query = "generator=brownian-bridge@1&seed=1&drift=0.5&volatility=0&count=5000&format=json";
		var raw = SeriesQuery.ParseQueryText(query);

		var first = Assert.IsType<SeriesOutcome.Success>(SeriesService.Generate(raw, ready: true)).Result;
		var second = Assert.IsType<SeriesOutcome.Success>(SeriesService.Generate(raw, ready: true)).Result;

		Assert.Equal(first.Body, second.Body);
		Assert.DoesNotContain("extreme_prices", first.Body, StringComparison.Ordinal);
	}
}

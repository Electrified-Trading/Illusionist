namespace Illusionist.Service.Tests;

/// <summary>D6's fixed limits: 20,000 bars per call, and the 16,384-byte inline threshold.</summary>
public sealed class LimitTests
{
	private static readonly IGeneratorRegistry Registry = new GeneratorRegistry(GeneratorCatalog.All);
	private static readonly ISeriesService SeriesService = new SeriesService(Registry);

	[Fact]
	public void Count_AtLimit_Succeeds()
	{
		var outcome = SeriesService.Generate(
			SeriesQuery.ParseQueryText("generator=brownian-bridge@1&seed=1&count=20000"), ready: true);

		var success = Assert.IsType<SeriesOutcome.Success>(outcome);
		Assert.Equal(20000, success.Result.BarCount);
	}

	[Fact]
	public void Count_OverLimit_TooManyBars()
	{
		var outcome = SeriesService.Generate(
			SeriesQuery.ParseQueryText("generator=brownian-bridge@1&seed=1&count=20001"), ready: true);

		var failure = Assert.IsType<SeriesOutcome.Failure>(outcome);
		Assert.Equal(ErrorCodes.TooManyBars, failure.Error.Code);
		Assert.Equal("count", failure.Error.Parameter);
	}

	[Fact]
	public void Range_OverLimit_TooManyBars()
	{
		var outcome = SeriesService.Generate(
			SeriesQuery.ParseQueryText("generator=brownian-bridge@1&seed=1&from=1900-01-01&to=2199-12-31"), ready: true);

		var failure = Assert.IsType<SeriesOutcome.Failure>(outcome);
		Assert.Equal(ErrorCodes.TooManyBars, failure.Error.Code);
	}

	[Fact]
	public void Count_Zero_OutOfRange()
	{
		var outcome = SeriesService.Generate(
			SeriesQuery.ParseQueryText("generator=brownian-bridge@1&seed=1&count=0"), ready: true);

		var failure = Assert.IsType<SeriesOutcome.Failure>(outcome);
		Assert.Equal(ErrorCodes.OutOfRange, failure.Error.Code);
		Assert.Contains("1 and 20000", failure.Error.Message);
	}

	[Theory]
	[InlineData(16384, true)]
	[InlineData(16385, false)]
	public void InlineThreshold_DecidesOnByteCount(int byteCount, bool expectedInline)
	{
		var key = new SeriesKey(
			new GeneratorRef("brownian-bridge", 1), SeedMode.Bare, 1, null, 0.0001, 0.01,
			new DateOnly(2023, 3, 1), 100m, "1d", new SeriesExtent.Count(1), SeriesFormat.Csv);
		var result = new SeriesResult(key, Body: string.Empty, byteCount, BarCount: 1, First: null, Last: null, Summary: null, Warnings: []);

		Assert.Equal(expectedInline, result.Inline);
	}
}

namespace Illusionist.Service.Tests;

/// <summary>Stateless random access (D3's guarantee): range and count requests agree on every date they share.</summary>
public sealed class RangeConsistencyTests
{
	private static readonly IGeneratorRegistry Registry = new GeneratorRegistry(GeneratorCatalog.All);
	private static readonly ISeriesService SeriesService = new SeriesService(Registry);

	[Fact]
	public void RangeBars_EqualCountModeBars_OnSharedDates()
	{
		// csv, not json: json's body embeds the request's own canonical "uri" (D3), which
		// necessarily differs between a count-mode and a range-mode request for the same bars.
		var countOutcome = SeriesService.Generate(
			SeriesQuery.ParseQueryText("generator=brownian-bridge@1&seed=1&count=10"), ready: true);
		var rangeOutcome = SeriesService.Generate(
			SeriesQuery.ParseQueryText("generator=brownian-bridge@1&seed=1&from=2023-03-01&to=2023-03-14"), ready: true);

		var countBody = Assert.IsType<SeriesOutcome.Success>(countOutcome).Result.Body;
		var rangeBody = Assert.IsType<SeriesOutcome.Success>(rangeOutcome).Result.Body;

		// The range spans exactly the same 10 trading days as the count-mode request (2023-03-01
		// through 2023-03-14 is 10 weekdays with no known holiday in that window), so the two
		// bodies -- generated through entirely different timestamp-walk code paths -- must be
		// byte-identical, not merely "close."
		Assert.Equal(countBody, rangeBody);
	}

	[Fact]
	public void RangeBeforeAnchor_IsDeterministic_AcrossTwoCalls()
	{
		const string query = "generator=brownian-bridge@1&seed=1&from=2020-01-01&to=2020-01-10";

		var first = Assert.IsType<SeriesOutcome.Success>(
			SeriesService.Generate(SeriesQuery.ParseQueryText(query), ready: true)).Result;
		var second = Assert.IsType<SeriesOutcome.Success>(
			SeriesService.Generate(SeriesQuery.ParseQueryText(query), ready: true)).Result;

		// 2020-01-01 predates the default reference anchor (2023-03-01): a bar's value is a pure
		// function of (key, timestamp), so "before the anchor" is not a special case, and two
		// independent generations of the same range must be byte-identical.
		Assert.Equal(first.Body, second.Body);
		Assert.Equal(8, first.BarCount); // 2020-01-01..2020-01-10: two weekend days excluded, no known holiday in this window
	}
}

using Illusionist.Service.Mcp;
using Illusionist.Service.Tests.Support;
using NSubstitute;

namespace Illusionist.Service.Tests;

/// <summary>
/// The three tools' own shapes (D2/D10.10), plus one generation-time error that genuinely requires
/// a substituted source: a zero <c>Open</c> with no exception involved. The other
/// <c>price_out_of_range</c> trigger (an <see cref="OverflowException"/> from real arithmetic) is
/// reachable through real input and is covered by <see cref="ValidationTests.PriceOutOfRange_RealOverflow_NoMocks"/>
/// instead -- keeping a fake for it here would add nothing that test doesn't already prove for real.
/// </summary>
public sealed class ToolCatalogTests(IllusionistWebApplicationFactory factory) : IClassFixture<IllusionistWebApplicationFactory>
{
	[Fact]
	public async Task ToolsList_ReturnsExactlyTheThreeNames()
	{
		await using var client = await McpTestClient.CreateAsync(factory);
		var tools = await client.ListToolsAsync();

		Assert.Equal(
			[ToolCatalog.Generators, ToolCatalog.Describe, ToolCatalog.Series],
			tools.Select(t => t.Name));
	}

	[Fact]
	public void SeriesSchema_PropertyNames_EqualParsersKnownNames()
	{
		var tool = ToolCatalog.Tools.Single(t => t.Name == ToolCatalog.Series);
		var properties = tool.InputSchema.GetProperty("properties");
		var schemaNames = properties.EnumerateObject().Select(p => p.Name).ToHashSet();

		Assert.Equal(SeriesQuery.KnownNames.ToHashSet(), schemaNames);
	}

	[Fact]
	public void EveryTool_Annotations_AreReadOnlyAndIdempotent()
	{
		Assert.All(ToolCatalog.Tools, tool =>
		{
			Assert.NotNull(tool.Annotations);
			Assert.True(tool.Annotations!.ReadOnlyHint);
			Assert.True(tool.Annotations.IdempotentHint);
			Assert.False(tool.Annotations.DestructiveHint);
			Assert.False(tool.Annotations.OpenWorldHint);
		});
	}

	[Fact]
	public void PriceOutOfRange_WhenOpenIsZero()
	{
		var timestamp = BrownianBridgeV1.GoldenAnchorDate.ToDateTime(DefaultEquitiesSchedule.MarketOpen);
		var source = Substitute.For<ISeriesSource>();
		source.Timestamps(Arg.Any<int>()).Returns([timestamp]);
		source.BarAt(Arg.Any<DateTime>()).Returns(Bar.Create(timestamp, new OHLC { Open = 0m, High = 1m, Low = 0m, Close = 1m }, 0));

		var outcome = GenerateWithFakeSource(source);

		var failure = Assert.IsType<SeriesOutcome.Failure>(outcome);
		Assert.Equal(ErrorCodes.PriceOutOfRange, failure.Error.Code);
	}

	private static SeriesOutcome GenerateWithFakeSource(ISeriesSource source)
	{
		var version = Substitute.For<IGeneratorVersion>();
		var reference = new GeneratorRef("brownian-bridge", 1);
		version.Ref.Returns(reference);
		version.GoldenCases.Returns([new GoldenCase(
			"dummy", SeedMode.Bare, 1, null, BrownianBridgeV1.GoldenAnchorDate, 100m, 0.0001, 0.01, "1d", 1, SeriesFormat.Csv,
			"0000000000000000000000000000000000000000000000000000000000000000")]);
		version.Open(Arg.Any<SeriesKey>()).Returns(source);

		var registry = new GeneratorRegistry([version]);
		var seriesService = new SeriesService(registry);
		var key = new SeriesKey(
			reference, SeedMode.Bare, 1, null, 0.0001, 0.01, BrownianBridgeV1.GoldenAnchorDate, 100m, "1d",
			new SeriesExtent.Count(1), SeriesFormat.Csv);

		return seriesService.GenerateFromKey(key);
	}
}

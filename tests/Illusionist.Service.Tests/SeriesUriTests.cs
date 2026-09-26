using Illusionist.Service.Tests.Support;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Illusionist.Service.Tests;

/// <summary>D1's canonical query round trip: any spelling of the same key comes back to one string, and that string reparses to itself.</summary>
public sealed class SeriesUriTests(IllusionistWebApplicationFactory factory) : IClassFixture<IllusionistWebApplicationFactory>
{
	private static readonly IGeneratorRegistry Registry = new GeneratorRegistry(GeneratorCatalog.All);
	private static readonly GeneratorRef BrownianBridgeV1Ref = new("brownian-bridge", 1);

	public static IEnumerable<object[]> RoundtripKeys()
	{
		SeriesKey Base(SeriesExtent extent) => new(
			BrownianBridgeV1Ref, SeedMode.Bare, 1, null, 0.0001, 0.01, new DateOnly(2023, 3, 1), 100m, "1d", extent, SeriesFormat.Csv);

		yield return [Base(new SeriesExtent.Count(60)) with { SeedMode = SeedMode.SymbolHashed, Symbol = "AAPL" }];
		yield return [Base(new SeriesExtent.Range(new DateOnly(2023, 3, 1), new DateOnly(2023, 3, 10)))];
		yield return [Base(new SeriesExtent.Count(60)) with { Drift = -0.25 }];
		yield return [Base(new SeriesExtent.Count(60)) with { Volatility = 0.00001 }];
		yield return [Base(new SeriesExtent.Count(60)) with { AnchorPrice = 100.5m }]; // already minimal scale -- SeriesKey itself does not normalize (SeriesKeyParser does)
	}

	[Theory]
	[MemberData(nameof(RoundtripKeys))]
	public void Canonical_Parse_Canonical_IsIdempotent(SeriesKey key)
	{
		var canonical = SeriesQuery.Canonical(key);
		var reparsed = SeriesKeyParser.Parse(SeriesQuery.ParseQueryText(canonical), Registry);

		Assert.Equal(canonical, SeriesQuery.Canonical(reparsed));
	}

	[Fact]
	public void Volatility_00001_PrintsAsExponentForm()
	{
		var key = new SeriesKey(BrownianBridgeV1Ref, SeedMode.Bare, 1, null, 0.0001, 0.00001,
			new DateOnly(2023, 3, 1), 100m, "1d", new SeriesExtent.Count(60), SeriesFormat.Csv);

		Assert.Contains("volatility=1E-05", SeriesQuery.Canonical(key));
	}

	[Fact]
	public void AnchorPrice_100_50_NormalizesToMinimalScale()
	{
		var key = SeriesKeyParser.Parse(
			SeriesQuery.ParseQueryText("generator=brownian-bridge@1&seed=1&count=60&anchorPrice=100.50"), Registry);

		Assert.Contains("anchorPrice=100.5&", SeriesQuery.Canonical(key));
	}

	[Fact]
	public void NonCanonicalSpelling_MapsToOneCanonicalUri()
	{
		const string nonCanonical = "anchorPrice=100.00&count=60&generator=brownian-bridge@1&seed=1";
		var key = SeriesKeyParser.Parse(SeriesQuery.ParseQueryText(nonCanonical), Registry);

		Assert.Equal(
			"generator=brownian-bridge%401&seedMode=bare&seed=1&drift=0.0001&volatility=0.01&anchorDate=2023-03-01&anchorPrice=100&timeframe=1d&count=60&format=csv",
			SeriesQuery.Canonical(key));
	}

	[Fact]
	public void AnchorPrice_100_And_100_00_ProduceIdenticalBytes()
	{
		var seriesService = new SeriesService(Registry);
		var plain = (SeriesOutcome.Success)seriesService.Generate(
			SeriesQuery.ParseQueryText("generator=brownian-bridge@1&seed=1&count=10&anchorPrice=100"), ready: true);
		var padded = (SeriesOutcome.Success)seriesService.Generate(
			SeriesQuery.ParseQueryText("generator=brownian-bridge@1&seed=1&count=10&anchorPrice=100.00"), ready: true);

		Assert.Equal(plain.Result.Body, padded.Result.Body);
	}

	[Fact]
	public async Task OversizedResult_IsNotInline_AndResourceLinkReadsToRestBytes()
	{
		await using var client = await McpTestClient.CreateAsync(factory);
		var result = await client.CallToolAsync("illusionist_series", new Dictionary<string, object?>
		{
			["generator"] = "brownian-bridge@1",
			["seed"] = 1,
			["count"] = 200,
		});

		Assert.False(result.IsError == true);
		var content = result.Content.ToList();
		var envelope = Assert.IsType<TextContentBlock>(content[0]).Text;
		Assert.Contains("\"inline\":false", envelope);

		var link = Assert.IsType<ResourceLinkBlock>(content[1]);
		var read = await client.ReadResourceAsync(link.Uri);
		var resourceText = Assert.IsType<TextResourceContents>(read.Contents.Single()).Text;

		var httpClient = factory.CreateClient();
		var restText = await httpClient.GetStringAsync("/v1/series?generator=brownian-bridge@1&seed=1&count=200");

		Assert.Equal(restText, resourceText);
	}

	[Fact]
	public async Task MalformedResourceUri_GivesInvalidParamsWithCodePrefix()
	{
		await using var client = await McpTestClient.CreateAsync(factory);

		var exception = await Assert.ThrowsAnyAsync<McpException>(
			() => client.ReadResourceAsync("not-an-illusionist-uri").AsTask());

		Assert.Contains("invalid_resource_uri:", exception.Message);
	}
}

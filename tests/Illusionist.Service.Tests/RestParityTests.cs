using System.Net;
using Illusionist.Service.Tests.Support;
using ModelContextProtocol.Protocol;

namespace Illusionist.Service.Tests;

/// <summary>REST is the MCP surface's twin: every route must answer with the identical bytes an MCP call would.</summary>
public sealed class RestParityTests(IllusionistWebApplicationFactory factory) : IClassFixture<IllusionistWebApplicationFactory>
{
	[Theory]
	[MemberData(nameof(GoldenCaseNamesAndFormats))]
	public async Task Series_GoldenCase_RestEqualsMcp(string fixtureName, string format)
	{
		var @case = BrownianBridgeV1.Instance.GoldenCases.Single(c => c.Name == fixtureName);
		var arguments = @case.ToToolArguments();
		arguments["format"] = format;

		var mcpBody = await CallSeriesToolInlineBody(arguments);
		var restBody = await GetSeriesRest(BuildQuery(@case, format));

		Assert.Equal(mcpBody, restBody);
	}

	[Fact]
	public async Task Series_RangeRequest_RestEqualsMcp()
	{
		var arguments = new Dictionary<string, object?>
		{
			["generator"] = "brownian-bridge@1",
			["seed"] = 1,
			["from"] = "2023-03-01",
			["to"] = "2023-03-10",
		};

		var mcpBody = await CallSeriesToolInlineBody(arguments);
		var restBody = await GetSeriesRest("generator=brownian-bridge@1&seed=1&from=2023-03-01&to=2023-03-10");

		Assert.Equal(mcpBody, restBody);
	}

	[Fact]
	public async Task Series_LargeRequest_ResourceLinkRestEqualsReadResource()
	{
		var arguments = new Dictionary<string, object?>
		{
			["generator"] = "brownian-bridge@1",
			["seed"] = 1,
			["count"] = 5000,
		};

		await using var client = await McpTestClient.CreateAsync(factory);
		var result = await client.CallToolAsync("illusionist_series", arguments);
		var content = result.Content.ToList();

		Assert.False(result.IsError == true);
		var link = Assert.IsType<ResourceLinkBlock>(content[1]);

		var read = await client.ReadResourceAsync(link.Uri);
		var resourceText = Assert.IsType<TextResourceContents>(read.Contents.Single()).Text;

		var restBody = await GetSeriesRest("generator=brownian-bridge@1&seed=1&count=5000");

		Assert.Equal(resourceText, restBody);
	}

	[Fact]
	public async Task Generators_RestEqualsMcp()
	{
		await using var client = await McpTestClient.CreateAsync(factory);
		var result = await client.CallToolAsync("illusionist_generators");
		var mcpText = Assert.IsType<TextContentBlock>(result.Content.Single()).Text;

		var httpClient = factory.CreateClient();
		var restText = await httpClient.GetStringAsync("/v1/generators");

		Assert.Equal(mcpText, restText);
	}

	[Fact]
	public async Task Describe_RestEqualsMcp()
	{
		await using var client = await McpTestClient.CreateAsync(factory);
		var result = await client.CallToolAsync("illusionist_describe", new Dictionary<string, object?> { ["generator"] = "brownian-bridge@1" });
		var mcpText = Assert.IsType<TextContentBlock>(result.Content.Single()).Text;

		var httpClient = factory.CreateClient();
		var restText = await httpClient.GetStringAsync("/v1/generators/brownian-bridge@1");

		Assert.Equal(mcpText, restText);
	}

	[Theory]
	[InlineData("generator=nonexistent-generator@1&seed=1", HttpStatusCode.NotFound, "unknown_generator")]
	[InlineData("generator=brownian-bridge@1&seed=1&count=999999", HttpStatusCode.BadRequest, "too_many_bars")]
	[InlineData("generator=brownian-bridge@1&seed=1&anchorDate=2023-13-40", HttpStatusCode.BadRequest, "invalid_parameter")]
	public async Task Series_BadInput_RestErrorMatchesMcpError(string query, HttpStatusCode expectedStatus, string expectedCode)
	{
		var httpClient = factory.CreateClient();
		var response = await httpClient.GetAsync($"/v1/series?{query}");
		Assert.Equal(expectedStatus, response.StatusCode);
		var restJson = await response.Content.ReadAsStringAsync();

		await using var client = await McpTestClient.CreateAsync(factory);
		var arguments = QueryToArguments(query);
		var result = await client.CallToolAsync("illusionist_series", arguments);
		Assert.True(result.IsError);
		var mcpJson = Assert.IsType<TextContentBlock>(result.Content.Single()).Text;

		Assert.Equal(mcpJson, restJson);
		Assert.Contains($"\"code\":\"{expectedCode}\"", restJson);
	}

	public static IEnumerable<object[]> GoldenCaseNamesAndFormats()
	{
		foreach (var @case in BrownianBridgeV1.Instance.GoldenCases)
		{
			yield return [@case.Name, "csv"];
			yield return [@case.Name, "json"];
		}
	}

	private async Task<string> CallSeriesToolInlineBody(Dictionary<string, object?> arguments)
	{
		await using var client = await McpTestClient.CreateAsync(factory);
		var result = await client.CallToolAsync("illusionist_series", arguments);
		Assert.False(result.IsError == true);
		var content = result.Content.ToList();
		return Assert.IsType<TextContentBlock>(content[1]).Text;
	}

	private async Task<string> GetSeriesRest(string query)
	{
		var httpClient = factory.CreateClient();
		return await httpClient.GetStringAsync($"/v1/series?{query}");
	}

	private static string BuildQuery(GoldenCase @case, string format)
	{
		var key = @case.ToKey(new GeneratorRef("brownian-bridge", 1)) with { Format = format == "csv" ? SeriesFormat.Csv : SeriesFormat.Json };
		return SeriesQuery.Canonical(key);
	}

	private static Dictionary<string, object?> QueryToArguments(string query)
	{
		var arguments = new Dictionary<string, object?>();
		foreach (var part in query.Split('&'))
		{
			var eq = part.IndexOf('=');
			var name = Uri.UnescapeDataString(part[..eq]);
			var value = Uri.UnescapeDataString(part[(eq + 1)..]);
			arguments[name] = int.TryParse(value, out var i) ? i : value;
		}

		return arguments;
	}
}

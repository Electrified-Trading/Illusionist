using Illusionist.Service.Tests.Support;
using ModelContextProtocol.Protocol;

namespace Illusionist.Service.Tests;

/// <summary>
/// A real MCP client, speaking streamable HTTP against the hosted service, reproduces every golden
/// fixture byte for byte through <c>illusionist_series</c>.
/// </summary>
public sealed class McpGoldenTests(IllusionistWebApplicationFactory factory) : IClassFixture<IllusionistWebApplicationFactory>
{
	[Theory]
	[MemberData(nameof(CaseNames))]
	public async Task Series_MatchesGoldenFixture_ByteForByte(string fixtureName)
	{
		var @case = BrownianBridgeV1.Instance.GoldenCases.Single(c => c.Name == fixtureName);
		await using var client = await McpTestClient.CreateAsync(factory);

		var result = await client.CallToolAsync("illusionist_series", @case.ToToolArguments());

		Assert.False(result.IsError == true);
		var content = result.Content.ToList();
		var body = Assert.IsType<TextContentBlock>(content[1]).Text;

		Assert.Equal(GoldenFixtures.ReadText(fixtureName), body);
	}

	public static IEnumerable<object[]> CaseNames
		=> BrownianBridgeV1.Instance.GoldenCases.Select(c => new object[] { c.Name });
}

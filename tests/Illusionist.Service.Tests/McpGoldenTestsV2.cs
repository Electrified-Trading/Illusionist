using Illusionist.Service.Tests.Support;
using ModelContextProtocol.Protocol;

namespace Illusionist.Service.Tests;

/// <summary>
/// A real MCP client, speaking streamable HTTP against the hosted service, reproduces every
/// <c>brownian-bridge@2</c> golden fixture byte for byte -- the <c>@2</c> twin of
/// <see cref="McpGoldenTests"/>, proving the wire path (not just <see cref="GoldenSelfCheck"/>'s own
/// direct call) for the version that has no reference-platform excuse not to.
/// </summary>
public sealed class McpGoldenTestsV2(IllusionistWebApplicationFactory factory) : IClassFixture<IllusionistWebApplicationFactory>
{
	[Theory]
	[MemberData(nameof(CaseNames))]
	public async Task Series_MatchesGoldenFixture_ByteForByte(string fixtureName)
	{
		var @case = BrownianBridgeV2.Instance.GoldenCases.Single(c => c.Name == fixtureName);
		await using var client = await McpTestClient.CreateAsync(factory);

		var result = await client.CallToolAsync("illusionist_series", @case.ToToolArguments(BrownianBridgeV2.Instance.Ref));

		Assert.False(result.IsError == true);
		var content = result.Content.ToList();
		var body = Assert.IsType<TextContentBlock>(content[1]).Text;

		Assert.Equal(GoldenFixtures.ReadText(fixtureName), body);
	}

	public static IEnumerable<object[]> CaseNames
		=> BrownianBridgeV2.Instance.GoldenCases.Select(c => new object[] { c.Name });
}

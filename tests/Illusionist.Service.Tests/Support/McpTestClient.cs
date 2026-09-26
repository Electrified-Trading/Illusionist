using Microsoft.AspNetCore.Mvc.Testing;
using ModelContextProtocol.Client;

namespace Illusionist.Service.Tests.Support;

/// <summary>
/// Builds a real MCP client (streamable HTTP) over a hosted factory's in-process <c>TestServer</c>
/// -- the same wire protocol a real agent speaks, minus a real socket.
/// </summary>
internal static class McpTestClient
{
	public static async Task<McpClient> CreateAsync(WebApplicationFactory<Program> factory)
	{
		var httpClient = factory.CreateClient();
		var transport = new HttpClientTransport(
			new HttpClientTransportOptions
			{
				Endpoint = new Uri(httpClient.BaseAddress!, "mcp"),
				TransportMode = HttpTransportMode.StreamableHttp,
			},
			httpClient,
			loggerFactory: null,
			ownsHttpClient: false);

		return await McpClient.CreateAsync(transport);
	}
}

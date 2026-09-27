using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Illusionist.Service.Mcp;

/// <summary>
/// The four low-level MCP handlers (D0.5): <c>tools/list</c>, <c>tools/call</c>,
/// <c>resources/templates/list</c> and <c>resources/read</c>. Registered via
/// <c>WithListToolsHandler</c>/<c>WithCallToolHandler</c>/<c>WithListResourceTemplatesHandler</c>/<c>WithReadResourceHandler</c>
/// rather than attribute-bound methods, so one parser (<c>SeriesQuery</c>/<c>SeriesKeyParser</c>)
/// validates every surface identically and no SDK binding error can escape the D5 error taxonomy.
/// </summary>
public static partial class McpHandlers
{
	/// <summary><c>tools/list</c>: the three tools this service exposes, unpaginated.</summary>
	public static ValueTask<ListToolsResult> ListTools(RequestContext<ListToolsRequestParams> request, CancellationToken cancellationToken)
		=> ValueTask.FromResult(new ListToolsResult { Tools = [.. ToolCatalog.Tools] });

	/// <summary><c>resources/templates/list</c>: the one <c>illusionist://series</c> template.</summary>
	public static ValueTask<ListResourceTemplatesResult> ListResourceTemplates(
		RequestContext<ListResourceTemplatesRequestParams> request, CancellationToken cancellationToken)
		=> ValueTask.FromResult(new ListResourceTemplatesResult { ResourceTemplates = [.. ResourceTemplates.All] });

	/// <summary>Builds the <see cref="McpProtocolException"/> every resource-read failure surfaces as (D5).</summary>
	private static McpProtocolException ToProtocolException(IllusionistError error)
		=> new($"{error.Code}: {error.Message}", McpErrorCode.InvalidParams);

	/// <summary>Resolves a required app singleton from the request's own service provider (D0.5).</summary>
	private static T GetRequiredService<T>(RequestContext<CallToolRequestParams> request) where T : notnull
		=> request.Services!.GetRequiredService<T>();

	private static T GetRequiredService<T>(RequestContext<ReadResourceRequestParams> request) where T : notnull
		=> request.Services!.GetRequiredService<T>();
}

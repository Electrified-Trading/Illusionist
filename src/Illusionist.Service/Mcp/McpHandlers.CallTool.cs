using System.Text.Json;
using System.Text.RegularExpressions;
using Illusionist.Service.Generators;
using Illusionist.Service.SelfCheck;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Illusionist.Service.Mcp;

public static partial class McpHandlers
{
	[GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*@[1-9][0-9]*$")]
	private static partial Regex DescribeGeneratorRefPattern();

	/// <summary><c>tools/call</c>: dispatches to one of the three registered tools. Tool code never throws for input problems (D5).</summary>
	public static ValueTask<CallToolResult> CallTool(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken)
	{
		var result = request.Params?.Name switch
		{
			ToolCatalog.Generators => HandleGenerators(request),
			ToolCatalog.Describe => HandleDescribe(request),
			ToolCatalog.Series => HandleSeries(request),
			var name => throw new McpProtocolException($"Unknown tool '{name}'.", McpErrorCode.InvalidParams),
		};

		return ValueTask.FromResult(result);
	}

	/// <summary><c>illusionist_generators</c>: no arguments accepted.</summary>
	private static CallToolResult HandleGenerators(RequestContext<CallToolRequestParams> request)
	{
		if (request.Params?.Arguments is { Count: > 0 } arguments)
			return ErrorResult(IllusionistError.UnknownParameter(arguments.Keys.First(), []));

		var registry = GetRequiredService<IGeneratorRegistry>(request);
		var json = Series.GeneratorListingRenderer.RenderGenerators(registry.All);
		return new CallToolResult { Content = [new TextContentBlock { Text = json }] };
	}

	/// <summary><c>illusionist_describe</c>: one required <c>generator</c> argument, <c>id@version</c>.</summary>
	private static CallToolResult HandleDescribe(RequestContext<CallToolRequestParams> request)
	{
		var arguments = request.Params?.Arguments;
		if (arguments is null || !arguments.TryGetValue("generator", out var element))
			return ErrorResult(IllusionistError.MissingParameter("generator", ErrorMessages.MissingParameter.Generator));

		var extra = arguments.Keys.FirstOrDefault(name => name != "generator");
		if (extra is not null)
			return ErrorResult(IllusionistError.UnknownParameter(extra, ["generator"]));

		if (element.ValueKind != JsonValueKind.String)
			return ErrorResult(IllusionistError.InvalidParameter("generator", "a string", element.GetRawText()));

		var text = element.GetString() ?? string.Empty;
		if (!DescribeGeneratorRefPattern().IsMatch(text) || !GeneratorRef.TryParse(text, out var reference))
			return ErrorResult(IllusionistError.InvalidGeneratorRef(text));

		var registry = GetRequiredService<IGeneratorRegistry>(request);
		if (!registry.HasId(reference.Id))
			return ErrorResult(IllusionistError.UnknownGenerator(reference.Id, registry.AllRefs));

		if (!registry.TryGet(reference, out var version) || version is null)
			return ErrorResult(IllusionistError.UnknownGeneratorVersion(reference.Id, reference.Version, registry.AllRefs));

		var selfCheckState = GetRequiredService<SelfCheckState>(request);
		var json = Series.GeneratorListingRenderer.RenderDescribe(version, selfCheckState.Current);
		return new CallToolResult { Content = [new TextContentBlock { Text = json }] };
	}

	/// <summary>Builds the <c>IsError:true</c> result every input-problem case returns instead of throwing (D5).</summary>
	private static CallToolResult ErrorResult(IllusionistError error)
		=> new() { IsError = true, Content = [new TextContentBlock { Text = error.ToJson() }] };
}

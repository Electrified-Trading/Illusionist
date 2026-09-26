using Illusionist.Service.SelfCheck;
using Illusionist.Service.Series;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Illusionist.Service.Mcp;

public static partial class McpHandlers
{
	/// <summary>
	/// <c>illusionist_series</c>: the readiness gate first (before any parsing), then the shared
	/// <see cref="ISeriesService"/> pipeline, then the size-threshold envelope decision (D3).
	/// </summary>
	private static CallToolResult HandleSeries(RequestContext<CallToolRequestParams> request)
	{
		var selfCheckState = GetRequiredService<SelfCheckState>(request);
		if (!selfCheckState.IsReady)
			return ErrorResult(IllusionistError.NotReady());

		IReadOnlyDictionary<string, string> raw;
		try
		{
			raw = SeriesQuery.ParseToolArguments(request.Params?.Arguments);
		}
		catch (IllusionistErrorException ex)
		{
			return ErrorResult(ex.Error);
		}

		var seriesService = GetRequiredService<ISeriesService>(request);
		var outcome = seriesService.Generate(raw, ready: true);

		return outcome switch
		{
			SeriesOutcome.Success success => BuildSeriesSuccess(request, success.Result),
			SeriesOutcome.Failure failure => ErrorResult(failure.Error),
			var other => ErrorResult(IllusionistError.InternalError()),
		};
	}

	private static CallToolResult BuildSeriesSuccess(RequestContext<CallToolRequestParams> request, SeriesResult result)
	{
		var publicBaseUrl = GetRequiredService<IOptions<ServiceOptions>>(request).Value.PublicBaseUrl;
		var envelope = SeriesEnvelope.Build(result, publicBaseUrl);

		var content = new List<ContentBlock> { new TextContentBlock { Text = envelope } };

		if (result.Inline)
		{
			content.Add(new TextContentBlock { Text = result.Body });
		}
		else
		{
			content.Add(new ResourceLinkBlock
			{
				Uri = SeriesQuery.ResourceUri(result.Key),
				Name = "series",
				MimeType = SeriesMime.For(result.Key.Format),
				Size = result.ByteCount,
			});
		}

		return new CallToolResult { Content = content };
	}
}

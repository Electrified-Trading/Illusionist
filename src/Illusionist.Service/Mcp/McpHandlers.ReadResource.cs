using Illusionist.Service.SelfCheck;
using Illusionist.Service.Series;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Illusionist.Service.Mcp;

public static partial class McpHandlers
{
	private const string SeriesUriPrefix = "illusionist://series?";

	/// <summary>
	/// <c>resources/read</c>: every failure (a malformed URI, an unready host, or any D5 validation
	/// error) surfaces as a thrown <see cref="ModelContextProtocol.McpProtocolException"/> -- unlike
	/// the tool surface, resource reads never return an in-band error result (D5).
	/// </summary>
	public static ValueTask<ReadResourceResult> ReadResource(RequestContext<ReadResourceRequestParams> request, CancellationToken cancellationToken)
	{
		var uri = request.Params?.Uri ?? string.Empty;
		if (!uri.StartsWith(SeriesUriPrefix, StringComparison.Ordinal))
			throw ToProtocolException(IllusionistError.InvalidResourceUri(uri));

		var selfCheckState = GetRequiredService<SelfCheckState>(request);
		if (!selfCheckState.IsReady)
			throw ToProtocolException(IllusionistError.NotReady());

		IReadOnlyDictionary<string, string> raw;
		try
		{
			raw = SeriesQuery.ParseQueryText(uri[SeriesUriPrefix.Length..]);
		}
		catch (IllusionistErrorException ex)
		{
			throw ToProtocolException(ex.Error);
		}

		var seriesService = GetRequiredService<ISeriesService>(request);
		var outcome = seriesService.Generate(raw, ready: true);

		return outcome switch
		{
			SeriesOutcome.Success success => ValueTask.FromResult(new ReadResourceResult
			{
				Contents = [new TextResourceContents { Uri = uri, MimeType = SeriesMime.For(success.Result.Key.Format), Text = success.Result.Body }],
			}),
			SeriesOutcome.Failure failure => throw ToProtocolException(failure.Error),
			var other => throw ToProtocolException(IllusionistError.InternalError()),
		};
	}
}

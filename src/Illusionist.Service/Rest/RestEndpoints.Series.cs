using System.Text;
using Illusionist.Service.SelfCheck;
using Illusionist.Service.Series;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Illusionist.Service.Rest;

public static partial class RestEndpoints
{
	private static void MapSeries(IEndpointRouteBuilder app)
	{
		app.MapGet("/v1/series", (
			HttpContext context,
			ISeriesService seriesService,
			SelfCheckState selfCheckState,
			IOptions<ServiceOptions> options) =>
		{
			if (!selfCheckState.IsReady)
				return ErrorResult(context, IllusionistError.NotReady());

			IReadOnlyDictionary<string, string> raw;
			try
			{
				// Request.QueryString.Value, never Request.Query, which decodes '+' as a space (D1).
				raw = SeriesQuery.ParseQueryText(context.Request.QueryString.Value ?? string.Empty);
			}
			catch (IllusionistErrorException ex)
			{
				return ErrorResult(context, ex.Error);
			}

			var outcome = seriesService.Generate(raw, ready: true);
			return outcome switch
			{
				SeriesOutcome.Success success => WriteSeriesResult(context, success.Result, options.Value.PublicBaseUrl),
				SeriesOutcome.Failure failure => ErrorResult(context, failure.Error),
				var other => ErrorResult(context, IllusionistError.InternalError()),
			};
		});
	}

	/// <summary>The full rendered body at any size, with <c>Illusionist-Uri</c>/<c>Illusionist-Warning</c> and a one-year immutable cache (D4).</summary>
	private static IResult WriteSeriesResult(HttpContext context, SeriesResult result, string? publicBaseUrl)
	{
		context.Response.Headers["Illusionist-Uri"] = SeriesQuery.ResourceUri(result.Key);
		foreach (var warning in result.Warnings)
			context.Response.Headers.Append("Illusionist-Warning", warning);

		context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";

		return Results.Bytes(Encoding.UTF8.GetBytes(result.Body), $"{SeriesMime.For(result.Key.Format)}; charset=utf-8");
	}
}

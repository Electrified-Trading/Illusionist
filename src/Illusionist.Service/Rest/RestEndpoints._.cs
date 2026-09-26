using Illusionist.Service.Generators;
using Illusionist.Service.SelfCheck;
using Illusionist.Service.Series;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Illusionist.Service.Rest;

/// <summary>The REST twin (D4): the same parser and renderers as the MCP surface, over plain HTTP GET.</summary>
public static partial class RestEndpoints
{
	/// <summary>Maps every REST route this service serves.</summary>
	public static IEndpointRouteBuilder MapIllusionistRest(this IEndpointRouteBuilder app)
	{
		app.MapGet("/", () => Results.Text(
			"""{"service":"illusionist","version":"1.0.0","mcp":"/mcp","generators":"/v1/generators","health":"/healthz"}""",
			"application/json; charset=utf-8"));

		app.MapGet("/v1/generators", (IGeneratorRegistry registry)
			=> Results.Text(GeneratorListingRenderer.RenderGenerators(registry.All), "application/json; charset=utf-8"));

		app.MapGet("/v1/generators/{generatorRef}", (HttpContext context, string generatorRef, IGeneratorRegistry registry, SelfCheckState selfCheckState) =>
		{
			if (!GeneratorRef.TryParse(generatorRef, out var reference))
				return ErrorResult(context, IllusionistError.InvalidGeneratorRef(generatorRef));

			if (!registry.HasId(reference.Id))
				return ErrorResult(context, IllusionistError.UnknownGenerator(reference.Id, registry.AllRefs));

			if (!registry.TryGet(reference, out var version) || version is null)
				return ErrorResult(context, IllusionistError.UnknownGeneratorVersion(reference.Id, reference.Version, registry.AllRefs));

			return Results.Text(
				GeneratorListingRenderer.RenderDescribe(version, selfCheckState.Current), "application/json; charset=utf-8");
		});

		MapSeries(app);

		app.MapGet("/healthz", (HttpContext context, IGoldenSelfCheck selfCheck, SelfCheckState selfCheckState, ILogger<Program> logger) =>
		{
			context.Response.Headers.CacheControl = "no-store";
			var report = SelfCheckRunner.RunAndPublish(selfCheck, selfCheckState, logger);
			return Results.Text(report.ToJson(), "application/json; charset=utf-8", statusCode: report.IsReady ? 200 : 503);
		});

		return app;
	}

	/// <summary>Builds the D5 error response every REST route shares: the error JSON body, its status, and <c>Cache-Control: no-store</c>.</summary>
	private static IResult ErrorResult(HttpContext context, IllusionistError error)
	{
		context.Response.Headers.CacheControl = "no-store";
		return Results.Text(error.ToJson(), "application/json; charset=utf-8", statusCode: error.HttpStatus);
	}
}

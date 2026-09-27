using Illusionist.Service;
using Illusionist.Service.Generators;
using Illusionist.Service.Mcp;
using Illusionist.Service.Rest;
using Illusionist.Service.SelfCheck;
using Illusionist.Service.Series;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;

// Ops's one-line proof for their host: runs the golden check without starting Kestrel (D8).
if (args.Contains("--self-check", StringComparer.Ordinal))
{
	var registry = new GeneratorRegistry(GeneratorCatalog.All);
	var seriesService = new SeriesService(registry);
	var selfCheck = new GoldenSelfCheck(registry, seriesService);
	return new SelfCheckCommand(selfCheck).Run(Console.Out);
}

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(o =>
{
	o.UseUtcTimestamp = true;
	o.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
});

builder.Services.AddSingleton<IGeneratorRegistry>(new GeneratorRegistry(GeneratorCatalog.All));
builder.Services.AddSingleton<ISeriesService, SeriesService>();
builder.Services.AddSingleton<IGoldenSelfCheck, GoldenSelfCheck>();
builder.Services.AddSingleton<SelfCheckState>();
builder.Services.Configure<ServiceOptions>(builder.Configuration.GetSection("Illusionist"));

builder.Services
	.AddMcpServer(o =>
	{
		o.ServerInfo = new Implementation { Name = "illusionist", Version = "1.0.0" };
		o.ServerInstructions =
			"Illusionist generates deterministic, synthetic, market-like OHLCV bars and never reads real market data. " +
			"Call illusionist_series with generator 'brownian-bridge@1', a seed, and a count (or from/to). " +
			"The same arguments always return the same bytes, so keep the returned uri rather than the data: reading it regenerates them. " +
			"illusionist_describe states the reproducibility contract.";
	})
	.WithHttpTransport(o => o.Stateless = true)
	.WithListToolsHandler(McpHandlers.ListTools)
	.WithCallToolHandler(McpHandlers.CallTool)
	.WithListResourceTemplatesHandler(McpHandlers.ListResourceTemplates)
	.WithReadResourceHandler(McpHandlers.ReadResource);

var app = builder.Build();

ValidatePublicBaseUrl(app.Services.GetRequiredService<IOptions<ServiceOptions>>().Value);

// Kestrel does not listen until this completes -- a host that cannot reproduce its own golden
// fixtures stays up (readable at /healthz) rather than crash-looping (D8).
SelfCheckRunner.RunAndPublish(
	app.Services.GetRequiredService<IGoldenSelfCheck>(),
	app.Services.GetRequiredService<SelfCheckState>(),
	app.Services.GetRequiredService<ILogger<Program>>());

app.MapMcp("/mcp");
app.MapIllusionistRest();

app.Run();

return 0;

static void ValidatePublicBaseUrl(ServiceOptions options)
{
	if (options.PublicBaseUrl is null)
		return;

	var isAbsoluteHttpUrl =
		Uri.TryCreate(options.PublicBaseUrl, UriKind.Absolute, out var uri)
		&& uri.Scheme is "http" or "https"
		&& !options.PublicBaseUrl.EndsWith('/');

	if (!isAbsoluteHttpUrl)
		throw new InvalidOperationException("invalid configuration: Illusionist__PublicBaseUrl must be an absolute http(s) URL");
}

/// <summary>Exposed so <c>WebApplicationFactory&lt;Program&gt;</c> can host this service in-process for tests.</summary>
public partial class Program;

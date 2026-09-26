using ModelContextProtocol.Protocol;

namespace Illusionist.Service.Mcp;

/// <summary>The one resource template this service advertises via <c>resources/templates/list</c> (D3).</summary>
public static class ResourceTemplates
{
	/// <summary>
	/// <c>illusionist://series{?generator,seedMode,seed,symbol,drift,volatility,anchorDate,anchorPrice,timeframe,count,from,to,format}</c>,
	/// named <c>series</c>. Reading a URI built from this template regenerates the identical bytes; nothing is ever stored.
	/// </summary>
	public static IReadOnlyList<ResourceTemplate> All { get; } =
	[
		new ResourceTemplate
		{
			Name = "series",
			UriTemplate =
				"illusionist://series{?generator,seedMode,seed,symbol,drift,volatility,anchorDate,anchorPrice,timeframe,count,from,to,format}",
			Description =
				"A deterministic, synthetic OHLCV bar series. Reading a URI built from this template regenerates the identical bytes; nothing is ever stored.",
		},
	];
}

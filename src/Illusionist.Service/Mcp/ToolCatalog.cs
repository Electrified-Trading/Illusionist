using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace Illusionist.Service.Mcp;

/// <summary>
/// The three MCP tools this service exposes (D2): their names, agent-facing descriptions and
/// hand-written JSON Schemas. Schemas are hand-written, not derived from
/// <c>Illusionist.Service.Generators.GeneratorDescriptor</c>, because the tool surface is
/// generator-registry-agnostic (one shared <c>illusionist_series</c> schema across every
/// registered generator), while a descriptor is per-version informational content for
/// <c>illusionist_generators</c>/<c>illusionist_describe</c>.
/// </summary>
public static class ToolCatalog
{
	public const string Generators = "illusionist_generators";
	public const string Describe = "illusionist_describe";
	public const string Series = "illusionist_series";

	private static readonly ToolAnnotations ReadOnlyAnnotations = new()
	{
		ReadOnlyHint = true,
		IdempotentHint = true,
		DestructiveHint = false,
		OpenWorldHint = false,
	};

	/// <summary>The three tools, in the order <c>tools/list</c> reports them.</summary>
	public static IReadOnlyList<Tool> Tools { get; } =
	[
		new Tool
		{
			Name = Generators,
			Description =
				"List the generator versions this service runs (currently brownian-bridge@1), with every parameter's type, default and allowed range, the supported timeframes, and the per-call limits. The list only grows: old versions stay callable forever.",
			InputSchema = ParseSchema("""{"type":"object","additionalProperties":false}"""),
			Annotations = ReadOnlyAnnotations,
		},

		new Tool
		{
			Name = Describe,
			Description =
				"Return the reproducibility contract of one generator version: the exact key that determines its output, what is guaranteed, where the guarantee stops (hosts, calendar years), the limits, and whether this host passed its golden self-check. Read it before relying on regenerated bars matching earlier ones.",
			InputSchema = ParseSchema("""
				{
				  "type": "object",
				  "properties": {
				    "generator": {
				      "type": "string",
				      "pattern": "^[a-z0-9]+(-[a-z0-9]+)*@[1-9][0-9]*$",
				      "description": "Generator and version, e.g. 'brownian-bridge@1'."
				    }
				  },
				  "required": ["generator"],
				  "additionalProperties": false
				}
				"""),
			Annotations = ReadOnlyAnnotations,
		},

		new Tool
		{
			Name = Series,
			Description =
				"Generate a synthetic, market-like OHLCV bar series (never real market data). Deterministic: the same arguments always produce the same bytes, forever, so store the returned uri instead of the bars. Results up to 16 KiB come back inline after a one-line JSON envelope; larger results come back as an illusionist:// resource link plus a REST path, both of which regenerate the identical bytes when read. At most 20000 bars per call. Unknown or misspelled arguments are rejected, never ignored.",
			InputSchema = ParseSchema(SeriesInputSchemaJson),
			Annotations = ReadOnlyAnnotations,
		},
	];

	private static JsonElement ParseSchema(string json)
		=> JsonDocument.Parse(json).RootElement.Clone();

	private const string SeriesInputSchemaJson = """
		{
		  "type": "object",
		  "properties": {
		    "generator": {
		      "type": "string",
		      "description": "Generator and version, e.g. 'brownian-bridge@1'. Always explicit: a version's output never changes."
		    },
		    "seedMode": {
		      "type": "string",
		      "enum": ["bare", "symbol-hashed"],
		      "default": "bare",
		      "description": "'bare': the seed alone picks the path. 'symbol-hashed': the seed is combined with a hash of symbol, so one seed gives a different path per symbol."
		    },
		    "seed": {
		      "type": "integer",
		      "minimum": -2147483648,
		      "maximum": 2147483647,
		      "description": "Any 32-bit integer; different seeds give independent paths."
		    },
		    "symbol": {
		      "type": "string",
		      "pattern": "^[A-Za-z0-9._-]{1,32}$",
		      "description": "A label hashed into the seed (symbol-hashed mode only). Not a lookup: output has nothing to do with the real instrument."
		    },
		    "drift": {
		      "type": "number",
		      "minimum": -0.5,
		      "maximum": 0.5,
		      "default": 0.0001,
		      "description": "Annualized log drift (0.08 approximately +8%/yr)."
		    },
		    "volatility": {
		      "type": "number",
		      "minimum": 0,
		      "maximum": 1,
		      "default": 0.01,
		      "description": "Annualized volatility; typical stocks 0.2-0.5. The default 0.01 is the reference geometry and looks nearly flat."
		    },
		    "anchorDate": {
		      "type": "string",
		      "format": "date",
		      "default": "2023-03-01",
		      "description": "Date of the first bar (09:30), where the path starts at anchorPrice. Must be a weekday that is not a 2024-2025 U.S. market holiday."
		    },
		    "anchorPrice": {
		      "type": "number",
		      "minimum": 0.01,
		      "maximum": 100000,
		      "default": 100,
		      "description": "Price at the anchor bar's open."
		    },
		    "timeframe": {
		      "type": "string",
		      "enum": ["1d"],
		      "default": "1d",
		      "description": "Bar interval. brownian-bridge@1 supports '1d' only."
		    },
		    "count": {
		      "type": "integer",
		      "minimum": 1,
		      "maximum": 20000,
		      "default": 60,
		      "description": "Number of bars starting at the anchor. Use count or from/to, not both."
		    },
		    "from": {
		      "type": "string",
		      "format": "date",
		      "description": "First date of a range (inclusive). Bars fall on every trading day in [from, to], before or after the anchor."
		    },
		    "to": {
		      "type": "string",
		      "format": "date",
		      "description": "Last date of the range (inclusive)."
		    },
		    "format": {
		      "type": "string",
		      "enum": ["csv", "json"],
		      "default": "csv",
		      "description": "'csv': Index,TimestampTicks,Open,High,Low,Close,Volume (the golden layout; TimestampTicks are .NET ticks). 'json': rows of [timestamp, open, high, low, close, volume] with ISO timestamps."
		    }
		  },
		  "required": ["generator", "seed"],
		  "additionalProperties": false
		}
		""";
}

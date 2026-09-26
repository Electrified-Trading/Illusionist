using System.Text.Json;

namespace Illusionist.Service.Series;

/// <summary>
/// The query-string mechanics of a series key (D1): turning REST query strings, resource-URI
/// suffixes and MCP tool argument dictionaries into one canonical name/value shape, and turning a
/// validated <see cref="SeriesKey"/> back into its one canonical string.
/// </summary>
public static partial class SeriesQuery
{
	/// <summary>Every parameter name this service understands, in canonical order.</summary>
	public static readonly IReadOnlyList<string> KnownNames =
	[
		"generator", "seedMode", "seed", "symbol", "drift", "volatility",
		"anchorDate", "anchorPrice", "timeframe", "count", "from", "to", "format",
	];

	/// <summary>
	/// Splits raw query text (a REST query string, with or without its leading <c>?</c>, or a
	/// resource URI's suffix after <c>illusionist://series?</c>) on <c>&amp;</c> and the first
	/// <c>=</c>, decoding each side with <see cref="Uri.UnescapeDataString"/>.
	/// </summary>
	/// <exception cref="IllusionistErrorException">An unknown or duplicate parameter name was found.</exception>
	public static IReadOnlyDictionary<string, string> ParseQueryText(string queryText)
	{
		var text = queryText.Length > 0 && queryText[0] == '?' ? queryText[1..] : queryText;
		var pairs = new Dictionary<string, string>(StringComparer.Ordinal);
		if (text.Length == 0)
			return pairs;

		foreach (var part in text.Split('&'))
		{
			if (part.Length == 0)
				continue;

			var eq = part.IndexOf('=');
			var rawName = eq < 0 ? part : part[..eq];
			var rawValue = eq < 0 ? string.Empty : part[(eq + 1)..];
			Add(pairs, Uri.UnescapeDataString(rawName), Uri.UnescapeDataString(rawValue));
		}

		return pairs;
	}

	/// <summary>
	/// Maps an MCP tool call's argument dictionary into the same name/value shape: a JSON string
	/// becomes its value, a JSON number becomes <see cref="JsonElement.GetRawText"/>, null counts as
	/// absent, and any other kind is <c>invalid_parameter</c>.
	/// </summary>
	/// <exception cref="IllusionistErrorException">An unknown or duplicate parameter name was found, or a value was not a string or number.</exception>
	public static IReadOnlyDictionary<string, string> ParseToolArguments(IEnumerable<KeyValuePair<string, JsonElement>>? arguments)
	{
		var pairs = new Dictionary<string, string>(StringComparer.Ordinal);
		if (arguments is null)
			return pairs;

		foreach (var (name, element) in arguments)
		{
			if (element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined)
				continue;

			var value = element.ValueKind switch
			{
				JsonValueKind.String => element.GetString() ?? string.Empty,
				JsonValueKind.Number => element.GetRawText(),
				_ => throw new IllusionistErrorException(
					IllusionistError.InvalidParameter(name, "a string or number", element.GetRawText())),
			};

			Add(pairs, name, value);
		}

		return pairs;
	}

	private static void Add(Dictionary<string, string> pairs, string name, string value)
	{
		if (!KnownNames.Contains(name, StringComparer.Ordinal))
			throw new IllusionistErrorException(IllusionistError.UnknownParameter(name, KnownNames));

		if (!pairs.TryAdd(name, value))
			throw new IllusionistErrorException(IllusionistError.DuplicateParameter(name));
	}
}

namespace Illusionist.Service.Tests.Support;

/// <summary>Builds an <c>illusionist_series</c> argument dictionary or query string for one <see cref="GoldenCase"/>, against an explicit generator version -- never defaulted, so a V2 (or later) test can never accidentally exercise V1's wire path instead.</summary>
internal static class GoldenCaseArguments
{
	public static Dictionary<string, object?> ToToolArguments(this GoldenCase @case, GeneratorRef generator)
	{
		var arguments = new Dictionary<string, object?>
		{
			["generator"] = generator.ToString(),
			["seed"] = @case.Seed,
			["count"] = @case.Count,
			["drift"] = @case.Drift,
			["volatility"] = @case.Volatility,
			["anchorPrice"] = (double)@case.AnchorPrice,
			["anchorDate"] = @case.AnchorDate.ToString("yyyy-MM-dd"),
			["timeframe"] = @case.Timeframe,
			["format"] = @case.Format == SeriesFormat.Csv ? "csv" : "json",
		};

		if (@case.SeedMode == SeedMode.SymbolHashed)
		{
			arguments["seedMode"] = "symbol-hashed";
			arguments["symbol"] = @case.Symbol;
		}

		return arguments;
	}

	public static string ToQueryString(this GoldenCase @case, GeneratorRef generator)
	{
		var key = @case.ToKey(generator);
		return SeriesQuery.Canonical(key);
	}
}

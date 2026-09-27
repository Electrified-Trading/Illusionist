namespace Illusionist.Service.Tests.Support;

/// <summary>Builds an <c>illusionist_series</c> argument dictionary or query string for one <see cref="GoldenCase"/>.</summary>
internal static class GoldenCaseArguments
{
	public static Dictionary<string, object?> ToToolArguments(this GoldenCase @case)
	{
		var arguments = new Dictionary<string, object?>
		{
			["generator"] = "brownian-bridge@1",
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

	public static string ToQueryString(this GoldenCase @case)
	{
		var key = @case.ToKey(new GeneratorRef("brownian-bridge", 1));
		return SeriesQuery.Canonical(key);
	}
}

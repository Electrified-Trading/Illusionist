using System.Globalization;
using System.Text.RegularExpressions;
using Illusionist.Service.Generators;

namespace Illusionist.Service.Series;

/// <summary>
/// Turns a name/value shape (from <see cref="SeriesQuery"/>) into a validated
/// <see cref="SeriesKey"/>, in the exact D5 field order: the first failure wins. Readiness and the
/// unknown/duplicate-name check both happen before this runs (readiness in <c>SeriesService</c>,
/// names in <see cref="SeriesQuery"/> itself) -- this type only ever sees an already-deduplicated,
/// all-known parameter set.
/// </summary>
public static partial class SeriesKeyParser
{
	[GeneratedRegex("^[A-Za-z0-9._-]{1,32}$")]
	private static partial Regex SymbolPattern();

	/// <summary>The daily equities schedule, used only to answer "is this date a trading day?" -- the same schedule <see cref="BrownianBridgeV1"/> opens.</summary>
	private static readonly ISchedule TradingDaySchedule = new DefaultEquitiesScheduleFactory().GetSchedule(BarInterval.Day(1));

	/// <summary>Validates and builds the key. Throws <see cref="IllusionistErrorException"/> on the first failure.</summary>
	public static SeriesKey Parse(IReadOnlyDictionary<string, string> raw, IGeneratorRegistry registry)
	{
		var version = ParseGenerator(raw, registry);
		var seedMode = ParseSeedMode(raw);
		var seed = ParseSeed(raw);
		var symbol = ParseSymbol(raw, seedMode);
		var drift = ParseDrift(raw);
		var volatility = ParseVolatility(raw);
		var anchorDate = ParseAnchorDate(raw);
		var anchorPrice = ParseAnchorPrice(raw);
		var timeframe = ParseTimeframe(raw, version);
		var extent = ParseExtent(raw, version, seedMode, seed, symbol, drift, volatility, anchorDate, anchorPrice, timeframe);
		var format = ParseFormat(raw);

		return new SeriesKey(version.Ref, seedMode, seed, symbol, drift, volatility, anchorDate, anchorPrice, timeframe, extent, format);
	}

	private static IGeneratorVersion ParseGenerator(IReadOnlyDictionary<string, string> raw, IGeneratorRegistry registry)
	{
		if (!raw.TryGetValue("generator", out var text))
			throw new IllusionistErrorException(IllusionistError.MissingParameter("generator", ErrorMessages.MissingParameter.Generator));

		if (!GeneratorRef.TryParse(text, out var reference))
			throw new IllusionistErrorException(IllusionistError.InvalidGeneratorRef(text));

		if (!registry.HasId(reference.Id))
			throw new IllusionistErrorException(IllusionistError.UnknownGenerator(reference.Id, registry.AllRefs));

		if (!registry.TryGet(reference, out var version))
			throw new IllusionistErrorException(IllusionistError.UnknownGeneratorVersion(reference.Id, reference.Version, registry.AllRefs));

		return version!;
	}

	private static SeedMode ParseSeedMode(IReadOnlyDictionary<string, string> raw)
	{
		if (!raw.TryGetValue("seedMode", out var text))
			return SeedMode.Bare;

		return text switch
		{
			"bare" => SeedMode.Bare,
			"symbol-hashed" => SeedMode.SymbolHashed,
			_ => throw new IllusionistErrorException(IllusionistError.InvalidParameterEnum("seedMode", ["bare", "symbol-hashed"], text)),
		};
	}

	private static int ParseSeed(IReadOnlyDictionary<string, string> raw)
	{
		if (!raw.TryGetValue("seed", out var text))
			throw new IllusionistErrorException(IllusionistError.MissingParameter("seed", ErrorMessages.MissingParameter.Seed));

		if (!int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var seed))
			throw new IllusionistErrorException(IllusionistError.InvalidParameter("seed", ErrorMessages.InvalidParameterExpectation.Seed, text));

		return seed;
	}

	private static string? ParseSymbol(IReadOnlyDictionary<string, string> raw, SeedMode seedMode)
	{
		var hasSymbol = raw.TryGetValue("symbol", out var text);

		if (seedMode == SeedMode.SymbolHashed)
		{
			if (!hasSymbol)
				throw new IllusionistErrorException(IllusionistError.MissingParameter("symbol", ErrorMessages.MissingParameter.Symbol));

			if (!SymbolPattern().IsMatch(text!))
				throw new IllusionistErrorException(IllusionistError.InvalidParameter("symbol", ErrorMessages.InvalidParameterExpectation.Symbol, text!));

			return text;
		}

		if (hasSymbol)
			throw new IllusionistErrorException(IllusionistError.ConflictingParameters("symbol", ErrorMessages.ConflictingParameters.Symbol));

		return null;
	}

	private static double ParseDrift(IReadOnlyDictionary<string, string> raw)
		=> raw.TryGetValue("drift", out var text)
			? ParseFiniteDouble("drift", text, -0.5, 0.5)
			: 0.0001;

	private static double ParseVolatility(IReadOnlyDictionary<string, string> raw)
		=> raw.TryGetValue("volatility", out var text)
			? ParseFiniteDouble("volatility", text, 0.0, 1.0)
			: 0.01;

	private static double ParseFiniteDouble(string name, string text, double min, double max)
	{
		if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
			|| double.IsNaN(value)
			|| double.IsInfinity(value))
		{
			throw new IllusionistErrorException(
				IllusionistError.InvalidParameter(name, ErrorMessages.InvalidParameterExpectation.DriftOrVolatility, text));
		}

		if (value < min || value > max)
		{
			throw new IllusionistErrorException(IllusionistError.OutOfRange(
				name, min.ToString(CultureInfo.InvariantCulture), max.ToString(CultureInfo.InvariantCulture), text));
		}

		return value;
	}

	private static DateOnly ParseAnchorDate(IReadOnlyDictionary<string, string> raw)
	{
		var date = raw.TryGetValue("anchorDate", out var text)
			? ParseDate("anchorDate", text)
			: new DateOnly(2023, 3, 1);

		if (!TradingDaySchedule.IsValidBarTime(date.ToDateTime(DefaultEquitiesSchedule.MarketOpen)))
			throw new IllusionistErrorException(IllusionistError.NotATradingDay(date));

		return date;
	}

	private static decimal ParseAnchorPrice(IReadOnlyDictionary<string, string> raw)
		=> raw.TryGetValue("anchorPrice", out var text)
			? ParsePlainDecimal("anchorPrice", text, 0.01m, 100_000m)
			: 100m;

	private static string ParseTimeframe(IReadOnlyDictionary<string, string> raw, IGeneratorVersion version)
	{
		if (!raw.TryGetValue("timeframe", out var text))
			return version.Descriptor.Timeframes[0];

		if (!version.Descriptor.Timeframes.Contains(text, StringComparer.Ordinal))
			throw new IllusionistErrorException(IllusionistError.InvalidParameterEnum("timeframe", version.Descriptor.Timeframes, text));

		return text;
	}

	/// <summary>
	/// Validates <c>count</c> or <c>from</c>/<c>to</c> (exactly one, D5's conflicting/missing checks),
	/// then for a range, walks the generator's own schedule (via a candidate key) to find whether it
	/// holds more than <see cref="ServiceLimits.MaxBars"/> -- holidays and weekends make that
	/// impossible to derive from the two dates alone.
	/// </summary>
	private static SeriesExtent ParseExtent(
		IReadOnlyDictionary<string, string> raw,
		IGeneratorVersion version,
		SeedMode seedMode,
		int seed,
		string? symbol,
		double drift,
		double volatility,
		DateOnly anchorDate,
		decimal anchorPrice,
		string timeframe)
	{
		var hasCount = raw.TryGetValue("count", out var countText);
		var hasFrom = raw.TryGetValue("from", out var fromText);
		var hasTo = raw.TryGetValue("to", out var toText);

		if (hasCount && (hasFrom || hasTo))
			throw new IllusionistErrorException(IllusionistError.ConflictingParameters("count", ErrorMessages.ConflictingParameters.Count));

		if (hasFrom != hasTo)
			throw new IllusionistErrorException(IllusionistError.MissingParameter(hasFrom ? "to" : "from", ErrorMessages.MissingParameter.FromAndTo));

		if (hasFrom)
		{
			var from = ParseDate("from", fromText!);
			var to = ParseDate("to", toText!);
			if (from > to)
				throw new IllusionistErrorException(IllusionistError.InvalidRange(from, to));

			var range = new SeriesExtent.Range(from, to);
			var candidate = new SeriesKey(
				version.Ref, seedMode, seed, symbol, drift, volatility, anchorDate, anchorPrice, timeframe, range, SeriesFormat.Csv);

			if (version.Open(candidate).Timestamps(ServiceLimits.MaxBars) is null)
				throw new IllusionistErrorException(IllusionistError.TooManyBarsRange(from, to));

			return range;
		}

		var count = 60;
		if (hasCount)
		{
			if (!int.TryParse(countText, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out count))
				throw new IllusionistErrorException(IllusionistError.InvalidParameter("count", ErrorMessages.InvalidParameterExpectation.Count, countText!));

			if (count < 1)
			{
				throw new IllusionistErrorException(IllusionistError.OutOfRange(
					"count", "1", ServiceLimits.MaxBars.ToString(CultureInfo.InvariantCulture), countText!));
			}

			if (count > ServiceLimits.MaxBars)
				throw new IllusionistErrorException(IllusionistError.TooManyBarsCount(count));
		}

		return new SeriesExtent.Count(count);
	}

	private static SeriesFormat ParseFormat(IReadOnlyDictionary<string, string> raw)
	{
		if (!raw.TryGetValue("format", out var text))
			return SeriesFormat.Csv;

		return text switch
		{
			"csv" => SeriesFormat.Csv,
			"json" => SeriesFormat.Json,
			_ => throw new IllusionistErrorException(IllusionistError.InvalidParameterEnum("format", ["csv", "json"], text)),
		};
	}

	private static DateOnly ParseDate(string name, string text)
	{
		if (!DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
			throw new IllusionistErrorException(IllusionistError.InvalidParameter(name, ErrorMessages.InvalidParameterExpectation.Date, text));

		if (date < ServiceLimits.MinDate || date > ServiceLimits.MaxDate)
		{
			throw new IllusionistErrorException(IllusionistError.OutOfRange(
				name,
				ServiceLimits.MinDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
				ServiceLimits.MaxDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
				text));
		}

		return date;
	}

	private static decimal ParsePlainDecimal(string name, string text, decimal min, decimal max)
	{
		if (text.AsSpan().IndexOfAny('e', 'E') >= 0
			|| !decimal.TryParse(text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value))
		{
			throw new IllusionistErrorException(IllusionistError.InvalidParameter(name, ErrorMessages.InvalidParameterExpectation.AnchorPrice, text));
		}

		// Normalize decimal scale (100.00 and 100 are one key) -- see SeriesKey's own remarks.
		var normalized = decimal.Parse(value.ToString("0.############################", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

		if (normalized < min || normalized > max)
		{
			throw new IllusionistErrorException(IllusionistError.OutOfRange(
				name, min.ToString(CultureInfo.InvariantCulture), max.ToString(CultureInfo.InvariantCulture), text));
		}

		return normalized;
	}
}

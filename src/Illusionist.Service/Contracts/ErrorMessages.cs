using System.Globalization;

namespace Illusionist.Service.Contracts;

/// <summary>
/// Builds the deterministic error taxonomy's exact message text (D5 of the service design),
/// verbatim -- every surface reports the same words for the same condition.
/// </summary>
public static class ErrorMessages
{
	/// <summary>The exact <see cref="ErrorCodes.NotReady"/> message.</summary>
	public const string NotReady =
		"This host failed its golden self-check, so its output may not match the reproducibility contract. See /healthz.";

	/// <summary>The exact <see cref="ErrorCodes.InternalError"/> message.</summary>
	public const string InternalError = "Unexpected server error; it has been logged.";

	/// <summary>Exact text for each <see cref="ErrorCodes.MissingParameter"/> case.</summary>
	public static class MissingParameter
	{
		public const string Generator = "'generator' is required, as id@version, e.g. 'brownian-bridge@1'.";
		public const string Seed = "'seed' is required: any 32-bit integer.";
		public const string Symbol = "'symbol' is required when seedMode is 'symbol-hashed'.";
		public const string FromAndTo = "'from' and 'to' must be given together.";
	}

	/// <summary>Exact text for each <see cref="ErrorCodes.ConflictingParameters"/> case.</summary>
	public static class ConflictingParameters
	{
		public const string Symbol =
			"'symbol' is only used when seedMode is 'symbol-hashed'; remove it or set seedMode to 'symbol-hashed'.";
		public const string Count = "Use either 'count' or 'from' and 'to', not both.";
	}

	/// <summary>The fixed expectation phrase for each <see cref="ErrorCodes.InvalidParameter"/> field.</summary>
	public static class InvalidParameterExpectation
	{
		public const string Seed = "a 32-bit integer";
		public const string DriftOrVolatility = "a finite number";
		public const string AnchorPrice = "a plain decimal number such as 100 or 12.5";
		public const string Date = "a date written yyyy-MM-dd";
		public const string Count = "an integer";
		public const string Symbol = "1-32 characters from A-Z, a-z, 0-9, '.', '_' or '-'";
	}

	public static string UnknownParameter(string name, IEnumerable<string> knownNamesInOrder)
		=> $"Unknown parameter '{name}'. Known parameters: {string.Join(", ", knownNamesInOrder)}.";

	public static string DuplicateParameter(string name)
		=> $"Parameter '{name}' was given more than once.";

	public static string InvalidGeneratorRef(string value)
		=> $"'generator' must be id@version, e.g. 'brownian-bridge@1'; got '{Truncate(value)}'.";

	public static string UnknownGenerator(string id, IEnumerable<string> availableRefs)
		=> $"No generator '{id}'. Available: {string.Join(", ", availableRefs)}.";

	public static string UnknownGeneratorVersion(string id, int version, IEnumerable<string> availableRefs)
		=> $"'{id}' has no version {version.ToString(CultureInfo.InvariantCulture)}. Available: {string.Join(", ", availableRefs)}.";

	public static string InvalidParameterEnum(string name, IEnumerable<string> allowed, string value)
		=> $"'{name}' must be one of: {string.Join(", ", allowed)}; got '{Truncate(value)}'.";

	public static string InvalidParameter(string name, string expectation, string value)
		=> $"'{name}' must be {expectation}; got '{Truncate(value)}'.";

	public static string OutOfRange(string name, string min, string max, string value)
		=> $"'{name}' must be between {min} and {max}; got {value}.";

	public static string NotATradingDay(DateOnly date)
		=> $"anchorDate {date:yyyy-MM-dd} is not a trading day (a weekend, or a U.S. market holiday in the 2024-2025 calendar).";

	public static string InvalidRange(DateOnly from, DateOnly to)
		=> $"'from' ({from:yyyy-MM-dd}) is after 'to' ({to:yyyy-MM-dd}).";

	public static string TooManyBarsCount(int value)
		=> $"'count' {value.ToString(CultureInfo.InvariantCulture)} exceeds the limit of {ServiceLimits.MaxBars.ToString(CultureInfo.InvariantCulture)} bars per call; split the request into from/to ranges.";

	public static string TooManyBarsRange(DateOnly from, DateOnly to)
		=> $"The range {from:yyyy-MM-dd}..{to:yyyy-MM-dd} holds more than {ServiceLimits.MaxBars.ToString(CultureInfo.InvariantCulture)} bars (the limit per call); narrow it.";

	public static string PriceOutOfRange(int index, DateOnly date)
		=> $"These parameters push the price outside the representable range at bar {index.ToString(CultureInfo.InvariantCulture)} ({date:yyyy-MM-dd}); lower volatility, drift, anchorPrice or the number of bars.";

	public static string InvalidResourceUri(string value)
		=> $"Not an Illusionist series URI: '{Truncate(value)}'. Expected illusionist://series?<query>.";

	/// <summary>Cuts a raw received value to 64 characters plus <c>...</c>, per D5's placeholder rule.</summary>
	public static string Truncate(string value)
		=> value.Length <= 64
			? value
			: string.Concat(value.AsSpan(0, 64), "...");
}

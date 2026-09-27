namespace Illusionist.Service.Contracts;

/// <summary>
/// The deterministic error taxonomy's codes. Every surface (MCP tool, MCP resource, REST) reports
/// the same code for the same condition; see <see cref="IllusionistError"/> for the exact HTTP
/// status attached to each and <see cref="ErrorMessages"/> for the exact text.
/// </summary>
public static class ErrorCodes
{
	/// <summary>The host failed its golden self-check.</summary>
	public const string NotReady = "not_ready";

	/// <summary>A parameter name is not one this endpoint understands.</summary>
	public const string UnknownParameter = "unknown_parameter";

	/// <summary>A parameter name was given more than once.</summary>
	public const string DuplicateParameter = "duplicate_parameter";

	/// <summary>A required parameter is absent.</summary>
	public const string MissingParameter = "missing_parameter";

	/// <summary><c>generator</c> is not well-formed as <c>id@version</c>.</summary>
	public const string InvalidGeneratorRef = "invalid_generator_ref";

	/// <summary>No generator with the given id is registered.</summary>
	public const string UnknownGenerator = "unknown_generator";

	/// <summary>The generator id is registered, but not at the requested version.</summary>
	public const string UnknownGeneratorVersion = "unknown_generator_version";

	/// <summary>A parameter's value is not well-formed for its type.</summary>
	public const string InvalidParameter = "invalid_parameter";

	/// <summary>A well-formed value falls outside its allowed range.</summary>
	public const string OutOfRange = "out_of_range";

	/// <summary>Two parameters were both given, but only one may be.</summary>
	public const string ConflictingParameters = "conflicting_parameters";

	/// <summary><c>anchorDate</c> is a weekend or a known holiday.</summary>
	public const string NotATradingDay = "not_a_trading_day";

	/// <summary><c>from</c> is after <c>to</c>.</summary>
	public const string InvalidRange = "invalid_range";

	/// <summary>The requested bar count or range exceeds <see cref="ServiceLimits.MaxBars"/>.</summary>
	public const string TooManyBars = "too_many_bars";

	/// <summary>Generation produced a non-representable or non-positive price.</summary>
	public const string PriceOutOfRange = "price_out_of_range";

	/// <summary>An MCP resource URI is not an <c>illusionist://series</c> URI.</summary>
	public const string InvalidResourceUri = "invalid_resource_uri";

	/// <summary>An unexpected server error, already logged.</summary>
	public const string InternalError = "internal_error";
}

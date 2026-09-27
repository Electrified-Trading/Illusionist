using System.Text;
using System.Text.Json;

namespace Illusionist.Service.Contracts;

/// <summary>
/// The one error shape every surface (MCP tool, MCP resource, REST) reports: a stable
/// <paramref name="Code"/>, a human/agent-readable <paramref name="Message"/>, the offending
/// <paramref name="Parameter"/> when one applies, and the <paramref name="HttpStatus"/> the REST
/// twin answers with (unused by MCP surfaces, which map the same code their own way -- see
/// <c>McpHandlers</c>).
/// </summary>
public sealed record IllusionistError(string Code, string Message, string? Parameter, int HttpStatus)
{
	/// <summary>Serializes as <c>{"error":{"code":…,"message":…,"parameter":…}}</c>, <c>parameter</c> omitted when absent.</summary>
	public string ToJson()
	{
		using var stream = new MemoryStream();
		using (var writer = new Utf8JsonWriter(stream, IllusionistJson.WriterOptions))
		{
			writer.WriteStartObject();
			writer.WriteStartObject("error");
			writer.WriteString("code", Code);
			writer.WriteString("message", Message);
			if (Parameter is not null)
				writer.WriteString("parameter", Parameter);

			writer.WriteEndObject();
			writer.WriteEndObject();
		}

		return Encoding.UTF8.GetString(stream.ToArray());
	}

	public static IllusionistError NotReady()
		=> new(ErrorCodes.NotReady, ErrorMessages.NotReady, Parameter: null, HttpStatus: 503);

	public static IllusionistError UnknownParameter(string name, IEnumerable<string> knownNamesInOrder)
		=> new(ErrorCodes.UnknownParameter, ErrorMessages.UnknownParameter(name, knownNamesInOrder), name, HttpStatus: 400);

	public static IllusionistError DuplicateParameter(string name)
		=> new(ErrorCodes.DuplicateParameter, ErrorMessages.DuplicateParameter(name), name, HttpStatus: 400);

	public static IllusionistError MissingParameter(string parameter, string message)
		=> new(ErrorCodes.MissingParameter, message, parameter, HttpStatus: 400);

	public static IllusionistError InvalidGeneratorRef(string value)
		=> new(ErrorCodes.InvalidGeneratorRef, ErrorMessages.InvalidGeneratorRef(value), "generator", HttpStatus: 400);

	public static IllusionistError UnknownGenerator(string id, IEnumerable<string> availableRefs)
		=> new(ErrorCodes.UnknownGenerator, ErrorMessages.UnknownGenerator(id, availableRefs), "generator", HttpStatus: 404);

	public static IllusionistError UnknownGeneratorVersion(string id, int version, IEnumerable<string> availableRefs)
		=> new(ErrorCodes.UnknownGeneratorVersion, ErrorMessages.UnknownGeneratorVersion(id, version, availableRefs), "generator", HttpStatus: 404);

	public static IllusionistError InvalidParameterEnum(string name, IEnumerable<string> allowed, string value)
		=> new(ErrorCodes.InvalidParameter, ErrorMessages.InvalidParameterEnum(name, allowed, value), name, HttpStatus: 400);

	public static IllusionistError InvalidParameter(string name, string expectation, string value)
		=> new(ErrorCodes.InvalidParameter, ErrorMessages.InvalidParameter(name, expectation, value), name, HttpStatus: 400);

	public static IllusionistError OutOfRange(string name, string min, string max, string value)
		=> new(ErrorCodes.OutOfRange, ErrorMessages.OutOfRange(name, min, max, value), name, HttpStatus: 400);

	public static IllusionistError ConflictingParameters(string parameter, string message)
		=> new(ErrorCodes.ConflictingParameters, message, parameter, HttpStatus: 400);

	public static IllusionistError NotATradingDay(DateOnly date)
		=> new(ErrorCodes.NotATradingDay, ErrorMessages.NotATradingDay(date), "anchorDate", HttpStatus: 400);

	public static IllusionistError InvalidRange(DateOnly from, DateOnly to)
		=> new(ErrorCodes.InvalidRange, ErrorMessages.InvalidRange(from, to), "to", HttpStatus: 400);

	public static IllusionistError TooManyBarsCount(int value)
		=> new(ErrorCodes.TooManyBars, ErrorMessages.TooManyBarsCount(value), "count", HttpStatus: 400);

	public static IllusionistError TooManyBarsRange(DateOnly from, DateOnly to)
		=> new(ErrorCodes.TooManyBars, ErrorMessages.TooManyBarsRange(from, to), "to", HttpStatus: 400);

	public static IllusionistError PriceOutOfRange(int index, DateOnly date)
		=> new(ErrorCodes.PriceOutOfRange, ErrorMessages.PriceOutOfRange(index, date), Parameter: null, HttpStatus: 422);

	public static IllusionistError InvalidResourceUri(string value)
		=> new(ErrorCodes.InvalidResourceUri, ErrorMessages.InvalidResourceUri(value), Parameter: null, HttpStatus: 400);

	public static IllusionistError InternalError()
		=> new(ErrorCodes.InternalError, ErrorMessages.InternalError, Parameter: null, HttpStatus: 500);
}

/// <summary>
/// Carries an <see cref="Contracts.IllusionistError"/> across the boundary between parsing/generation
/// and whichever surface (MCP tool, MCP resource, REST) is serving the request -- each surface
/// converts it its own way (D5); this type is never itself serialized.
/// </summary>
public sealed class IllusionistErrorException(IllusionistError error) : Exception(error.Message)
{
	/// <summary>The structured error this exception carries.</summary>
	public IllusionistError Error { get; } = error;
}

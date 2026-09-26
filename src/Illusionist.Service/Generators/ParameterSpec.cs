namespace Illusionist.Service.Generators;

/// <summary>
/// One <c>illusionist_series</c> parameter's shape, as reported by <c>illusionist_generators</c>
/// and used to render each generator's own row in the JSON Schema (<c>Illusionist.Service.Mcp.ToolCatalog</c>).
/// </summary>
/// <param name="Name">The parameter's name, e.g. <c>drift</c>.</param>
/// <param name="Type">The JSON Schema type: <c>string</c>, <c>integer</c> or <c>number</c>.</param>
/// <param name="Default">The default value's raw JSON literal text (a quoted string for <see cref="Type"/> <c>string</c>, otherwise a bare number), or <see langword="null"/> when required.</param>
/// <param name="Minimum">The minimum, as raw text, or <see langword="null"/>.</param>
/// <param name="Maximum">The maximum, as raw text, or <see langword="null"/>.</param>
/// <param name="Enum">The allowed values, when the parameter is an enum.</param>
/// <param name="Pattern">The validating regular expression, when the parameter is pattern-constrained.</param>
/// <param name="Required">Whether the parameter must always be given.</param>
/// <param name="Description">The agent-facing description (D2's table, verbatim).</param>
public sealed record ParameterSpec(
	string Name,
	string Type,
	string? Default,
	string? Minimum,
	string? Maximum,
	IReadOnlyList<string>? Enum,
	string? Pattern,
	bool Required,
	string Description);

namespace Illusionist.Service.Generators;

/// <summary>
/// A generator version's own metadata: what it is called, what it accepts, and what it supports --
/// the content of <c>illusionist_generators</c>' list entries and the basis for
/// <c>illusionist_describe</c> and the tool's JSON Schema.
/// </summary>
/// <param name="Ref">The generator and version this describes.</param>
/// <param name="Summary">A one-line, agent-facing summary.</param>
/// <param name="Timeframes">The bar intervals this version supports.</param>
/// <param name="Parameters">Every <c>illusionist_series</c> parameter, in table order.</param>
public sealed record GeneratorDescriptor(
	GeneratorRef Ref,
	string Summary,
	IReadOnlyList<string> Timeframes,
	IReadOnlyList<ParameterSpec> Parameters);

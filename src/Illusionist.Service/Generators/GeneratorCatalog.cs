namespace Illusionist.Service.Generators;

/// <summary>
/// The explicit list of every generator version this service ever registers. Adding a version
/// means adding an entry here (and its own golden fixtures); an existing entry is never edited in
/// place (D7).
/// </summary>
public static class GeneratorCatalog
{
	/// <summary>Every generator version this service runs, in registration order.</summary>
	public static IReadOnlyList<IGeneratorVersion> All { get; } = [BrownianBridgeV1.Instance];
}

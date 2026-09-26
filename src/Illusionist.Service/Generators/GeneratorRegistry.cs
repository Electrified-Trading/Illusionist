namespace Illusionist.Service.Generators;

/// <summary>
/// The explicit, exact-match generator registry (D7). Every entry must carry at least one golden
/// case (D7, forever rule 4) -- a version with nothing pinning its output has no business being
/// published.
/// </summary>
public sealed class GeneratorRegistry : IGeneratorRegistry
{
	private readonly Dictionary<GeneratorRef, IGeneratorVersion> _byRef;
	private readonly HashSet<string> _ids;

	/// <summary>Builds the registry from <paramref name="versions"/>, validating each has a golden case.</summary>
	public GeneratorRegistry(IReadOnlyList<IGeneratorVersion> versions)
	{
		All = versions;
		AllRefs = [.. versions.Select(v => v.Ref.ToString())];
		_byRef = versions.ToDictionary(v => v.Ref);
		_ids = [.. versions.Select(v => v.Ref.Id)];

		foreach (var version in versions)
		{
			if (version.GoldenCases.Count == 0)
				throw new InvalidOperationException($"Generator '{version.Ref}' has no golden case; every registered version must be pinned.");
		}
	}

	/// <inheritdoc />
	public IReadOnlyList<IGeneratorVersion> All { get; }

	/// <inheritdoc />
	public IReadOnlyList<string> AllRefs { get; }

	/// <inheritdoc />
	public bool HasId(string id)
		=> _ids.Contains(id);

	/// <inheritdoc />
	public bool TryGet(GeneratorRef reference, out IGeneratorVersion? version)
		=> _byRef.TryGetValue(reference, out version);
}

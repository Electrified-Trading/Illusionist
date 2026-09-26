namespace Illusionist.Service.Generators;

/// <summary>
/// Looks up registered generator versions by their exact <see cref="GeneratorRef"/>. There is no
/// "latest" alias: a caller that omits the version gets <c>invalid_generator_ref</c>, never a
/// silently-resolved current version (D7, forever rule 1).
/// </summary>
public interface IGeneratorRegistry
{
	/// <summary>Every registered version, in registration order.</summary>
	IReadOnlyList<IGeneratorVersion> All { get; }

	/// <summary>Every registered version's <see cref="GeneratorRef"/>, rendered as <c>id@version</c>, in registration order.</summary>
	IReadOnlyList<string> AllRefs { get; }

	/// <summary>Whether any version of generator <paramref name="id"/> is registered.</summary>
	bool HasId(string id);

	/// <summary>Looks up the exact version. <see langword="false"/> when <paramref name="reference"/> is not registered.</summary>
	bool TryGet(GeneratorRef reference, out IGeneratorVersion? version);
}

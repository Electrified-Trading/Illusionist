namespace Illusionist.Service.Generators;

/// <summary>
/// One registered, frozen generator version. Old versions stay callable forever: an
/// output-changing algorithm gets a new <see cref="Ref"/> and its own registration (D7), never an
/// edit to an existing one.
/// </summary>
public interface IGeneratorVersion
{
	/// <summary>This version's identity, e.g. <c>brownian-bridge@1</c>.</summary>
	GeneratorRef Ref { get; }

	/// <summary>This version's metadata and parameter table.</summary>
	GeneratorDescriptor Descriptor { get; }

	/// <summary>
	/// The golden cases this version must reproduce byte for byte on every host. These are the
	/// cases tied to a committed fixture file under <c>tests/Golden/Fixtures/</c> and to
	/// <c>GoldenBarSeriesCases.All</c> one-to-one (see <c>GoldenManifestTests</c>) -- never add a
	/// case here without also adding the fixture, since the two must stay in lockstep.
	/// </summary>
	IReadOnlyList<GoldenCase> GoldenCases { get; }

	/// <summary>
	/// Every case the readiness gate (<see cref="Illusionist.Service.SelfCheck.IGoldenSelfCheck"/>)
	/// actually verifies. Defaults to <see cref="GoldenCases"/>; a version may verify additional
	/// cases here (e.g. covering a format, drift, anchor date or extent shape none of the six
	/// fixture-backed cases exercise) that are pinned the same way -- a hash frozen once from the
	/// real generate-and-render path -- but are not tied to a committed fixture file, so they never
	/// have to move in lockstep with <c>tests/Golden/</c>.
	/// </summary>
	IReadOnlyList<GoldenCase> ReadinessCases => GoldenCases;

	/// <summary>Opens a stateless source for the given key.</summary>
	ISeriesSource Open(SeriesKey key);

	/// <summary>
	/// The one host platform (<see cref="Illusionist.Service.SelfCheck.HostPlatform"/>-formatted,
	/// e.g. <c>windows-x64</c>) this version's golden fixtures are proven byte-identical on, or
	/// <see langword="null"/> when this version is proven byte-identical on every host by
	/// construction (built only from IEEE-754-exact operations -- see
	/// <see cref="Illusionist.Core.Numerics.DeterministicMath"/>). A version with a reference
	/// platform is not required to reproduce its own golden cases anywhere else: the readiness gate
	/// (<see cref="Illusionist.Service.SelfCheck.IGoldenSelfCheck"/>) skips its cases entirely on a
	/// non-reference host, rather than blocking every other registered version's readiness on a
	/// platform gap this version admits to having.
	/// </summary>
	string? ReferencePlatform => null;
}

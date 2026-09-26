namespace Illusionist.Service.Generators;

/// <summary>
/// Optional companion to <see cref="IGeneratorVersion"/> supplying <c>illusionist_describe</c>'s
/// prose (D3): what is guaranteed, where the guarantee stops, and the known limitations. Kept off
/// <see cref="IGeneratorVersion"/> itself because that contract is fixed by D7 verbatim; a version
/// that does not implement this is described with an empty prose set rather than a cast failure.
/// </summary>
public interface IDescribableGenerator
{
	/// <summary>What every call is guaranteed to produce.</summary>
	IReadOnlyList<string> Guarantees { get; }

	/// <summary>Where the byte-identity guarantee stops (hosts, calendar years).</summary>
	string Scope { get; }

	/// <summary>Known, deliberate limitations of this version's model.</summary>
	IReadOnlyList<string> Limitations { get; }
}

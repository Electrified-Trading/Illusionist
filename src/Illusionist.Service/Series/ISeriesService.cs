namespace Illusionist.Service.Series;

/// <summary>
/// The one generate-and-render path every surface uses (D8): readiness, parsing, validation,
/// generation and rendering, or an already-valid key straight to generation for the golden
/// self-check, which deliberately bypasses the readiness gate (it is what produces the readiness
/// verdict in the first place).
/// </summary>
public interface ISeriesService
{
	/// <summary>
	/// The full pipeline: the readiness gate, <see cref="SeriesQuery"/> parsing, <see cref="SeriesKeyParser"/>
	/// validation, generation and rendering.
	/// </summary>
	/// <param name="rawParameters">The request's name/value parameters, already deduplicated and known-name-checked.</param>
	/// <param name="ready">Whether this host currently passes its golden self-check.</param>
	SeriesOutcome Generate(IReadOnlyDictionary<string, string> rawParameters, bool ready);

	/// <summary>Generation and rendering only, for an already-validated key. Bypasses the readiness gate.</summary>
	SeriesOutcome GenerateFromKey(SeriesKey key);
}

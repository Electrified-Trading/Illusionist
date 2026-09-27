namespace Illusionist.Service;

/// <summary>
/// Bound from the <c>Illusionist</c> configuration section (environment variable
/// <c>Illusionist__PublicBaseUrl</c>).
/// </summary>
public sealed class ServiceOptions
{
	/// <summary>
	/// An absolute <c>http(s)</c> URL with no trailing slash, used to make the envelope's
	/// <c>rest</c> field and REST's own generated links absolute. <see langword="null"/> when
	/// unset -- links are then relative (<c>/v1/series?...</c>).
	/// </summary>
	public string? PublicBaseUrl { get; set; }
}

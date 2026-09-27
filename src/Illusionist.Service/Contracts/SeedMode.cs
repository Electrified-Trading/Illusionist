namespace Illusionist.Service.Contracts;

/// <summary>
/// How a request's <c>seed</c> maps to the generator's internal seed.
/// </summary>
public enum SeedMode
{
	/// <summary>The seed alone picks the path; a symbol is never involved.</summary>
	Bare,

	/// <summary>The seed is combined with a hash of <c>symbol</c>, giving one path per symbol per seed.</summary>
	SymbolHashed,
}

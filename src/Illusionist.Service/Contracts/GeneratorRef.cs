using System.Globalization;

namespace Illusionist.Service.Contracts;

/// <summary>
/// Identifies one generator version, e.g. <c>brownian-bridge@1</c>. Old versions stay callable
/// forever (see <c>GeneratorCatalog</c>): a version's output never changes once published.
/// </summary>
/// <param name="Id">The generator's stable identifier, e.g. <c>brownian-bridge</c>.</param>
/// <param name="Version">The generator's version number, starting at 1.</param>
public readonly record struct GeneratorRef(string Id, int Version)
{
	/// <summary>Parses <c>id@version</c>, splitting at the last <c>@</c>.</summary>
	/// <returns><see langword="true"/> when <paramref name="text"/> is a well-formed reference.</returns>
	public static bool TryParse(string text, out GeneratorRef reference)
	{
		reference = default;

		var at = text.LastIndexOf('@');
		if (at <= 0 || at == text.Length - 1)
			return false;

		var id = text[..at];
		var versionText = text[(at + 1)..];
		if (!int.TryParse(versionText, NumberStyles.None, CultureInfo.InvariantCulture, out var version) || version < 1)
			return false;

		reference = new GeneratorRef(id, version);
		return true;
	}

	/// <summary>Renders as <c>id@version</c>.</summary>
	public override string ToString()
		=> string.Create(CultureInfo.InvariantCulture, $"{Id}@{Version}");
}

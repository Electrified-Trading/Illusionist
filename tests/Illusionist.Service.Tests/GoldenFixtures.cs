using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace Illusionist.Service.Tests;

/// <summary>
/// Resolves and reads the committed golden fixtures from this source file's own location, exactly
/// as <c>Illusionist.Tests.Golden.GeneratorGoldenTests</c> does, so editing a fixture takes effect
/// without a csproj copy-item or depending on the test runner's working directory.
/// </summary>
internal static class GoldenFixtures
{
	public static string GetPath(string fixtureName, [CallerFilePath] string sourceFilePath = "")
		=> Path.Combine(Path.GetDirectoryName(sourceFilePath)!, "..", "Golden", "Fixtures", fixtureName + ".csv");

	public static string ReadText(string fixtureName)
		=> File.ReadAllText(GetPath(fixtureName));

	public static string ComputeSha256(string text)
		=> Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
}

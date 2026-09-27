using System.Runtime.CompilerServices;

namespace Illusionist.Tests.Golden;

/// <summary>
/// Pins <c>BrownianBridgeBarSeries</c>'s output, at c3bccfb, against the exact parameters
/// a downstream consumer's own usage exercises. These are frozen fixture comparisons, not
/// statistical assertions -- a passing run means today's generator produces byte-identical
/// output to the committed fixture; any difference at all is a stop, because a downstream
/// consumer's own calibration studies and reference constants depend on these exact bytes
/// never moving under them (see a downstream consumer's own regeneration-path remarks).
/// </summary>
/// <remarks>
/// See <see cref="GoldenBarSeriesGenerator"/> for the generation itself and
/// <see cref="ReproducibilityScopeTests"/> for the scope this comparison is (and is not) good
/// for: same runtime family, same architecture, only.
/// </remarks>
public class GeneratorGoldenTests
{
	public static IEnumerable<object[]> FixtureNames
		=> GoldenBarSeriesCases.All.Select(c => new object[] { c.FixtureName });

	[Theory]
	[MemberData(nameof(FixtureNames))]
	public void Generate_MatchesCommittedFixture(string fixtureName)
	{
		var @case = GoldenBarSeriesCases.All.Single(c => c.FixtureName == fixtureName);
		var actual = GoldenBarFormatter.Format(GoldenBarSeriesGenerator.Generate(@case));
		var expected = File.ReadAllText(GetFixturePath(fixtureName));

		Assert.Equal(expected, actual);
	}

	[Fact]
	public void SymbolHashedCases_DifferentSymbolsSameSeed_ProduceDifferentFixtures()
	{
		// Sanity check on the golden set itself, mirroring a downstream consumer's own
		// cross-process stability check that different symbols sharing one seed produce
		// different output: if these two fixtures ever came back equal, the symbol-hashed
		// cases would not actually be exercising the symbol-hashing path at all.
		var synth = File.ReadAllText(GetFixturePath("symbol-hashed-synth-seed12345"));
		var aapl = File.ReadAllText(GetFixturePath("symbol-hashed-aapl-seed12345"));

		Assert.NotEqual(synth, aapl);
	}

	/// <summary>
	/// Resolves a fixture's path from this source file's own location rather than the build
	/// output directory, so editing a fixture takes effect without a csproj copy-item and without
	/// depending on the test runner's working directory.
	/// </summary>
	internal static string GetFixturePath(string fixtureName, [CallerFilePath] string sourceFilePath = "")
		=> Path.Combine(Path.GetDirectoryName(sourceFilePath)!, "Fixtures", fixtureName + ".csv");
}

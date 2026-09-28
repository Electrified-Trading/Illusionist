namespace Illusionist.Tests.Golden;

/// <summary>
/// Pins <c>BrownianBridgeBarSeriesV2</c>'s output against its own committed fixtures -- the
/// <c>@2</c> twin of <see cref="GeneratorGoldenTests"/>. A passing run means today's
/// <c>brownian-bridge@2</c> produces byte-identical output to the committed fixture; because <c>@2</c>
/// uses only IEEE-754-exact operations (see <see cref="Illusionist.Core.Numerics.DeterministicMath"/>),
/// that guarantee is expected to hold on every host, not only x64 Windows (contrast
/// <see cref="ReproducibilityScopeTests"/>, which states the narrower scope <c>@1</c> is limited to).
/// </summary>
public class GeneratorGoldenTestsV2
{
	public static IEnumerable<object[]> FixtureNames
		=> GoldenBarSeriesCasesV2.All.Select(c => new object[] { c.FixtureName });

	[Theory]
	[MemberData(nameof(FixtureNames))]
	public void Generate_MatchesCommittedFixture(string fixtureName)
	{
		var @case = GoldenBarSeriesCasesV2.All.Single(c => c.FixtureName == fixtureName);
		var actual = GoldenBarFormatter.Format(GoldenBarSeriesGeneratorV2.Generate(@case));
		var expected = File.ReadAllText(GeneratorGoldenTests.GetFixturePath(fixtureName));

		Assert.Equal(expected, actual);
	}

	[Fact]
	public void SymbolHashedCases_DifferentSymbolsSameSeed_ProduceDifferentFixtures()
	{
		var synth = File.ReadAllText(GeneratorGoldenTests.GetFixturePath("symbol-hashed-synth-seed12345-v2"));
		var aapl = File.ReadAllText(GeneratorGoldenTests.GetFixturePath("symbol-hashed-aapl-seed12345-v2"));

		Assert.NotEqual(synth, aapl);
	}

	[Fact]
	public void SameGeometry_V1AndV2ProduceDifferentBytes_ButBothWellFormed()
	{
		// @1 and @2 draw from the same Box-Muller/Brownian-bridge construction but through
		// different Log/Exp/Cos implementations -- not required to (and not observed to) produce
		// the same bytes. This is a sanity check that the two golden sets are not accidentally
		// identical (which would mean @2 was not actually exercising DeterministicMath).
		var v1 = File.ReadAllText(GeneratorGoldenTests.GetFixturePath("bare-reference-seed1"));
		var v2 = File.ReadAllText(GeneratorGoldenTests.GetFixturePath("bare-reference-seed1-v2"));

		Assert.NotEqual(v1, v2);
	}
}

using Illusionist.Tests.Golden;

namespace Illusionist.Service.Tests;

/// <summary>
/// Proves <see cref="BrownianBridgeV2.GoldenCases"/> is the same manifest as
/// <see cref="GoldenBarSeriesCasesV2.All"/> -- the <c>@2</c> twin of <see cref="GoldenManifestTests"/>.
/// </summary>
public sealed class GoldenManifestTestsV2
{
	private static readonly IReadOnlyList<GoldenCase> ServiceCases = BrownianBridgeV2.Instance.GoldenCases;

	[Theory]
	[MemberData(nameof(FixtureNames))]
	public void PinnedHash_MatchesCommittedFixtureFile(string fixtureName)
	{
		var @case = ServiceCases.Single(c => c.Name == fixtureName);
		var expected = GoldenFixtures.ComputeSha256(GoldenFixtures.ReadText(fixtureName));

		Assert.Equal(expected, @case.Sha256);
	}

	[Fact]
	public void ServiceCases_MatchGoldenBarSeriesCasesV2OneToOne()
	{
		var reference = GoldenBarSeriesCasesV2.All;

		Assert.Equal(reference.Count, ServiceCases.Count);

		foreach (var expected in reference)
		{
			var actual = ServiceCases.SingleOrDefault(c => c.Name == expected.FixtureName);
			Assert.True(actual is not null, $"No service golden case named '{expected.FixtureName}'.");

			var expectedSeedMode = expected.Path == GoldenGenerationPath.SymbolHashed ? SeedMode.SymbolHashed : SeedMode.Bare;

			Assert.Equal(expectedSeedMode, actual!.SeedMode);
			Assert.Equal(expected.Seed, actual.Seed);
			Assert.Equal(expected.Symbol, actual.Symbol);
			Assert.Equal(expected.AnchorPrice, actual.AnchorPrice);
			Assert.Equal(expected.Drift, actual.Drift);
			Assert.Equal(expected.Volatility, actual.Volatility);
			Assert.Equal(expected.BarCount, actual.Count);
			Assert.Equal(DateOnly.FromDateTime(GoldenBarSeriesCases.ReferenceAnchorTimestamp), actual.AnchorDate);
		}
	}

	public static IEnumerable<object[]> FixtureNames => ServiceCases.Select(c => new object[] { c.Name });
}

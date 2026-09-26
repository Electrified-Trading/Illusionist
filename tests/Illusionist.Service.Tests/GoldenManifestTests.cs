using Illusionist.Tests.Golden;

namespace Illusionist.Service.Tests;

/// <summary>
/// Proves <see cref="BrownianBridgeV1.GoldenCases"/> is the same manifest as
/// <see cref="GoldenBarSeriesCases.All"/> -- not just similar, but a one-to-one match on every
/// field that determines the bytes -- and that each case's pinned hash matches the committed
/// fixture file it names.
/// </summary>
public sealed class GoldenManifestTests
{
	private static readonly IReadOnlyList<GoldenCase> ServiceCases = BrownianBridgeV1.Instance.GoldenCases;

	[Theory]
	[MemberData(nameof(FixtureNames))]
	public void PinnedHash_MatchesCommittedFixtureFile(string fixtureName)
	{
		var @case = ServiceCases.Single(c => c.Name == fixtureName);
		var expected = GoldenFixtures.ComputeSha256(GoldenFixtures.ReadText(fixtureName));

		Assert.Equal(expected, @case.Sha256);
	}

	[Fact]
	public void ServiceCases_MatchGoldenBarSeriesCasesOneToOne()
	{
		var reference = GoldenBarSeriesCases.All;

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
			Assert.Equal(new TimeOnly(9, 30), TimeOnly.FromDateTime(GoldenBarSeriesCases.ReferenceAnchorTimestamp));
		}
	}

	public static IEnumerable<object[]> FixtureNames => ServiceCases.Select(c => new object[] { c.Name });
}

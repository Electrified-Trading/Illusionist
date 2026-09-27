using NSubstitute;

namespace Illusionist.Service.Tests;

/// <summary>
/// <see cref="SeriesEnvelope.PlatformScopeWarning"/>: a request served by a version whose declared
/// <see cref="IGeneratorVersion.ReferencePlatform"/> is not this host's own carries a non-breaking
/// warning saying so (the same channel as <c>calendar</c>/<c>extreme_prices</c>, never in the
/// rendered bytes). This host is brownian-bridge@1's own reference platform and brownian-bridge@2
/// has no reference platform at all, so neither ever triggers this warning for real here -- a
/// substituted version with a reference platform that can never match any real host is the only way
/// to exercise this path at all in this environment (see also <see cref="SelfCheckTests.ReferencePlatformMismatch_ReportedHonestly_ButDoesNotBlockReadiness"/>
/// for the companion readiness-side behavior).
/// </summary>
public sealed class PlatformWarningTests
{
	private static readonly BrownianBridgeV1 ReferenceGenerator = BrownianBridgeV1.Instance;

	/// <summary>
	/// <see cref="GeneratorRegistry"/> refuses a version with no golden case at all (D7, forever rule
	/// 4) -- unrelated to what this file tests, but required to construct a fake version at all.
	/// </summary>
	private static readonly GoldenCase DummyGoldenCase = ReferenceGenerator.GoldenCases[0];

	[Fact]
	public void ReferencePlatformMismatch_WarnsWithBothPlatforms()
	{
		var version = Substitute.For<IGeneratorVersion>();
		version.Ref.Returns(new GeneratorRef("pinned-elsewhere", 1));
		version.GoldenCases.Returns([DummyGoldenCase]);
		version.ReferencePlatform.Returns("never-matches-any-real-host");
		version.Open(Arg.Any<SeriesKey>()).Returns(callInfo => ReferenceGenerator.Open(callInfo.Arg<SeriesKey>()));

		var registry = new GeneratorRegistry([version]);
		var seriesService = new SeriesService(registry);

		var key = new SeriesKey(
			version.Ref, SeedMode.Bare, Seed: 1, Symbol: null, Drift: 0.0001, Volatility: 0.01,
			BrownianBridgeV1.GoldenAnchorDate, AnchorPrice: 100m, Timeframe: "1d",
			new SeriesExtent.Count(5), SeriesFormat.Csv);

		var outcome = seriesService.GenerateFromKey(key);

		var result = Assert.IsType<SeriesOutcome.Success>(outcome).Result;
		Assert.Contains(result.Warnings, w => w.StartsWith("platform:", StringComparison.Ordinal));
		Assert.Contains("never-matches-any-real-host", result.Warnings.Single(w => w.StartsWith("platform:", StringComparison.Ordinal)));
	}

	[Fact]
	public void NoReferencePlatform_NeverWarns()
	{
		var version = Substitute.For<IGeneratorVersion>();
		version.Ref.Returns(new GeneratorRef("fully-portable", 1));
		version.GoldenCases.Returns([DummyGoldenCase]);
		version.ReferencePlatform.Returns((string?)null);
		version.Open(Arg.Any<SeriesKey>()).Returns(callInfo => ReferenceGenerator.Open(callInfo.Arg<SeriesKey>()));

		var registry = new GeneratorRegistry([version]);
		var seriesService = new SeriesService(registry);

		var key = new SeriesKey(
			version.Ref, SeedMode.Bare, Seed: 1, Symbol: null, Drift: 0.0001, Volatility: 0.01,
			BrownianBridgeV1.GoldenAnchorDate, AnchorPrice: 100m, Timeframe: "1d",
			new SeriesExtent.Count(5), SeriesFormat.Csv);

		var outcome = seriesService.GenerateFromKey(key);

		var result = Assert.IsType<SeriesOutcome.Success>(outcome).Result;
		Assert.DoesNotContain(result.Warnings, w => w.StartsWith("platform:", StringComparison.Ordinal));
	}

	[Fact]
	public void PlatformWarning_NeverChangesRenderedBytes()
	{
		var pinnedElsewhere = Substitute.For<IGeneratorVersion>();
		pinnedElsewhere.Ref.Returns(new GeneratorRef("pinned-elsewhere", 1));
		pinnedElsewhere.GoldenCases.Returns([DummyGoldenCase]);
		pinnedElsewhere.ReferencePlatform.Returns("never-matches-any-real-host");
		pinnedElsewhere.Open(Arg.Any<SeriesKey>()).Returns(callInfo => ReferenceGenerator.Open(callInfo.Arg<SeriesKey>()));

		var portable = Substitute.For<IGeneratorVersion>();
		portable.Ref.Returns(new GeneratorRef("fully-portable", 1));
		portable.GoldenCases.Returns([DummyGoldenCase]);
		portable.ReferencePlatform.Returns((string?)null);
		portable.Open(Arg.Any<SeriesKey>()).Returns(callInfo => ReferenceGenerator.Open(callInfo.Arg<SeriesKey>()));

		SeriesKey KeyFor(GeneratorRef generatorRef) => new(
			generatorRef, SeedMode.Bare, Seed: 1, Symbol: null, Drift: 0.0001, Volatility: 0.01,
			BrownianBridgeV1.GoldenAnchorDate, AnchorPrice: 100m, Timeframe: "1d",
			new SeriesExtent.Count(5), SeriesFormat.Csv);

		var registry = new GeneratorRegistry([pinnedElsewhere, portable]);
		var seriesService = new SeriesService(registry);

		var pinnedResult = Assert.IsType<SeriesOutcome.Success>(seriesService.GenerateFromKey(KeyFor(pinnedElsewhere.Ref))).Result;
		var portableResult = Assert.IsType<SeriesOutcome.Success>(seriesService.GenerateFromKey(KeyFor(portable.Ref))).Result;

		Assert.Equal(portableResult.Body, pinnedResult.Body);
	}
}

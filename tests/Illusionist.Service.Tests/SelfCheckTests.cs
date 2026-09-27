using System.Net;
using Illusionist.Service.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using NSubstitute;

namespace Illusionist.Service.Tests;

/// <summary>D8's readiness gate: a host either proves it reproduces the golden fixtures, or every series-bearing surface refuses.</summary>
public sealed class SelfCheckTests(IllusionistWebApplicationFactory factory) : IClassFixture<IllusionistWebApplicationFactory>
{
	[Fact]
	public void ThisHost_PassesAllReadinessCases()
	{
		var registry = new GeneratorRegistry(GeneratorCatalog.All);
		var seriesService = new SeriesService(registry);
		var selfCheck = new GoldenSelfCheck(registry, seriesService);

		var report = selfCheck.Run();

		Assert.True(report.IsReady);
		// Two registered versions, 10 cases each (6 fixture-backed golden cases + 4 readiness-only
		// cases: range mode, json, non-default drift, a different anchor date) -- see
		// BrownianBridgeV1.ReadinessCases and BrownianBridgeV2.ReadinessCases. On this host (its own
		// reference platform), @1's cases match for real, not just by being waived.
		Assert.Equal(20, report.Cases.Count);
		Assert.All(report.Cases, c => Assert.True(c.Match));
	}

	[Fact]
	public void FlippedHashCharacter_GivesFailReport()
	{
		var real = BrownianBridgeV1.Instance;
		var flippedCase = real.GoldenCases[0] with { Sha256 = "0" + real.GoldenCases[0].Sha256[1..] };

		var fakeVersion = Substitute.For<IGeneratorVersion>();
		fakeVersion.Ref.Returns(real.Ref);
		fakeVersion.GoldenCases.Returns([flippedCase]);
		// GoldenSelfCheck iterates ReadinessCases, not GoldenCases -- NSubstitute does not fall
		// through to an interface default-implementation for an unstubbed member, so this must be
		// stubbed explicitly even though it happens to equal GoldenCases here.
		fakeVersion.ReadinessCases.Returns([flippedCase]);
		fakeVersion.Open(Arg.Any<SeriesKey>()).Returns(callInfo => real.Open(callInfo.Arg<SeriesKey>()));

		var registry = new GeneratorRegistry([fakeVersion]);
		var selfCheck = new GoldenSelfCheck(registry, new SeriesService(registry));

		var report = selfCheck.Run();

		Assert.False(report.IsReady);
		Assert.False(report.Cases.Single().Match);
		Assert.NotEqual(report.Cases.Single().Expected, report.Cases.Single().Actual);
	}

	/// <summary>
	/// The scenario this host cannot otherwise exercise (it *is* brownian-bridge@1's own reference
	/// platform, so @1 always matches here, and @2 has no reference platform to ever fail against):
	/// a version whose declared <see cref="IGeneratorVersion.ReferencePlatform"/> never matches this
	/// host reports its real (mis)match honestly but does not block another, fully-portable version's
	/// readiness -- the D8/reference-platform design this task's finding required.
	/// </summary>
	[Fact]
	public void ReferencePlatformMismatch_ReportedHonestly_ButDoesNotBlockReadiness()
	{
		var reference = BrownianBridgeV1.Instance;
		var mismatchedCase = reference.GoldenCases[0] with { Sha256 = "0" + reference.GoldenCases[0].Sha256[1..] };

		var pinnedElsewhere = Substitute.For<IGeneratorVersion>();
		pinnedElsewhere.Ref.Returns(reference.Ref);
		pinnedElsewhere.GoldenCases.Returns([mismatchedCase]);
		pinnedElsewhere.ReadinessCases.Returns([mismatchedCase]);
		pinnedElsewhere.ReferencePlatform.Returns("never-matches-any-real-host");
		pinnedElsewhere.Open(Arg.Any<SeriesKey>()).Returns(callInfo => reference.Open(callInfo.Arg<SeriesKey>()));

		var portable = Substitute.For<IGeneratorVersion>();
		portable.Ref.Returns(new GeneratorRef("portable-generator", 1));
		portable.GoldenCases.Returns([reference.GoldenCases[0]]);
		portable.ReadinessCases.Returns([reference.GoldenCases[0]]);
		portable.ReferencePlatform.Returns((string?)null);
		portable.Open(Arg.Any<SeriesKey>()).Returns(callInfo => reference.Open(callInfo.Arg<SeriesKey>()));

		var registry = new GeneratorRegistry([pinnedElsewhere, portable]);
		var selfCheck = new GoldenSelfCheck(registry, new SeriesService(registry));

		var report = selfCheck.Run();

		Assert.True(report.IsReady);

		var pinnedResult = report.Cases.Single(c => c.Ref == reference.Ref.ToString());
		Assert.False(pinnedResult.Match);
		Assert.False(pinnedResult.CountsTowardReadiness);
		Assert.Equal("never-matches-any-real-host", pinnedResult.ReferencePlatform);

		var portableResult = report.Cases.Single(c => c.Ref == "portable-generator@1");
		Assert.True(portableResult.Match);
		Assert.True(portableResult.CountsTowardReadiness);
		Assert.Null(portableResult.ReferencePlatform);
	}

	[Fact]
	public async Task WhileNotReady_HealthzIs503_SeriesSurfacesRefuse_GeneratorsStillAnswers()
	{
		var failingReport = new SelfCheckReport(
			new HostInfo("test-os", "test-arch", "test-framework", "test-platform"),
			[new SelfCheckCaseResult("brownian-bridge@1", "fake-case", "expected", "actual", Match: false, ReferencePlatform: null, CountsTowardReadiness: true)]);

		var fakeSelfCheck = Substitute.For<IGoldenSelfCheck>();
		fakeSelfCheck.Run().Returns(failingReport);

		await using var notReadyFactory = factory.WithWebHostBuilder(builder =>
			builder.ConfigureServices(services => services.AddSingleton(fakeSelfCheck)));

		var httpClient = notReadyFactory.CreateClient();

		var health = await httpClient.GetAsync("/healthz");
		Assert.Equal(HttpStatusCode.ServiceUnavailable, health.StatusCode);

		var series = await httpClient.GetAsync("/v1/series?generator=brownian-bridge@1&seed=1");
		Assert.Equal(HttpStatusCode.ServiceUnavailable, series.StatusCode);
		Assert.Contains("\"code\":\"not_ready\"", await series.Content.ReadAsStringAsync());

		var generators = await httpClient.GetAsync("/v1/generators");
		Assert.Equal(HttpStatusCode.OK, generators.StatusCode);

		await using var client = await McpTestClient.CreateAsync(notReadyFactory);

		var toolResult = await client.CallToolAsync("illusionist_series", new Dictionary<string, object?>
		{
			["generator"] = "brownian-bridge@1",
			["seed"] = 1,
		});
		Assert.True(toolResult.IsError);

		var resourceException = await Assert.ThrowsAnyAsync<McpException>(
			() => client.ReadResourceAsync("illusionist://series?generator=brownian-bridge@1&seed=1").AsTask());
		Assert.Contains("not_ready:", resourceException.Message);

		var generatorsTool = await client.CallToolAsync("illusionist_generators");
		Assert.False(generatorsTool.IsError == true);
	}

	[Fact]
	public void SelfCheckCommand_Run_ExitsZero_WhenPassing()
	{
		var registry = new GeneratorRegistry(GeneratorCatalog.All);
		var command = new SelfCheckCommand(new GoldenSelfCheck(registry, new SeriesService(registry)));

		using var writer = new StringWriter();
		var exitCode = command.Run(writer);

		Assert.Equal(0, exitCode);
		Assert.Contains("\"status\":\"ready\"", writer.ToString());
	}

	[Fact]
	public void SelfCheckCommand_Run_ExitsOne_WhenFailing()
	{
		var failing = Substitute.For<IGoldenSelfCheck>();
		failing.Run().Returns(new SelfCheckReport(
			new HostInfo("os", "arch", "framework", "platform"),
			[new SelfCheckCaseResult("brownian-bridge@1", "case", "expected", "actual", Match: false, ReferencePlatform: null, CountsTowardReadiness: true)]));

		var command = new SelfCheckCommand(failing);
		using var writer = new StringWriter();
		var exitCode = command.Run(writer);

		Assert.Equal(1, exitCode);
		Assert.Contains("\"status\":\"not-ready\"", writer.ToString());
	}
}

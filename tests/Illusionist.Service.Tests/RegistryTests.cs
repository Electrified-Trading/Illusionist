using NSubstitute;

namespace Illusionist.Service.Tests;

/// <summary>
/// D7's "forever" rules: every published version resolves by its exact ref, describes itself, and
/// passes its own self-check. <see cref="PublishedRefs"/> is the append-only list this repository
/// has ever shipped -- adding a version here is a permanent commitment that it stays callable.
/// </summary>
public sealed class RegistryTests
{
	/// <summary>Append-only: every ref this service has ever published. Never remove an entry.</summary>
	public static readonly IReadOnlyList<string> PublishedRefs = ["brownian-bridge@1", "brownian-bridge@2"];

	private static readonly IGeneratorRegistry Registry = new GeneratorRegistry(GeneratorCatalog.All);
	private static readonly ISeriesService SeriesService = new SeriesService(Registry);
	private static readonly IGoldenSelfCheck SelfCheck = new GoldenSelfCheck(Registry, SeriesService);

	[Theory]
	[MemberData(nameof(PublishedRefsData))]
	public void PublishedRef_Resolves(string reference)
	{
		Assert.True(GeneratorRef.TryParse(reference, out var parsed));
		Assert.True(Registry.HasId(parsed.Id));
		Assert.True(Registry.TryGet(parsed, out var version));
		Assert.NotNull(version);
	}

	[Theory]
	[MemberData(nameof(PublishedRefsData))]
	public void PublishedRef_Describes(string reference)
	{
		GeneratorRef.TryParse(reference, out var parsed);
		Registry.TryGet(parsed, out var version);

		var json = GeneratorListingRenderer.RenderDescribe(version!, selfCheckReport: null);

		Assert.Contains($"\"ref\":\"{reference}\"", json);
	}

	[Fact]
	public void EveryPublishedRef_PassesSelfCheck()
	{
		var report = SelfCheck.Run();

		foreach (var reference in PublishedRefs)
			Assert.Contains(report.Cases, c => c.Ref == reference && c.Match);
	}

	[Fact]
	public void EveryRegisteredVersion_HasAtLeastOneGoldenCase()
	{
		Assert.All(Registry.All, version => Assert.NotEmpty(version.GoldenCases));
	}

	[Fact]
	public void Registry_ConstructorThrows_WhenAVersionHasNoGoldenCase()
	{
		var barren = Substitute.For<IGeneratorVersion>();
		barren.Ref.Returns(new GeneratorRef("barren", 1));
		barren.GoldenCases.Returns(Array.Empty<GoldenCase>());

		Assert.Throws<InvalidOperationException>(() => new GeneratorRegistry([barren]));
	}

	public static IEnumerable<object[]> PublishedRefsData => PublishedRefs.Select(r => new object[] { r });
}

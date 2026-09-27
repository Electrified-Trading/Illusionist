using System.Runtime.InteropServices;
using Xunit.Abstractions;

namespace Illusionist.Tests.Golden;

/// <summary>
/// States the honest scope of "byte-identical" by reporting the host's own identity beside a
/// golden result, rather than asserting anything about a host this suite cannot run on.
/// </summary>
/// <remarks>
/// <para>
/// <b>The scope, stated plainly:</b> <c>BrownianBridgeBarSeries</c>'s generator builds every bar
/// from double-precision transcendentals (<c>Math.Exp</c>, <c>Math.Log</c>, <c>Math.Cos</c>,
/// <c>Math.Sqrt</c> -- see its own <c>Generator.cs</c>). IEEE-754 guarantees those operations'
/// *inputs and outputs*, not that two different runtimes compute the same intermediate rounding
/// on the way there. <see cref="GeneratorGoldenTests"/>'s comparisons are therefore only known to
/// hold **within one .NET major version family, on x64** -- the family and architecture every
/// fixture in this repo was generated and is currently verified on. Across CPU architectures
/// (ARM64 in particular, since the owner's own stated motive for a shared service is "if I change
/// computers") or across .NET major versions, byte-identity is <b>UNVERIFIED</b> until a golden
/// run actually passes there.
/// </para>
/// <para>
/// This test does not and cannot fail that question for a host it isn't running on. What it
/// does: print this process's own <see cref="RuntimeInformation"/> next to a real golden
/// comparison's pass/fail, in the test output, so that running this exact test on a new host
/// (an ARM64 machine, a future .NET major) is the actual proof procedure -- read the two lines
/// together, not the assertion alone.
/// </para>
/// </remarks>
public class ReproducibilityScopeTests(ITestOutputHelper output)
{
	[Fact]
	public void GoldenFixture_ReportsRuntimeIdentityBesideItsOwnResult()
	{
		var @case = GoldenBarSeriesCases.All[0];
		var actual = GoldenBarFormatter.Format(GoldenBarSeriesGenerator.Generate(@case));
		var expected = File.ReadAllText(GeneratorGoldenTests.GetFixturePath(@case.FixtureName));
		var matches = actual == expected;

		output.WriteLine($"OSArchitecture: {RuntimeInformation.OSArchitecture}");
		output.WriteLine($"ProcessArchitecture: {RuntimeInformation.ProcessArchitecture}");
		output.WriteLine($"FrameworkDescription: {RuntimeInformation.FrameworkDescription}");
		output.WriteLine($"OSDescription: {RuntimeInformation.OSDescription}");
		output.WriteLine($"Fixture: {@case.FixtureName}");
		output.WriteLine($"Result: {(matches ? "MATCH" : "MISMATCH")}");

		Assert.True(
			matches,
			$"'{@case.FixtureName}' did not match on {RuntimeInformation.OSArchitecture}/" +
			$"{RuntimeInformation.FrameworkDescription} -- see this test's own remarks for what " +
			"that does and does not mean about the reproducibility scope.");
	}
}

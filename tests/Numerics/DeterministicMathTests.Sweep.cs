using Illusionist.Core.Numerics;
using Xunit.Abstractions;

namespace Illusionist.Tests.Numerics;

/// <summary>
/// Sweeps <see cref="DeterministicMath.Log"/>, <see cref="DeterministicMath.Exp"/> and
/// <see cref="DeterministicMath.Cos"/> against <see cref="Math.Log(double)"/>,
/// <see cref="Math.Exp(double)"/> and <see cref="Math.Cos(double)"/> across a wide range of inputs.
/// This machine's own <c>Math.*</c> (UCRT) is not a certified reference -- it is simply the only
/// other independent implementation available here -- so a real discrepancy this sweep finds is
/// worth investigating either way. See <see cref="DeterministicMathTests"/> (the exact-bits table)
/// for the test that actually pins this port's own output for cross-platform regression.
/// </summary>
public sealed partial class DeterministicMathTests(ITestOutputHelper output)
{
	/// <summary>
	/// The largest ULP distance from <see cref="Math.Log(double)"/>/<see cref="Math.Exp(double)"/>/
	/// <see cref="Math.Cos(double)"/> this sweep has been measured to reach on this host -- a
	/// from-scratch series-based port is not expected to land on the exact same last-bit rounding as
	/// a minimax-fitted platform library on every input, so this is a quality bound, not a
	/// determinism claim (determinism comes from using only IEEE-exact primitives, proven instead by
	/// the exact-bits table).
	/// </summary>
	private const int MaxAllowedUlp = 2;

	[Fact]
	public void Log_AgreesWithMathLog_AcrossWideSweep()
	{
		AssertAgreement("Log", Enumerable.Range(0, 200_000).Select(i => Math.Pow(2.0, -40.0 + i * 80.0 / 200_000)), DeterministicMath.Log, Math.Log);
	}

	[Fact]
	public void Exp_AgreesWithMathExp_AcrossWideSweep()
	{
		AssertAgreement("Exp", Enumerable.Range(0, 200_000).Select(i => -700.0 + i * 1400.0 / 200_000), DeterministicMath.Exp, Math.Exp);
	}

	[Fact]
	public void Cos_AgreesWithMathCos_AcrossSmallAndMediumRange()
	{
		// Covers the fast (|x| <= pi/4) and medium (pi/4 < |x| < 65536) reduction paths -- see
		// DeterministicMath.Cos.cs.
		AssertAgreement("Cos (small/medium)", Enumerable.Range(0, 200_000).Select(i => -70000.0 + i * 140000.0 / 200_000), DeterministicMath.Cos, Math.Cos);
	}

	[Fact]
	public void Cos_AgreesWithMathCos_AcrossExactReductionRange()
	{
		// Beyond 65536: the BigInteger Payne-Hanek-style path (DeterministicMath.ReduceExact).
		var inputs = Enumerable.Range(0, 2_000)
			.Select(i => Math.Pow(10.0, 5.0 + i * 300.0 / 2_000))
			.Where(double.IsFinite);

		AssertAgreement("Cos (exact reduction)", inputs, DeterministicMath.Cos, Math.Cos);
	}

	private void AssertAgreement(string label, IEnumerable<double> inputs, Func<double, double> actual, Func<double, double> reference)
	{
		long maxUlp = 0;
		var maxUlpInput = double.NaN;
		var count = 0;

		foreach (var x in inputs)
		{
			count++;
			var a = actual(x);
			var r = reference(x);
			var ulp = UlpComparison.Distance(a, r);
			if (ulp <= maxUlp)
				continue;

			maxUlp = ulp;
			maxUlpInput = x;
		}

		output.WriteLine($"{label}: {count} inputs, max ULP distance {maxUlp} at x={maxUlpInput:R}");
		Assert.True(maxUlp <= MaxAllowedUlp, $"{label}: max ULP distance {maxUlp} at x={maxUlpInput:R} exceeds {MaxAllowedUlp}.");
	}
}

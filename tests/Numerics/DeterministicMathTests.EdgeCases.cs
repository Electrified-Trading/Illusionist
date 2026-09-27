using Illusionist.Core.Numerics;

namespace Illusionist.Tests.Numerics;

/// <summary>The boundary inputs a from-scratch <c>log</c>/<c>exp</c>/<c>cos</c> most commonly gets wrong: zero, subnormals, exact powers of two, NaN/infinity, and (for <see cref="DeterministicMath.Cos"/>) angles far past its small-angle fast path.</summary>
public sealed partial class DeterministicMathTests
{
	[Fact]
	public void Log_OfZero_IsNegativeInfinity()
		=> Assert.Equal(double.NegativeInfinity, DeterministicMath.Log(0.0));

	[Fact]
	public void Log_OfNegativeZero_IsNegativeInfinity()
		=> Assert.Equal(double.NegativeInfinity, DeterministicMath.Log(-0.0));

	[Fact]
	public void Log_OfOne_IsExactlyZero()
		=> Assert.Equal(0.0, DeterministicMath.Log(1.0));

	[Fact]
	public void Log_OfNegativeNumber_IsNaN()
		=> Assert.True(double.IsNaN(DeterministicMath.Log(-1.0)));

	[Fact]
	public void Log_OfNaN_IsNaN()
		=> Assert.True(double.IsNaN(DeterministicMath.Log(double.NaN)));

	[Fact]
	public void Log_OfPositiveInfinity_IsPositiveInfinity()
		=> Assert.Equal(double.PositiveInfinity, DeterministicMath.Log(double.PositiveInfinity));

	[Fact]
	public void Log_OfSmallestSubnormal_MatchesMathLogWithinOneUlp()
	{
		var actual = DeterministicMath.Log(double.Epsilon);
		var reference = Math.Log(double.Epsilon);

		Assert.True(UlpComparison.Distance(actual, reference) <= MaxAllowedUlp);
	}

	[Fact]
	public void Log_OfLargestSubnormal_MatchesMathLogWithinOneUlp()
	{
		var largestSubnormal = BitConverter.Int64BitsToDouble(0x000F_FFFF_FFFF_FFFF);
		Assert.True(double.IsSubnormal(largestSubnormal)); // Sanity: still subnormal (double.Epsilon is the *smallest* subnormal, not a bound on this one).

		var actual = DeterministicMath.Log(largestSubnormal);
		var reference = Math.Log(largestSubnormal);

		Assert.True(UlpComparison.Distance(actual, reference) <= MaxAllowedUlp);
	}

	[Fact]
	public void Log_OfHugeValue_MatchesMathLogWithinOneUlp()
	{
		var actual = DeterministicMath.Log(double.MaxValue);
		var reference = Math.Log(double.MaxValue);

		Assert.True(UlpComparison.Distance(actual, reference) <= MaxAllowedUlp);
	}

	[Fact]
	public void Exp_OfZero_IsExactlyOne()
		=> Assert.Equal(1.0, DeterministicMath.Exp(0.0));

	[Fact]
	public void Exp_OfNaN_IsNaN()
		=> Assert.True(double.IsNaN(DeterministicMath.Exp(double.NaN)));

	[Fact]
	public void Exp_OfPositiveInfinity_IsPositiveInfinity()
		=> Assert.Equal(double.PositiveInfinity, DeterministicMath.Exp(double.PositiveInfinity));

	[Fact]
	public void Exp_OfNegativeInfinity_IsZero()
		=> Assert.Equal(0.0, DeterministicMath.Exp(double.NegativeInfinity));

	[Fact]
	public void Exp_FarEnoughPositive_OverflowsToPositiveInfinity()
		=> Assert.Equal(double.PositiveInfinity, DeterministicMath.Exp(1000.0));

	[Fact]
	public void Exp_FarEnoughNegative_UnderflowsToZero()
		=> Assert.Equal(0.0, DeterministicMath.Exp(-1000.0));

	[Fact]
	public void Exp_NearUnderflowBoundary_MatchesMathExpWithinOneUlp()
	{
		// Math.Exp(x) for x this negative produces a subnormal result -- exercises ScalePow2's
		// gradual-underflow rounding, not just its normal-range path.
		foreach (var x in new[] { -735.0, -740.0, -744.0, -744.4, -745.0, -745.13 })
		{
			var actual = DeterministicMath.Exp(x);
			var reference = Math.Exp(x);

			Assert.True(UlpComparison.Distance(actual, reference) <= MaxAllowedUlp, $"x={x}: actual={actual:R}, reference={reference:R}");
		}
	}

	[Fact]
	public void Cos_OfZero_IsExactlyOne()
		=> Assert.Equal(1.0, DeterministicMath.Cos(0.0));

	[Fact]
	public void Cos_OfNegativeZero_IsExactlyOne()
		=> Assert.Equal(1.0, DeterministicMath.Cos(-0.0));

	[Fact]
	public void Cos_OfNaN_IsNaN()
		=> Assert.True(double.IsNaN(DeterministicMath.Cos(double.NaN)));

	[Fact]
	public void Cos_OfPositiveInfinity_IsNaN()
		=> Assert.True(double.IsNaN(DeterministicMath.Cos(double.PositiveInfinity)));

	[Fact]
	public void Cos_OfNegativeInfinity_IsNaN()
		=> Assert.True(double.IsNaN(DeterministicMath.Cos(double.NegativeInfinity)));

	[Fact]
	public void Cos_IsEven()
	{
		foreach (var x in new[] { 0.3, 1.0, 12345.6789, 1.0e10, 1.0e100 })
			Assert.Equal(DeterministicMath.Cos(x), DeterministicMath.Cos(-x));
	}

	[Theory]
	[InlineData(1.0e6)]
	[InlineData(1.0e10)]
	[InlineData(1.0e15)]
	[InlineData(1.0e50)]
	[InlineData(1.0e100)]
	[InlineData(1.0e300)]
	public void Cos_LargeAngleReduction_MatchesMathCosWithinOneUlp(double x)
	{
		var actual = DeterministicMath.Cos(x);
		var reference = Math.Cos(x);

		Assert.True(UlpComparison.Distance(actual, reference) <= MaxAllowedUlp, $"x={x:R}: actual={actual:R}, reference={reference:R}");
	}

	[Fact]
	public void Cos_OfDoubleMaxValue_IsFiniteAndMatchesMathCos()
	{
		// The largest finite double: exercises ReduceExact at the maximum representable exponent.
		var actual = DeterministicMath.Cos(double.MaxValue);
		var reference = Math.Cos(double.MaxValue);

		Assert.True(double.IsFinite(actual));
		Assert.True(UlpComparison.Distance(actual, reference) <= MaxAllowedUlp, $"actual={actual:R}, reference={reference:R}");
	}

	[Fact]
	public void Cos_AtExactMultiplesOfPiOver2_StaysBounded()
	{
		// Near-exact multiples of pi/2 are cos's own worst-conditioned inputs (the true derivative
		// is +-1, so a tiny reduction error shows up almost linearly) -- not a byte-identity claim
		// against Math.Cos (whose own reduction differs from this port's), only a sanity bound.
		foreach (var n in new[] { 1, 2, 3, 4, 100, 1_000_000 })
		{
			var x = n * Math.PI / 2.0;
			var actual = DeterministicMath.Cos(x);

			Assert.True(actual is >= -1.0 and <= 1.0, $"n={n}: cos returned {actual:R}, outside [-1, 1].");
		}
	}
}

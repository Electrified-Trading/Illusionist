namespace Illusionist.Core.Numerics;

public static partial class DeterministicMath
{
	/// <summary>Taylor terms kept in <see cref="EvaluateExpSeries"/> beyond the constant term -- see its own remarks for the convergence bound this satisfies.</summary>
	private const int ExpSeriesTerms = 17;

	private static readonly double[] InverseFactorials = BuildInverseFactorials(ExpSeriesTerms);

	/// <summary>
	/// A deterministic <c>e^x</c>: within about 1 ulp of <see cref="Math.Exp(double)"/> on every
	/// platform, because every step below is <c>+</c>, <c>-</c>, <c>*</c>, <c>/</c> or exact bit
	/// manipulation -- never a call into the platform's own <c>exp</c>. Matches
	/// <see cref="Math.Exp(double)"/>'s handling of its domain: <see cref="double.NaN"/> for NaN,
	/// positive infinity for positive infinity, <c>0.0</c> for negative infinity, and correct
	/// overflow to positive infinity / gradual underflow to a subnormal or zero result for finite
	/// <paramref name="x"/> far enough from zero.
	/// </summary>
	public static double Exp(double x)
	{
		if (double.IsNaN(x) || double.IsPositiveInfinity(x))
			return x;

		if (double.IsNegativeInfinity(x))
			return 0.0;

		if (x == 0.0)
			return 1.0;

		// Reduce to x = k*ln2 + r with |r| <= ln2/2, using ln2's Cody-Waite split so the subtraction
		// does not lose precision for large |k| (fdlibm's own e_exp.c reduction).
		var kd = Math.Round(x / Ln2CodyWaite.Hi, MidpointRounding.ToEven);
		var r = x - kd * Ln2CodyWaite.Hi;
		r -= kd * Ln2CodyWaite.Mid;
		r -= kd * Ln2CodyWaite.Tail;

		var expR = EvaluateExpSeries(r);

		// x is finite here, so k fits comfortably in an int (|x/ln2| < ~1.6e308/0.69, but exp
		// overflows to infinity long before that -- ScalePow2 below handles both directions exactly).
		var k = (int)kd;
		return ScalePow2(expR, k);
	}

	/// <summary>
	/// exp(r) = sum r^n / n! for n = 0..<see cref="ExpSeriesTerms"/>, each <c>1/n!</c> an exact IEEE
	/// division of two exact integers (every factorial up to 17! is exactly representable in a
	/// <see cref="double"/>'s 53-bit mantissa) -- a plain Taylor series, not <c>fdlibm</c>'s
	/// minimax-fitted <c>P1..P5</c>. <see cref="Exp"/>'s reduction keeps <c>|r| &lt;= ln2/2 ~= 0.3466</c>,
	/// so the 17th term here is already below 5e-18 -- comfortably past a <see cref="double"/>'s
	/// ~1.1e-16 relative precision.
	/// </summary>
	private static double EvaluateExpSeries(double r)
	{
		var acc = InverseFactorials[ExpSeriesTerms];
		for (var n = ExpSeriesTerms - 1; n >= 0; n--)
			acc = acc * r + InverseFactorials[n];

		return acc;
	}

	private static double[] BuildInverseFactorials(int maxN)
	{
		var result = new double[maxN + 1];
		var factorial = 1.0;
		result[0] = 1.0;
		for (var n = 1; n <= maxN; n++)
		{
			factorial *= n;
			result[n] = 1.0 / factorial;
		}

		return result;
	}
}

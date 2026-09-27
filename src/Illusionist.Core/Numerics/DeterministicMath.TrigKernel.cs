namespace Illusionist.Core.Numerics;

public static partial class DeterministicMath
{
	/// <summary>Taylor terms (beyond the constant/linear term) kept in <see cref="EvaluateCosSeries"/> and <see cref="EvaluateSinSeries"/> -- see their own remarks for the convergence bound this satisfies.</summary>
	private const int TrigSeriesTerms = 10;

	private static readonly double[] InverseCosFactorials = BuildAlternatingInverseFactorials(TrigSeriesTerms, stride: 2, offset: 0);
	private static readonly double[] InverseSinFactorials = BuildAlternatingInverseFactorials(TrigSeriesTerms, stride: 2, offset: 1);

	/// <summary>
	/// cos(x + y) for <c>|x| &lt;= pi/4</c> and a tiny reduction tail <paramref name="y"/>
	/// (typically ~1e-20): cos(x) to full precision via <see cref="EvaluateCosSeries"/>, plus the
	/// first-order correction <c>-y*sin(x)</c>, approximating <c>sin(x) ~= x</c> for that correction
	/// only -- valid because the correction itself is already several orders of magnitude below a
	/// double's precision floor, so a ~10% relative error confined to it is immaterial.
	/// </summary>
	private static double KernelCos(double x, double y)
	{
		var z = x * x;
		var cosX = EvaluateCosSeries(z);
		return cosX - y * x;
	}

	/// <summary>sin(x + y) for <c>|x| &lt;= pi/4</c> and a tiny reduction tail <paramref name="y"/>: sin(x) via <see cref="EvaluateSinSeries"/>, plus the first-order correction <c>y*cos(x)</c>, approximating <c>cos(x) ~= 1</c> for that correction only (see <see cref="KernelCos"/> for why this is safe).</summary>
	private static double KernelSin(double x, double y)
	{
		var z = x * x;
		var sinX = x * EvaluateSinSeries(z);
		return sinX + y;
	}

	/// <summary>
	/// cos(x) = 1 - z/2! + z^2/4! - z^3/6! + ... (z = x^2), every coefficient <c>(-1)^k/(2k)!</c> an
	/// exact IEEE division of two exact integers -- a plain Taylor series, not <c>fdlibm</c>'s
	/// minimax-fitted <c>C1..C6</c>. For <c>|x| &lt;= pi/4</c>, <c>z &lt;= ~0.6169</c>, and the
	/// <see cref="TrigSeriesTerms"/>-th term here is already below 2e-18 -- comfortably past a
	/// <see cref="double"/>'s ~1.1e-16 relative precision.
	/// </summary>
	private static double EvaluateCosSeries(double z)
	{
		var acc = InverseCosFactorials[TrigSeriesTerms];
		for (var k = TrigSeriesTerms - 1; k >= 0; k--)
			acc = acc * z + InverseCosFactorials[k];

		return acc;
	}

	/// <summary>sin(x)/x = 1 - z/3! + z^2/5! - z^3/7! + ... (z = x^2) -- the odd-power twin of <see cref="EvaluateCosSeries"/>, with the same convergence bound.</summary>
	private static double EvaluateSinSeries(double z)
	{
		var acc = InverseSinFactorials[TrigSeriesTerms];
		for (var k = TrigSeriesTerms - 1; k >= 0; k--)
			acc = acc * z + InverseSinFactorials[k];

		return acc;
	}

	/// <summary>Builds <c>[(-1)^k / (stride*k + offset)!]</c> for <c>k = 0..maxK</c> -- <see cref="InverseCosFactorials"/> (offset 0: 0!, 2!, 4!, ...) and <see cref="InverseSinFactorials"/> (offset 1: 1!, 3!, 5!, ...) share this builder.</summary>
	private static double[] BuildAlternatingInverseFactorials(int maxK, int stride, int offset)
	{
		var result = new double[maxK + 1];
		for (var k = 0; k <= maxK; k++)
		{
			var n = stride * k + offset;
			var factorial = 1.0;
			for (var i = 2; i <= n; i++)
				factorial *= i;

			result[k] = (k % 2 == 0 ? 1.0 : -1.0) / factorial;
		}

		return result;
	}
}

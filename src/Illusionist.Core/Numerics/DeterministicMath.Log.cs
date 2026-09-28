namespace Illusionist.Core.Numerics;

public static partial class DeterministicMath
{
	/// <summary>Odd-series terms kept in <see cref="EvaluateLogSeries"/> -- see its own remarks for the convergence bound this satisfies.</summary>
	private const int LogSeriesTerms = 12;

	/// <summary>
	/// A deterministic natural logarithm: within about 1 ulp of <see cref="Math.Log(double)"/> on
	/// every platform, because every step below is <c>+</c>, <c>-</c>, <c>*</c>, <c>/</c> or exact
	/// bit manipulation -- never a call into the platform's own <c>log</c>. Matches
	/// <see cref="Math.Log(double)"/>'s handling of its domain: <see cref="double.NaN"/> for NaN or
	/// negative input, negative infinity for zero, positive infinity for positive infinity.
	/// </summary>
	public static double Log(double x)
	{
		if (double.IsNaN(x) || x < 0.0)
			return double.NaN;

		if (x == 0.0)
			return double.NegativeInfinity;

		if (double.IsPositiveInfinity(x))
			return double.PositiveInfinity;

		var k = 0;
		if ((ToBits(x) & ExponentMask) == 0)
		{
			// Subnormal: lift into the normal range by an exact power-of-two multiply (never
			// rounds) before decomposing -- the same trick fdlibm's own e_log.c uses.
			k -= 54;
			x *= TwoPow54;
		}

		var bits = ToBits(x);
		var exponent = (int)((bits >> MantissaBits) & 0x7FF) - ExponentBias;
		k += exponent;

		// Reconstruct x's own mantissa with the exponent field forced to the bias, giving m in [1, 2)
		// with exactly x's own fraction bits -- no rounding, a field replacement.
		var m = FromBits((bits & MantissaMask) | ((long)ExponentBias << MantissaBits));

		// Round m to the nearer of 1 or 2 by comparing against sqrt(2), so the reduced f = m - 1
		// below stays small in both directions (fdlibm's own normalization step).
		if (m > Sqrt2)
		{
			m *= 0.5; // Exact: exponent field only.
			k += 1;
		}

		var f = m - 1.0;

		// log(1+f) = log((1+s)/(1-s)) = 2*artanh(s) where s = f/(2+f) -- an identity, not an
		// approximation; only EvaluateLogSeries's truncation is approximate.
		var s = f / (2.0 + f);
		var z = s * s;
		var twoS = 2.0 * s;
		var log1PlusF = twoS + twoS * EvaluateLogSeries(z);

		if (k == 0)
			return log1PlusF;

		// k's magnitude can reach into the low thousands (a subnormal's binary exponent), so ln2's
		// Cody-Waite split is needed here too -- see its own remarks.
		var result = k * Ln2CodyWaite.Hi;
		result += k * Ln2CodyWaite.Mid;
		result += k * Ln2CodyWaite.Tail;
		result += log1PlusF;
		return result;
	}

	/// <summary>
	/// R(z) = z/3 + z^2/5 + z^3/7 + ... (z = s^2), the series remainder in <c>2*artanh(s) = 2s*(1+R(z))</c>.
	/// Every coefficient <c>1/(2j+1)</c> is an exact IEEE division of two small integers -- a plain
	/// Taylor series, not <c>fdlibm</c>'s minimax-fitted <c>Lg1..Lg7</c>, so it needs no memorized
	/// constant at all. <see cref="Log"/>'s normalization keeps <c>|s| &lt;= (sqrt(2)-1)/(2+sqrt(2)-1)
	/// ~= 0.1716</c>, so <c>z &lt;= ~0.02944</c> and the 12th term here is already below 1e-19 --
	/// comfortably past a <see cref="double"/>'s ~1.1e-16 relative precision.
	/// </summary>
	private static double EvaluateLogSeries(double z)
	{
		var acc = 1.0 / (2 * LogSeriesTerms + 1);
		for (var j = LogSeriesTerms - 1; j >= 1; j--)
			acc = 1.0 / (2 * j + 1) + z * acc;

		return z * acc;
	}
}

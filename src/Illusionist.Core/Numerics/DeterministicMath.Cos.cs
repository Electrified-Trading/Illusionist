namespace Illusionist.Core.Numerics;

public static partial class DeterministicMath
{
	/// <summary>
	/// Below this magnitude, <see cref="ReduceMediumRange"/>'s plain-<see cref="double"/> Cody-Waite
	/// reduction is fast and accurate to wide margin; at or above it, <see cref="ReduceExact"/>'s
	/// <see cref="System.Numerics.BigInteger"/> reduction takes over. 2^16 comfortably covers every
	/// angle <c>brownian-bridge@2</c> ever actually computes (<c>2*pi*u</c> for <c>u</c> in <c>(0,1)</c>)
	/// with wide headroom for a "large angle" test sweep well beyond it, while keeping the
	/// <see cref="System.Numerics.BigInteger"/> path -- needed for full correctness up to any finite
	/// <see cref="double"/> -- off the hot path entirely.
	/// </summary>
	private const double MediumRangeLimit = 65536.0;

	/// <summary>
	/// A deterministic cosine: within about 1 ulp of <see cref="Math.Cos(double)"/> on every
	/// platform, because every step below (including argument reduction) is <c>+</c>, <c>-</c>,
	/// <c>*</c>, <c>/</c>, <see cref="Math.Sqrt(double)"/> or exact bit manipulation -- never a call
	/// into the platform's own <c>cos</c>. Matches <see cref="Math.Cos(double)"/>'s handling of its
	/// domain: <see cref="double.NaN"/> for NaN or either infinity, and correct argument reduction
	/// for any finite magnitude, however large.
	/// </summary>
	public static double Cos(double x)
	{
		if (!double.IsFinite(x))
			return double.NaN;

		x = Math.Abs(x); // cos is even; this also folds -0.0 to +0.0 harmlessly.

		if (x <= PiOver4)
			return KernelCos(x, 0.0);

		double y0, y1;
		var quadrant = x < MediumRangeLimit
			? ReduceMediumRange(x, out y0, out y1)
			: ReduceExact(x, out y0, out y1);

		return (quadrant & 3) switch
		{
			0 => KernelCos(y0, y1),
			1 => -KernelSin(y0, y1),
			2 => -KernelCos(y0, y1),
			_ => KernelSin(y0, y1),
		};
	}
}

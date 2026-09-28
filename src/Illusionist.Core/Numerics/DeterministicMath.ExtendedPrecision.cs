namespace Illusionist.Core.Numerics;

public static partial class DeterministicMath
{
	/// <summary>
	/// An unevaluated double-double: <see cref="Hi"/> plus the tiny correction <see cref="Lo"/>,
	/// together carrying roughly twice a <see cref="double"/>'s significant bits. Used only for this
	/// file's own constants (<c>pi</c>, <c>pi/2</c>, <c>ln2</c>) and argument-reduction residuals --
	/// never returned to a caller.
	/// </summary>
	private readonly record struct Extended(double Hi, double Lo);

	/// <summary>
	/// Veltkamp's split: <paramref name="value"/> into a 27-bit-clean <paramref name="hi"/> and the
	/// exact remainder <paramref name="lo"/>, such that <c>hi + lo == value</c> exactly and
	/// <c>hi * anything</c> below has no more than 26 significant bits below its own leading bit --
	/// the building block <see cref="TwoProduct"/> needs to multiply two doubles exactly without a
	/// fused multiply-add.
	/// </summary>
	private static void Split(double value, out double hi, out double lo)
	{
		const double splitter = 134217729.0; // 2^27 + 1.
		var t = splitter * value;
		hi = t - (t - value);
		lo = value - hi;
	}

	/// <summary>Dekker's algorithm: the exact product <c>a * b == hi + lo</c>, using only <c>+</c>, <c>-</c>, <c>*</c> (never <see cref="Math.FusedMultiplyAdd(double, double, double)"/> -- see the class remarks).</summary>
	private static void TwoProduct(double a, double b, out double hi, out double lo)
	{
		hi = a * b;
		Split(a, out var aHi, out var aLo);
		Split(b, out var bHi, out var bLo);
		lo = ((aHi * bHi - hi) + aHi * bLo + aLo * bHi) + aLo * bLo;
	}

	/// <summary>Knuth's TwoSum: the exact sum <c>a + b == hi + lo</c>, for any <paramref name="a"/>, <paramref name="b"/> (no ordering assumed).</summary>
	private static void TwoSum(double a, double b, out double hi, out double lo)
	{
		hi = a + b;
		var v = hi - a;
		lo = (a - (hi - v)) + (b - v);
	}

	/// <summary>
	/// Double-double multiplication, dropping only the <c>Lo*Lo</c> term (already below either
	/// operand's own <see cref="Extended"/> precision floor) -- the standard simplification used
	/// throughout double-double arithmetic libraries.
	/// </summary>
	private static Extended Multiply(Extended a, Extended b)
	{
		TwoProduct(a.Hi, b.Hi, out var p1, out var p2);
		p2 += a.Hi * b.Lo + a.Lo * b.Hi;
		TwoSum(p1, p2, out var hi, out var lo);
		return new Extended(hi, lo);
	}
}

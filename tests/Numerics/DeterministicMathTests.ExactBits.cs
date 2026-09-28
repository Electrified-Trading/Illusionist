using Illusionist.Core.Numerics;

namespace Illusionist.Tests.Numerics;

/// <summary>
/// Pins <see cref="DeterministicMath.Log"/>, <see cref="DeterministicMath.Exp"/> and
/// <see cref="DeterministicMath.Cos"/>'s own output, bit for bit, at a fixed table of inputs. Unlike
/// <see cref="DeterministicMathTests"/>'s sweep (which measures agreement with this host's own
/// <c>Math.*</c> and is not expected to hit 0 ULP everywhere -- see its own remarks), this test
/// captures nothing external: every expected value is <see cref="DeterministicMath"/>'s own result,
/// recorded once by running it here and never regenerated since. Every step in
/// <see cref="DeterministicMath"/> is <c>+</c>, <c>-</c>, <c>*</c>, <c>/</c>, <see cref="Math.Sqrt(double)"/>
/// or exact bit manipulation -- IEEE-754-mandated to round identically on every conforming platform
/// -- so this table is the real cross-platform guard: any host, any .NET major version, any CPU
/// architecture that computes even one different bit for one of these inputs has broken that
/// guarantee somewhere. See the change-log entry for the mutation-test that proves this table would
/// actually catch such a break (reverting one function to <see cref="Math"/> fails this test even on
/// this same machine).
/// </summary>
/// <remarks>Bit patterns are <see cref="ulong"/>, not <see cref="long"/>: a hex literal with the sign bit set (e.g. <c>0xC086...UL</c>) is <see cref="ulong"/> under C#'s literal-type rules even with an <c>L</c> suffix, so using <see cref="ulong"/> throughout avoids relying on that rule at all.</remarks>
public sealed partial class DeterministicMathTests
{
	public static IEnumerable<object[]> LogCases =>
	[
		[0x3FF0000000000000UL, 0x0000000000000000UL], // Log(1) == 0
		[0x4000000000000000UL, 0x3FE62E42FEFA39EFUL], // Log(2)
		[0x3FE0000000000000UL, 0xBFE62E42FEFA39EFUL], // Log(0.5)
		[0x4024000000000000UL, 0x40026BB1BBB55515UL], // Log(10)
		[0x400921FB54442D18UL, 0x3FF250D048E7A1BDUL], // Log(pi)
		[0x0000000000000001UL, 0xC0874385446D71C3UL], // Log(double.Epsilon), the smallest subnormal
		[0x01A56E1FC2F8F359UL, 0xC085963447F87FB5UL], // Log(1e-300)
		[0x7E37E43C8800759CUL, 0x4085963447F87FB5UL], // Log(1e300)
		[0x3FF000000006DF38UL, 0x3DDB7CDFFFFA18D8UL], // Log(1.0000000001), a hair above 1
		[0x3FEFFFFFFFF24190UL, 0xBDDB7CE00005E728UL], // Log(0.9999999999), a hair below 1
		[0x4330000000000001UL, 0x404205966F2B4F12UL], // Log(4503599627370497), 2^52 + 1
	];

	public static IEnumerable<object[]> ExpCases =>
	[
		[0x0000000000000000UL, 0x3FF0000000000000UL], // Exp(0) == 1
		[0x3FF0000000000000UL, 0x4005BF0A8B14576AUL], // Exp(1) == e
		[0xBFF0000000000000UL, 0x3FD78B56362CEF38UL], // Exp(-1) == 1/e
		[0x3FE0000000000000UL, 0x3FFA61298E1E069CUL], // Exp(0.5)
		[0xBFE0000000000000UL, 0x3FE368B2FC6F960AUL], // Exp(-0.5)
		[0x4024000000000000UL, 0x40D5829DCF950560UL], // Exp(10)
		[0xC024000000000000UL, 0x3F07CD79B5647C9AUL], // Exp(-10)
		[0x4085E00000000000UL, 0x7F0D945DF4F8EC8EUL], // Exp(700), near the overflow boundary
		[0xC085E00000000000UL, 0x00D14F2B0FB9307FUL], // Exp(-700), a subnormal result
		[0x3DDB7CDFD9D7BDBBUL, 0x3FF000000006DF38UL], // Exp(1e-10)
		[0x3FE62E42FEFA39EFUL, 0x4000000000000000UL], // Exp(ln2) == 2
	];

	public static IEnumerable<object[]> CosCases =>
	[
		[0x0000000000000000UL, 0x3FF0000000000000UL], // Cos(0) == 1
		[0x3FE0000000000000UL, 0x3FEC1528065B7D50UL], // Cos(0.5)
		[0x3FF0000000000000UL, 0x3FE14A280FB5068CUL], // Cos(1)
		[0x3FE921FB54442D18UL, 0x3FE6A09E667F3BCDUL], // Cos(pi/4) == sqrt(2)/2
		[0x3FF921FB54442D18UL, 0x3C91A62633145C07UL], // Cos(pi/2): near zero, the worst-conditioned case (see DeterministicMath.Constants.cs)
		[0x400921FB54442D18UL, 0xBFF0000000000000UL], // Cos(pi) == -1
		[0x401921FB54442D18UL, 0x3FF0000000000000UL], // Cos(2*pi) == 1
		[0x4059000000000000UL, 0x3FEB981DBF665FE0UL], // Cos(100), the medium-range reduction path
		[0x412E848000000000UL, 0x3FEDF9DF9906D32CUL], // Cos(1e6), still medium-range
		[0x430C6BF526340000UL, 0xBFE06C154609D33FUL], // Cos(1e15), the exact BigInteger reduction path
		[0x7E37E43C8800759CUL, 0xBFE2699022ADC4C1UL], // Cos(1e300), the far end of the exact reduction path
		[0xBFF0000000000000UL, 0x3FE14A280FB5068CUL], // Cos(-1) == Cos(1)
	];

	[Theory]
	[MemberData(nameof(LogCases))]
	public void Log_ExactBits(ulong xBits, ulong expectedYBits)
		=> AssertExactBits(DeterministicMath.Log, xBits, expectedYBits);

	[Theory]
	[MemberData(nameof(ExpCases))]
	public void Exp_ExactBits(ulong xBits, ulong expectedYBits)
		=> AssertExactBits(DeterministicMath.Exp, xBits, expectedYBits);

	[Theory]
	[MemberData(nameof(CosCases))]
	public void Cos_ExactBits(ulong xBits, ulong expectedYBits)
		=> AssertExactBits(DeterministicMath.Cos, xBits, expectedYBits);

	private static void AssertExactBits(Func<double, double> function, ulong xBits, ulong expectedYBits)
	{
		var x = BitConverter.UInt64BitsToDouble(xBits);
		var actual = function(x);
		var actualBits = BitConverter.DoubleToUInt64Bits(actual);

		Assert.True(
			expectedYBits == actualBits,
			$"x=0x{xBits:X16} ({x:R}): expected bits 0x{expectedYBits:X16}, got 0x{actualBits:X16} ({actual:R}).");
	}
}

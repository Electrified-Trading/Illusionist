namespace Illusionist.Core.Numerics;

/// <summary>
/// A managed, IEEE-754-only <c>log</c>, <c>exp</c> and <c>cos</c>: the transcendental functions
/// <c>brownian-bridge@2</c> uses, reimplemented from only <c>+</c>, <c>-</c>, <c>*</c>, <c>/</c>,
/// <see cref="Math.Sqrt(double)"/> and exact bit manipulation, so their output is byte-identical on
/// every host -- never a call into <see cref="Math.Log(double)"/>, <see cref="Math.Exp(double)"/> or
/// <see cref="Math.Cos(double)"/>, which route to the platform's own C runtime (UCRT on Windows,
/// glibc on Linux) and are not guaranteed to round identically in the last bit.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this exists.</b> <c>brownian-bridge@1</c>'s golden fixtures, pinned on x64 Windows, did
/// not reproduce on x64 Linux for two of ten cases (seed-dependent: whichever draws happened to
/// land on an input where UCRT's and glibc's <c>log</c>/<c>cos</c> round differently). A one-ulp
/// difference in a single Gaussian draw propagates through the Brownian bridge into a different
/// bar. <c>@1</c> is frozen exactly as published (see <c>BrownianBridgeBarSeries</c>); this type
/// backs a new, independent version, <c>brownian-bridge@2</c>
/// (<c>BrownianBridgeBarSeriesV2</c>), with the same algorithm and parameters but a
/// platform-independent math core.
/// </para>
/// <para>
/// <b>Provenance.</b> The structure below (exponent/mantissa decomposition, argument reduction,
/// odd/even polynomial kernels) follows the public-domain-adjacent <c>fdlibm</c> ("Freely
/// Distributable LIBM", Sun Microsystems, 1993) design that most platform libms -- including
/// glibc's and, historically, early Java's <c>StrictMath</c> -- are themselves derived from.
/// <c>fdlibm</c>'s own notice permits use and redistribution provided it is preserved:
/// </para>
/// <para>
/// <c>
/// Copyright (C) 1993 by Sun Microsystems, Inc. All rights reserved.
/// Developed at SunPro, a Sun Microsystems, Inc. business.
/// Permission to use, copy, modify, and distribute this software is freely
/// granted, provided that this notice is preserved.
/// </c>
/// </para>
/// <para>
/// This is not a transcription of <c>fdlibm</c>'s C source, and deliberately does not reuse its
/// polynomial coefficients (memorizing 15-16 significant digits from a source this environment
/// cannot fetch and diff against is itself a reproducibility risk). Instead, every coefficient here
/// is an exact rational number -- <c>1.0 / (2 * j + 1)</c>, <c>1.0 / n!</c> -- computed by ordinary
/// IEEE division at run time, which is required to be correctly rounded on every conforming
/// platform. The one place <c>fdlibm</c>'s own giant lookup table (<c>two_over_pi[]</c>, 396
/// <c>int32</c> words, for reducing huge arguments modulo <c>pi/2</c>) would normally appear, this
/// implementation instead derives <c>pi</c> itself from Machin's formula
/// (<c>pi = 16*atan(1/5) - 4*atan(1/239)</c>) using exact <see cref="System.Numerics.BigInteger"/>
/// arithmetic (see <c>DeterministicMath.Constants.cs</c>) -- auditable from the formula alone,
/// rather than trusted from a table this port's author could not verify against a reference.
/// </para>
/// <para>
/// <b>No fused multiply-add.</b> The extended-precision (double-double) arithmetic in
/// <c>DeterministicMath.ExtendedPrecision.cs</c> depends on every <c>+</c> and <c>*</c> below being
/// a single, independently and correctly rounded IEEE-754 operation -- never contracted into a
/// fused multiply-add (which would compute <c>a*b+c</c> with one rounding instead of two, silently
/// changing these algorithms' results). .NET does not perform this contraction implicitly: the JIT
/// never fuses an ordinary <c>a * b + c</c> expression, and only the explicit
/// <see cref="Math.FusedMultiplyAdd(double, double, double)"/> API performs one. This is consistent
/// with .NET Core 3.0's documented move to strict, hardware-independent IEEE-754 floating-point
/// semantics (ending the earlier x87-era practice of silently computing intermediates at higher
/// precision) -- UNVERIFIED against a citable specification in this environment (no network
/// access), stated here so a future reader can check it before ever introducing
/// <see cref="Math.FusedMultiplyAdd(double, double, double)"/> into this file.
/// </para>
/// </remarks>
public static partial class DeterministicMath;

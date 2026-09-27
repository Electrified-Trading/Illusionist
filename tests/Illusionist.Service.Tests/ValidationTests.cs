namespace Illusionist.Service.Tests;

/// <summary>
/// Every D5 error code and variant reachable through <see cref="SeriesKeyParser"/>'s real input
/// space, asserting the exact code and parameter (a representative subset also asserts the exact
/// message, spot-checked against D5's table verbatim), plus the fixed validation-order precedence.
/// </summary>
/// <remarks>
/// <c>price_out_of_range</c> IS reachable through real, unsubstituted input: <see cref="PriceOutOfRange_RealOverflow_NoMocks"/>
/// below reproduces it via <see cref="GeneratorRegistry"/>/<see cref="SeriesService"/> directly, no
/// <c>NSubstitute</c> involved (an anchor/range separation near the legal maximum of ~300 years,
/// at a legal drift, overflows <see langword="decimal"/>). An earlier version of this file, and of
/// this task's own build notes, claimed the opposite -- that it "could not be reached through any
/// real, in-range parameter combination" -- based on a hand analysis that only considered
/// count-mode's ~80-year ceiling and missed that <c>anchorDate</c> and a <c>from</c>/<c>to</c>
/// range are independent and may legally sit up to ~300 years apart.
/// <see cref="ToolCatalogTests"/> keeps one substituted-source <c>price_out_of_range</c> case (a
/// zero <c>Open</c> with no exception involved) because it exercises a different guard than the
/// overflow path and adds real coverage the real test cannot reach.
/// <c>internal_error</c> is genuinely untested, and unreachable: <c>SeriesOutcome</c>
/// (<c>Series/SeriesOutcome.cs</c>) is a closed two-case union (<c>Success</c>/<c>Failure</c>) with
/// a private constructor, so the <c>var other =&gt; InternalError()</c> branches in
/// <c>McpHandlers.Series.cs</c>, <c>McpHandlers.ReadResource.cs</c> and <c>RestEndpoints.Series.cs</c>
/// exist only to satisfy switch-expression exhaustiveness and cannot execute through any legal call.
/// </remarks>
public sealed class ValidationTests
{
	private static readonly IGeneratorRegistry Registry = new GeneratorRegistry(GeneratorCatalog.All);
	private static readonly ISeriesService SeriesService = new SeriesService(Registry);

	/// <summary>
	/// Mirrors the real surfaces' own two-step pipeline: <see cref="SeriesQuery.ParseQueryText"/>
	/// (readiness happens first in production, but has no bearing on parsing, so it is passed
	/// straight through to <see cref="ISeriesService.Generate"/> here) can itself throw for
	/// unknown/duplicate names, exactly as <c>RestEndpoints</c>/<c>McpHandlers</c> catch it.
	/// </summary>
	private static IllusionistError Fail(string query, bool ready = true)
	{
		IReadOnlyDictionary<string, string> raw;
		try
		{
			raw = SeriesQuery.ParseQueryText(query);
		}
		catch (IllusionistErrorException ex)
		{
			return ex.Error;
		}

		var outcome = SeriesService.Generate(raw, ready);
		return Assert.IsType<SeriesOutcome.Failure>(outcome).Error;
	}

	[Fact]
	public void NotReady_ExactMessage()
	{
		var error = Fail("generator=brownian-bridge@1&seed=1", ready: false);

		Assert.Equal(ErrorCodes.NotReady, error.Code);
		Assert.Equal(
			"This host failed its golden self-check, so its output may not match the reproducibility contract. See /healthz.",
			error.Message);
	}

	[Fact]
	public void UnknownParameter_ExactMessage()
	{
		var error = Fail("generator=brownian-bridge@1&seed=1&volatilty=0.5");

		Assert.Equal(ErrorCodes.UnknownParameter, error.Code);
		Assert.Equal("volatilty", error.Parameter);
		Assert.Equal(
			"Unknown parameter 'volatilty'. Known parameters: generator, seedMode, seed, symbol, drift, volatility, anchorDate, anchorPrice, timeframe, count, from, to, format.",
			error.Message);
	}

	[Fact]
	public void DuplicateParameter_ExactMessage()
	{
		var error = Fail("generator=brownian-bridge@1&seed=1&seed=2");

		Assert.Equal(ErrorCodes.DuplicateParameter, error.Code);
		Assert.Equal("seed", error.Parameter);
		Assert.Equal("Parameter 'seed' was given more than once.", error.Message);
	}

	[Fact]
	public void MissingGenerator_ExactMessage()
	{
		var error = Fail("seed=1");

		Assert.Equal(ErrorCodes.MissingParameter, error.Code);
		Assert.Equal("generator", error.Parameter);
		Assert.Equal("'generator' is required, as id@version, e.g. 'brownian-bridge@1'.", error.Message);
	}

	[Fact]
	public void MissingSeed_ExactMessage()
	{
		var error = Fail("generator=brownian-bridge@1");

		Assert.Equal(ErrorCodes.MissingParameter, error.Code);
		Assert.Equal("seed", error.Parameter);
		Assert.Equal("'seed' is required: any 32-bit integer.", error.Message);
	}

	[Fact]
	public void MissingSymbol_WhenSymbolHashed()
	{
		var error = Fail("generator=brownian-bridge@1&seed=1&seedMode=symbol-hashed");

		Assert.Equal(ErrorCodes.MissingParameter, error.Code);
		Assert.Equal("symbol", error.Parameter);
		Assert.Equal("'symbol' is required when seedMode is 'symbol-hashed'.", error.Message);
	}

	[Fact]
	public void MissingTo_WhenOnlyFromGiven()
	{
		var error = Fail("generator=brownian-bridge@1&seed=1&from=2023-03-01");

		Assert.Equal(ErrorCodes.MissingParameter, error.Code);
		Assert.Equal("to", error.Parameter);
		Assert.Equal("'from' and 'to' must be given together.", error.Message);
	}

	[Fact]
	public void InvalidGeneratorRef_ExactMessage()
	{
		var error = Fail("generator=bogus&seed=1");

		Assert.Equal(ErrorCodes.InvalidGeneratorRef, error.Code);
		Assert.Equal("generator", error.Parameter);
		Assert.Equal("'generator' must be id@version, e.g. 'brownian-bridge@1'; got 'bogus'.", error.Message);
	}

	[Fact]
	public void UnknownGenerator()
	{
		var error = Fail("generator=nonexistent@1&seed=1");

		Assert.Equal(ErrorCodes.UnknownGenerator, error.Code);
		Assert.Equal(404, error.HttpStatus);
	}

	[Fact]
	public void UnknownGeneratorVersion()
	{
		// @3 is deliberately never registered (brownian-bridge@1 and @2 are real) -- an unknown
		// *version* of a known id.
		var error = Fail("generator=brownian-bridge@3&seed=1");

		Assert.Equal(ErrorCodes.UnknownGeneratorVersion, error.Code);
		Assert.Equal(404, error.HttpStatus);
	}

	[Theory]
	[InlineData("generator=brownian-bridge@1&seed=abc", "seed")]
	[InlineData("generator=brownian-bridge@1&seed=1&drift=notanumber", "drift")]
	[InlineData("generator=brownian-bridge@1&seed=1&anchorPrice=1e2", "anchorPrice")]
	[InlineData("generator=brownian-bridge@1&seed=1&anchorDate=not-a-date", "anchorDate")]
	[InlineData("generator=brownian-bridge@1&seed=1&count=abc", "count")]
	[InlineData("generator=brownian-bridge@1&seedMode=symbol-hashed&symbol=###&seed=1", "symbol")]
	public void InvalidParameter_Variants(string query, string expectedParameter)
	{
		var error = Fail(query);

		Assert.Equal(ErrorCodes.InvalidParameter, error.Code);
		Assert.Equal(expectedParameter, error.Parameter);
	}

	[Theory]
	[InlineData("generator=brownian-bridge@1&seed=1&drift=10", "drift")]
	[InlineData("generator=brownian-bridge@1&seed=1&volatility=2", "volatility")]
	[InlineData("generator=brownian-bridge@1&seed=1&anchorPrice=0.001", "anchorPrice")]
	[InlineData("generator=brownian-bridge@1&seed=1&count=0", "count")]
	public void OutOfRange_Variants(string query, string expectedParameter)
	{
		var error = Fail(query);

		Assert.Equal(ErrorCodes.OutOfRange, error.Code);
		Assert.Equal(expectedParameter, error.Parameter);
	}

	[Fact]
	public void ConflictingParameters_SymbolInBareMode_ExactMessage()
	{
		var error = Fail("generator=brownian-bridge@1&seed=1&symbol=AAPL");

		Assert.Equal(ErrorCodes.ConflictingParameters, error.Code);
		Assert.Equal(
			"'symbol' is only used when seedMode is 'symbol-hashed'; remove it or set seedMode to 'symbol-hashed'.",
			error.Message);
	}

	[Fact]
	public void ConflictingParameters_CountAndRange_ExactMessage()
	{
		var error = Fail("generator=brownian-bridge@1&seed=1&count=10&from=2023-03-01&to=2023-03-10");

		Assert.Equal(ErrorCodes.ConflictingParameters, error.Code);
		Assert.Equal("Use either 'count' or 'from' and 'to', not both.", error.Message);
	}

	[Theory]
	[InlineData("2023-03-04")] // a Saturday
	[InlineData("2024-01-01")] // New Year's Day, in the known 2024-2025 calendar
	public void NotATradingDay(string anchorDate)
	{
		var error = Fail($"generator=brownian-bridge@1&seed=1&anchorDate={anchorDate}");

		Assert.Equal(ErrorCodes.NotATradingDay, error.Code);
		Assert.Equal("anchorDate", error.Parameter);
	}

	[Fact]
	public void InvalidRange_FromAfterTo_ExactMessage()
	{
		var error = Fail("generator=brownian-bridge@1&seed=1&from=2023-03-10&to=2023-03-01");

		Assert.Equal(ErrorCodes.InvalidRange, error.Code);
		Assert.Equal("'from' (2023-03-10) is after 'to' (2023-03-01).", error.Message);
	}

	[Fact]
	public void TooManyBars_Count()
	{
		var error = Fail("generator=brownian-bridge@1&seed=1&count=20001");

		Assert.Equal(ErrorCodes.TooManyBars, error.Code);
		Assert.Equal("count", error.Parameter);
	}

	[Fact]
	public void TooManyBars_Range()
	{
		var error = Fail("generator=brownian-bridge@1&seed=1&from=1900-01-01&to=2199-12-31");

		Assert.Equal(ErrorCodes.TooManyBars, error.Code);
	}

	/// <summary>
	/// price_out_of_range through the real, unsubstituted <see cref="GeneratorRegistry"/>/
	/// <see cref="SeriesService"/> path -- no mocks. anchorDate and a from/to range are independent
	/// (D2: "before or after the anchor") and may legally sit up to ~300 years apart within
	/// [1900-01-01, 2199-12-31]; a legal drift (0.3, inside [-0.5, 0.5]) compounded over that span
	/// overflows <see langword="decimal"/> well before any date-range or bar-count limit is hit
	/// (measured threshold ~0.22/yr at the maximum legal separation).
	/// </summary>
	[Fact]
	public void PriceOutOfRange_RealOverflow_NoMocks()
	{
		var error = Fail(
			"generator=brownian-bridge@1&seed=1&drift=0.3&volatility=0&anchorDate=1900-01-02&from=2199-01-01&to=2199-01-10");

		Assert.Equal(ErrorCodes.PriceOutOfRange, error.Code);
		Assert.Equal(422, error.HttpStatus);
		Assert.Equal(
			"These parameters push the price outside the representable range at bar 0 (2199-01-01); lower volatility, drift, anchorPrice or the number of bars.",
			error.Message);
	}

	// Validation-order precedence (D5): the first field in the fixed order wins when several are wrong at once.

	[Fact]
	public void Precedence_UnknownParameterBeatsInvalidGenerator()
	{
		var error = Fail("generator=bogus&seed=1&nope=1");

		Assert.Equal(ErrorCodes.UnknownParameter, error.Code);
	}

	[Fact]
	public void Precedence_GeneratorBeatsMissingSeed()
	{
		var error = Fail("generator=bogus");

		Assert.Equal(ErrorCodes.InvalidGeneratorRef, error.Code);
	}

	[Fact]
	public void Precedence_SeedModeBeatsInvalidSeed()
	{
		var error = Fail("generator=brownian-bridge@1&seedMode=bogus&seed=abc");

		Assert.Equal(ErrorCodes.InvalidParameter, error.Code);
		Assert.Equal("seedMode", error.Parameter);
	}

	[Fact]
	public void Precedence_SymbolConflictBeatsDriftOutOfRange()
	{
		var error = Fail("generator=brownian-bridge@1&seed=1&symbol=AAPL&drift=10");

		Assert.Equal(ErrorCodes.ConflictingParameters, error.Code);
		Assert.Equal("symbol", error.Parameter);
	}

	[Fact]
	public void Precedence_AnchorDateBeatsAnchorPriceOutOfRange()
	{
		var error = Fail("generator=brownian-bridge@1&seed=1&anchorDate=2024-01-01&anchorPrice=0.001");

		Assert.Equal(ErrorCodes.NotATradingDay, error.Code);
	}

	[Fact]
	public void Precedence_ExtentBeatsFormat()
	{
		var error = Fail("generator=brownian-bridge@1&seed=1&count=20001&format=bogus");

		Assert.Equal(ErrorCodes.TooManyBars, error.Code);
	}
}

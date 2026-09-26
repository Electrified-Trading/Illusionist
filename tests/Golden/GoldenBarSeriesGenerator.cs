using Illusionist.Core.Catalog;

namespace Illusionist.Tests.Golden;

/// <summary>
/// Generates a <see cref="GoldenBarSeriesCase"/>'s bars by reimplementing a downstream
/// consumer's own two call-site loops verbatim, so a regression in either loop's own logic --
/// not just the generator -- is caught by whatever test consumes this
/// (<see cref="GeneratorGoldenTests"/> and <see cref="ReproducibilityScopeTests"/> both do).
/// </summary>
internal static class GoldenBarSeriesGenerator
{
	/// <summary>
	/// The daily U.S. equities schedule every case shares -- the only schedule a downstream
	/// consumer's own regeneration path can resolve today.
	/// </summary>
	public static readonly ISchedule ReferenceSchedule =
		new DefaultEquitiesScheduleFactory().GetSchedule(BarInterval.Day(1));

	/// <summary>Generates <paramref name="case"/>'s bars under its own recorded parameters.</summary>
	public static IReadOnlyList<Bar<OHLC>> Generate(GoldenBarSeriesCase @case)
	{
		var anchor = new BarAnchor(GoldenBarSeriesCases.ReferenceAnchorTimestamp, @case.AnchorPrice);

		return @case.Path switch
		{
			GoldenGenerationPath.BareSeed => GenerateBareSeed(@case, anchor),
			GoldenGenerationPath.SymbolHashed => GenerateSymbolHashed(@case, anchor),
			_ => throw new NotSupportedException($"Unhandled generation path '{@case.Path}'."),
		};
	}

	/// <summary>
	/// Reimplements a downstream consumer's own production regeneration path verbatim: an
	/// independent <see cref="BrownianBridgeBarSeries.Generator"/> whose own bar 0 is generated
	/// exactly at the anchor's timestamp, advanced only through the generator's own schedule.
	/// </summary>
	private static IReadOnlyList<Bar<OHLC>> GenerateBareSeed(GoldenBarSeriesCase @case, BarAnchor anchor)
	{
		var generator = new BrownianBridgeBarSeries.Generator(
			@case.Seed, ReferenceSchedule, @case.Drift, @case.Volatility, anchor);

		var bars = new List<Bar<OHLC>>(@case.BarCount);
		var current = anchor.Timestamp;
		for (var i = 0; i < @case.BarCount; i++)
		{
			bars.Add(generator.GetBarAt(current));
			current = generator.Schedule.GetNextValidBarTime(current);
		}

		return bars;
	}

	/// <summary>
	/// Reimplements a downstream consumer's own cross-process stability check's call path: the
	/// symbol-hashing <see cref="BrownianBridgeBarSeries.Factory"/>, streamed from
	/// <see cref="IBarSeries{T}.GetBars"/> starting at the anchor's own timestamp.
	/// </summary>
	private static IReadOnlyList<Bar<OHLC>> GenerateSymbolHashed(GoldenBarSeriesCase @case, BarAnchor anchor)
	{
		var factory = new BrownianBridgeBarSeries.Factory(@case.Symbol!, @case.Seed, @case.Drift, @case.Volatility);
		var series = factory.GetSeries(ReferenceSchedule, anchor);

		return series.GetBars(anchor.Timestamp).Take(@case.BarCount).ToList();
	}
}

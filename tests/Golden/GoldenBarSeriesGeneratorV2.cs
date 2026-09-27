using Illusionist.Core.Catalog;

namespace Illusionist.Tests.Golden;

/// <summary>
/// Generates a <see cref="GoldenBarSeriesCase"/>'s bars through <see cref="BrownianBridgeBarSeriesV2"/>
/// -- the <c>@2</c> twin of <see cref="GoldenBarSeriesGenerator"/>, reusing
/// <see cref="GoldenBarSeriesGenerator.ReferenceSchedule"/> and the shared anchor timestamp so the
/// only difference between a <c>@1</c> and <c>@2</c> case with the same parameters is the generator
/// itself.
/// </summary>
internal static class GoldenBarSeriesGeneratorV2
{
	/// <summary>Generates <paramref name="case"/>'s bars under its own recorded parameters, via <c>brownian-bridge@2</c>.</summary>
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

	private static IReadOnlyList<Bar<OHLC>> GenerateBareSeed(GoldenBarSeriesCase @case, BarAnchor anchor)
	{
		var generator = new BrownianBridgeBarSeriesV2.Generator(
			@case.Seed, GoldenBarSeriesGenerator.ReferenceSchedule, @case.Drift, @case.Volatility, anchor);

		var bars = new List<Bar<OHLC>>(@case.BarCount);
		var current = anchor.Timestamp;
		for (var i = 0; i < @case.BarCount; i++)
		{
			bars.Add(generator.GetBarAt(current));
			current = generator.Schedule.GetNextValidBarTime(current);
		}

		return bars;
	}

	private static IReadOnlyList<Bar<OHLC>> GenerateSymbolHashed(GoldenBarSeriesCase @case, BarAnchor anchor)
	{
		var factory = new BrownianBridgeBarSeriesV2.Factory(@case.Symbol!, @case.Seed, @case.Drift, @case.Volatility);
		var series = factory.GetSeries(GoldenBarSeriesGenerator.ReferenceSchedule, anchor);

		return series.GetBars(anchor.Timestamp).Take(@case.BarCount).ToList();
	}
}

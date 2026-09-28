using Illusionist.Core.Catalog;

namespace Illusionist.Service.Generators;

/// <summary>
/// <c>brownian-bridge@2</c>: the same Brownian-bridge GBM generator as <c>@1</c>
/// (<see cref="BrownianBridgeV1"/>), but backed by <see cref="BrownianBridgeBarSeriesV2"/>, whose
/// Gaussian draws go through <see cref="Illusionist.Core.Numerics.DeterministicMath"/> instead of
/// <see cref="Math"/> -- see that type's class remarks for why. <c>@1</c> is never edited in place
/// (D7): this is a new, independently registered version.
/// </summary>
public sealed partial class BrownianBridgeV2 : IGeneratorVersion, IDescribableGenerator
{
	private BrownianBridgeV2()
	{
	}

	/// <summary>The one instance; registered once in <see cref="GeneratorCatalog"/>.</summary>
	public static BrownianBridgeV2 Instance { get; } = new();

	/// <inheritdoc />
	public GeneratorRef Ref { get; } = new("brownian-bridge", 2);

	/// <summary>
	/// <see langword="null"/>: unlike <see cref="BrownianBridgeV1"/>, this version has no reference
	/// platform to be limited to -- every step of its Gaussian draw is built only from IEEE-754-exact
	/// operations (see <see cref="Illusionist.Core.Numerics.DeterministicMath"/>), so its golden
	/// cases are expected to reproduce on any host whose self-check runs at all.
	/// </summary>
	public string? ReferencePlatform => null;

	/// <inheritdoc />
	public ISeriesSource Open(SeriesKey key)
	{
		var schedule = new DefaultEquitiesScheduleFactory().GetSchedule(BarInterval.Day(1));
		var anchor = new BarAnchor(key.AnchorDate.ToDateTime(DefaultEquitiesSchedule.MarketOpen), key.AnchorPrice);

		Func<DateTime, Bar<OHLC>> barAt = key.SeedMode switch
		{
			SeedMode.Bare
				=> new BrownianBridgeBarSeriesV2.Generator(key.Seed, schedule, key.Drift, key.Volatility, anchor).GetBarAt,

			SeedMode.SymbolHashed
				=> new BrownianBridgeBarSeriesV2.Factory(key.Symbol!, key.Seed, key.Drift, key.Volatility)
					.GetSeries(schedule, anchor).GetBarAt,

			_ => throw new ArgumentOutOfRangeException(nameof(key), key.SeedMode, "Unhandled seed mode."),
		};

		return new Source(barAt, schedule, anchor, key.Extent);
	}

	/// <summary>
	/// Stateless random access over one opened key -- identical shape to <see cref="BrownianBridgeV1"/>'s
	/// own private <c>Source</c> (both walk <see cref="Illusionist.Core.ISchedule"/> the same way);
	/// duplicated here rather than shared, consistent with keeping <c>@1</c> and <c>@2</c> fully
	/// independent end to end.
	/// </summary>
	private sealed class Source(
		Func<DateTime, Bar<OHLC>> barAt,
		ISchedule schedule,
		BarAnchor anchor,
		SeriesExtent extent) : ISeriesSource
	{
		public Bar<OHLC> BarAt(DateTime timestamp)
			=> barAt(timestamp);

		public IReadOnlyList<DateTime>? Timestamps(int maxBars)
			=> extent switch
			{
				SeriesExtent.Count(var count) => CountTimestamps(count),
				SeriesExtent.Range(var from, var to) => RangeTimestamps(from, to, maxBars),
				_ => throw new ArgumentOutOfRangeException(nameof(extent)),
			};

		private List<DateTime> CountTimestamps(int count)
		{
			var result = new List<DateTime>(count);
			var t = anchor.Timestamp;
			for (var i = 0; i < count; i++)
			{
				result.Add(t);
				t = schedule.GetNextValidBarTime(t);
			}

			return result;
		}

		private List<DateTime>? RangeTimestamps(DateOnly from, DateOnly to, int maxBars)
		{
			var t = from.ToDateTime(DefaultEquitiesSchedule.MarketOpen);
			if (!schedule.IsValidBarTime(t))
				t = schedule.GetNextValidBarTime(t);

			var result = new List<DateTime>();
			while (DateOnly.FromDateTime(t) <= to)
			{
				result.Add(t);
				if (result.Count > maxBars)
					return null;

				t = schedule.GetNextValidBarTime(t);
			}

			return result;
		}
	}
}

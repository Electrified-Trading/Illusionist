using Illusionist.Core.Catalog;

namespace Illusionist.Service.Generators;

/// <summary>
/// <c>brownian-bridge@1</c>: the frozen Brownian-bridge GBM generator (<see cref="BrownianBridgeBarSeries"/>),
/// wired to the service's <see cref="IGeneratorVersion"/> contract. Never edited in place -- an
/// output-changing algorithm gets a new version and its own registration (D7).
/// </summary>
public sealed partial class BrownianBridgeV1 : IGeneratorVersion, IDescribableGenerator
{
	private BrownianBridgeV1()
	{
	}

	/// <summary>The one instance; registered once in <see cref="GeneratorCatalog"/>.</summary>
	public static BrownianBridgeV1 Instance { get; } = new();

	/// <inheritdoc />
	public GeneratorRef Ref { get; } = new("brownian-bridge", 1);

	/// <inheritdoc />
	public ISeriesSource Open(SeriesKey key)
	{
		var schedule = new DefaultEquitiesScheduleFactory().GetSchedule(BarInterval.Day(1));
		var anchor = new BarAnchor(key.AnchorDate.ToDateTime(DefaultEquitiesSchedule.MarketOpen), key.AnchorPrice);

		Func<DateTime, Bar<OHLC>> barAt = key.SeedMode switch
		{
			SeedMode.Bare
				=> new BrownianBridgeBarSeries.Generator(key.Seed, schedule, key.Drift, key.Volatility, anchor).GetBarAt,

			SeedMode.SymbolHashed
				=> new BrownianBridgeBarSeries.Factory(key.Symbol!, key.Seed, key.Drift, key.Volatility)
					.GetSeries(schedule, anchor).GetBarAt,

			_ => throw new ArgumentOutOfRangeException(nameof(key), key.SeedMode, "Unhandled seed mode."),
		};

		return new Source(barAt, schedule, anchor, key.Extent);
	}

	/// <summary>
	/// Stateless random access over one opened key: <see cref="BarAt"/> is a pure function of the
	/// timestamp, and <see cref="Timestamps"/> walks the schedule to answer count or range extents
	/// without generating any bar.
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

		/// <summary>The golden loop, verbatim: walk <paramref name="count"/> bars from the anchor.</summary>
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

		/// <summary>
		/// Walks every trading-day timestamp in <c>[from, to]</c>, stopping (and returning
		/// <see langword="null"/>) the moment the count would exceed <paramref name="maxBars"/> --
		/// holidays and weekends make the count impossible to derive arithmetically.
		/// </summary>
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

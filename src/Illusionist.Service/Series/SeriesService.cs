using System.Text;
using Illusionist.Service.Generators;

namespace Illusionist.Service.Series;

/// <summary>The one generate-and-render implementation every surface shares (D8).</summary>
public sealed class SeriesService(IGeneratorRegistry registry) : ISeriesService
{
	/// <inheritdoc />
	public SeriesOutcome Generate(IReadOnlyDictionary<string, string> rawParameters, bool ready)
	{
		if (!ready)
			return new SeriesOutcome.Failure(IllusionistError.NotReady());

		try
		{
			var key = SeriesKeyParser.Parse(rawParameters, registry);
			return GenerateFromKey(key);
		}
		catch (IllusionistErrorException ex)
		{
			return new SeriesOutcome.Failure(ex.Error);
		}
	}

	/// <inheritdoc />
	public SeriesOutcome GenerateFromKey(SeriesKey key)
	{
		if (!registry.TryGet(key.Generator, out var version) || version is null)
			return new SeriesOutcome.Failure(IllusionistError.UnknownGeneratorVersion(key.Generator.Id, key.Generator.Version, registry.AllRefs));

		var source = version.Open(key);
		var timestamps = source.Timestamps(ServiceLimits.MaxBars);
		if (timestamps is null)
		{
			// Only reachable if a caller bypasses SeriesKeyParser with an already-oversized key
			// (e.g. GenerateFromKey called directly): SeriesKeyParser.Parse never returns one.
			return key.Extent is SeriesExtent.Range(var from, var to)
				? new SeriesOutcome.Failure(IllusionistError.TooManyBarsRange(from, to))
				: new SeriesOutcome.Failure(IllusionistError.TooManyBarsCount(((SeriesExtent.Count)key.Extent).BarCount));
		}

		var bars = new List<Bar<OHLC>>(timestamps.Count);
		for (var i = 0; i < timestamps.Count; i++)
		{
			Bar<OHLC> bar;
			try
			{
				bar = source.BarAt(timestamps[i]);
			}
			catch (OverflowException)
			{
				return new SeriesOutcome.Failure(IllusionistError.PriceOutOfRange(i, DateOnly.FromDateTime(timestamps[i])));
			}

			if (bar.Data.Open <= 0 || bar.Data.High <= 0 || bar.Data.Low <= 0 || bar.Data.Close <= 0)
				return new SeriesOutcome.Failure(IllusionistError.PriceOutOfRange(i, DateOnly.FromDateTime(timestamps[i])));

			bars.Add(bar);
		}

		var body = SeriesRenderer.Render(key, bars);
		var byteCount = Encoding.UTF8.GetByteCount(body);

		DateTime? first = bars.Count > 0 ? bars[0].Timestamp : null;
		DateTime? last = bars.Count > 0 ? bars[^1].Timestamp : null;
		OhlcSummary? summary = bars.Count > 0
			? new OhlcSummary(bars[0].Data.Open, bars.Max(b => b.Data.High), bars.Min(b => b.Data.Low), bars[^1].Data.Close)
			: null;

		var warnings = new List<string>();
		if (bars.Any(b => b.Timestamp.Year is < 2024 or > 2025))
			warnings.Add(SeriesEnvelope.CalendarScopeWarning);

		if (summary is not null)
		{
			// Plausibility, not validation: a legal key can still walk arbitrarily far from
			// anchorPrice without overflowing decimal. Warn, but never alter or reject the bytes.
			const decimal extremeRatioThreshold = 1000m;
			if (summary.High > key.AnchorPrice * extremeRatioThreshold)
				warnings.Add(SeriesEnvelope.ExtremePricesWarning(summary.High / key.AnchorPrice, aboveAnchor: true));

			if (summary.Low < key.AnchorPrice / extremeRatioThreshold)
				warnings.Add(SeriesEnvelope.ExtremePricesWarning(key.AnchorPrice / summary.Low, aboveAnchor: false));
		}

		return new SeriesOutcome.Success(new SeriesResult(key, body, byteCount, bars.Count, first, last, summary, warnings));
	}
}

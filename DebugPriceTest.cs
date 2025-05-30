using Illusionist.Core;
using Illusionist.Core.Catalog;

namespace DebugTest;

public class PriceDebug
{
    public static void Main()
    {
        const int seed = 12345;
        var interval = BarInterval.Minute(1);
        var anchor = new BarAnchor(new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc), 100.0m);
        var timestamp = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

        var factory = new GbmBarSeries.Factory("AAPL", seed);
        var series = factory.GetSeries(interval, anchor);
        var bar = series.GetBarAt(timestamp);

        Console.WriteLine($"Open: {bar.Open}");
        Console.WriteLine($"High: {bar.High}");
        Console.WriteLine($"Low: {bar.Low}");
        Console.WriteLine($"Close: {bar.Close}");
        Console.WriteLine($"Volume: {bar.Volume}");
    }
}

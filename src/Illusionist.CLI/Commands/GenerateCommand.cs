using Illusionist.Core;
using Illusionist.Core.Catalog;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Illusionist.CLI.Commands;

/// <summary>
/// Command to generate and display sample OHLCV bars for demonstration purposes.
/// </summary>
public sealed class GenerateCommand : Command<GenerateCommand.Settings>
{	public sealed class Settings : CommandSettings
	{
		[CommandOption("-s|--symbol")]
		[Description("The trading symbol to generate data for")]
		[DefaultValue("DEMO")]
		public string Symbol { get; init; } = "DEMO";

		[CommandOption("--seed")]
		[Description("The random seed for deterministic generation")]
		[DefaultValue(42)]
		public int Seed { get; init; } = 42;

		[CommandOption("-i|--interval")]
		[Description("The bar interval (e.g., 1m, 5m, 1h, 1d)")]
		[DefaultValue("1m")]
		public string Interval { get; init; } = "1m";

		[CommandOption("-f|--factory")]
		[Description("The factory type to use for bar generation")]
		[DefaultValue("gbm")]
		public string FactoryType { get; init; } = "gbm";

		[CommandOption("--drift")]
		[Description("The drift parameter for GBM (annual growth rate)")]
		[DefaultValue(0.0001)]
		public double Drift { get; init; } = 0.0001;

		[CommandOption("--volatility")]
		[Description("The volatility parameter for GBM (annual volatility)")]
		[DefaultValue(0.01)]
		public double Volatility { get; init; } = 0.01;
	}

	public override int Execute(CommandContext context, Settings settings)
	{
		AnsiConsole.MarkupLine($"[green]Generating bars for symbol:[/] [yellow]{settings.Symbol}[/]");
		AnsiConsole.MarkupLine($"[green]Seed:[/] [yellow]{settings.Seed}[/]");
		AnsiConsole.MarkupLine($"[green]Interval:[/] [yellow]{settings.Interval}[/]");		AnsiConsole.MarkupLine($"[green]Factory type:[/] [yellow]{settings.FactoryType.ToUpperInvariant()}[/]");
		AnsiConsole.WriteLine();

		var interval = ParseInterval(settings.Interval);
		var anchor = new BarAnchor(DateTime.UtcNow.Date.AddHours(9), 100.0m); // Default anchor at market open with $100 price

		// Create GBM factory
		IBarSeriesFactory factory = CreateFactory(settings);

		var series = factory.GetSeries(interval, anchor);
		var startTime = DateTime.UtcNow.Date.AddHours(9); // Market open time
		var bars = series.GetBars(startTime).Take(5).ToList();

		var table = new Table();
		table.AddColumn("Timestamp");
		table.AddColumn("Open", column => column.RightAligned());
		table.AddColumn("High", column => column.RightAligned());
		table.AddColumn("Low", column => column.RightAligned());
		table.AddColumn("Close", column => column.RightAligned());
		table.AddColumn("Volume", column => column.RightAligned());

		foreach (var bar in bars)
		{
			table.AddRow(
				bar.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
				bar.Open.ToString("F2"),
				bar.High.ToString("F2"),
				bar.Low.ToString("F2"),
				bar.Close.ToString("F2"),
				bar.Volume.ToString("N0"));
		}

		AnsiConsole.Write(table);
		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine("[dim]Note: This is demo data generated for illustration purposes.[/]");
		return 0;
	}

	private IBarSeriesFactory CreateFactory(Settings settings)
	{
		return settings.FactoryType.ToLowerInvariant() switch
		{
			"gbm" => new GbmBarSeries.Factory(settings.Symbol, settings.Seed, settings.Drift, settings.Volatility),
			_ => throw new ArgumentException($"Unsupported factory type: {settings.FactoryType}. Only 'gbm' is currently supported.")
		};
	}
	private static BarInterval ParseInterval(string interval)
	{
		return interval.ToLowerInvariant() switch
		{
			"1m" => BarInterval.Minute(1),
			"5m" => BarInterval.Minute(5),
			"15m" => BarInterval.Minute(15),
			"1h" => BarInterval.Hour(1),
			"4h" => BarInterval.Hour(4),
			"1d" => BarInterval.Day(1),
			_ => BarInterval.Minute(1)
		};
	}
}

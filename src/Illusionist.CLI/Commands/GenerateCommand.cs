using Illusionist.CLI.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Illusionist.CLI.Commands;

/// <summary>
/// Command to generate and display sample OHLCV bars for demonstration purposes.
/// </summary>
public sealed class GenerateCommand : Command<GenerateCommand.Settings>
{
	public sealed class Settings : CommandSettings
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
	}

	public override int Execute(CommandContext context, Settings settings)
	{
		AnsiConsole.MarkupLine($"[green]Generating bars for symbol:[/] [yellow]{settings.Symbol}[/]");
		AnsiConsole.MarkupLine($"[green]Seed:[/] [yellow]{settings.Seed}[/]");
		AnsiConsole.MarkupLine($"[green]Interval:[/] [yellow]{settings.Interval}[/]");
		AnsiConsole.WriteLine();

		var interval = ParseInterval(settings.Interval);
		var factory = new DemoBarSeriesFactory(settings.Seed);
		var series = factory.GetSeries(interval);

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

	private static TimeSpan ParseInterval(string interval)
	{
		return interval.ToLowerInvariant() switch
		{
			"1m" => TimeSpan.FromMinutes(1),
			"5m" => TimeSpan.FromMinutes(5),
			"15m" => TimeSpan.FromMinutes(15),
			"1h" => TimeSpan.FromHours(1),
			"4h" => TimeSpan.FromHours(4),
			"1d" => TimeSpan.FromDays(1),
			_ => TimeSpan.FromMinutes(1)
		};
	}
}

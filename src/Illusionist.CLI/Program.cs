using Illusionist.CLI.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();
app.Configure(config =>
{
	config.AddCommand<GenerateCommand>("generate")
		.WithDescription("Generate sample OHLCV bars for a symbol")
		.WithExample(["generate", "--symbol", "AAPL", "--seed", "12345", "--interval", "1m", "--factory", "gbm"]);
});

return app.Run(args);

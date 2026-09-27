using System.Globalization;
using System.Text;
using System.Text.Json;
using Illusionist.Service.Generators;
using Illusionist.Service.SelfCheck;

namespace Illusionist.Service.Series;

/// <summary>
/// Renders <c>illusionist_generators</c> and <c>illusionist_describe</c>'s JSON bodies (D3) --
/// shared verbatim between the MCP tools and their REST twins (D4 requires byte-identical text).
/// </summary>
public static class GeneratorListingRenderer
{
	/// <summary>The reproducibility key's fields, in order (the "Fixed inputs" contract at the top of the service design).</summary>
	private static readonly string[] ReproducibilityKeyFields =
	[
		"generator", "seedMode", "seed", "symbol", "drift", "volatility",
		"anchorDate", "anchorPrice", "timeframe", "count-or-range", "format",
	];

	/// <summary><c>{"generators":[…]}</c> -- every registered version's own listing entry.</summary>
	public static string RenderGenerators(IReadOnlyList<IGeneratorVersion> versions)
	{
		using var stream = new MemoryStream();
		using (var writer = new Utf8JsonWriter(stream, IllusionistJson.WriterOptions))
		{
			writer.WriteStartObject();
			writer.WriteStartArray("generators");
			foreach (var version in versions)
				WriteGeneratorEntry(writer, version);
			writer.WriteEndArray();
			writer.WriteEndObject();
		}

		return Encoding.UTF8.GetString(stream.ToArray());
	}

	/// <summary>One generator version's reproducibility contract (D3), including its self-check report.</summary>
	/// <param name="version">The version to describe.</param>
	/// <param name="selfCheckReport">The most recent (cached, never re-run here) self-check report, or <see langword="null"/> before the first run.</param>
	public static string RenderDescribe(IGeneratorVersion version, SelfCheckReport? selfCheckReport)
	{
		var describable = version as IDescribableGenerator;

		using var stream = new MemoryStream();
		using (var writer = new Utf8JsonWriter(stream, IllusionistJson.WriterOptions))
		{
			writer.WriteStartObject();
			writer.WriteString("ref", version.Ref.ToString());

			writer.WriteStartArray("key");
			foreach (var name in ReproducibilityKeyFields)
				writer.WriteStringValue(name);
			writer.WriteEndArray();

			WriteStringArray(writer, "guarantees", describable?.Guarantees ?? []);
			writer.WriteString("scope", describable?.Scope ?? string.Empty);
			WriteStringArray(writer, "limitations", describable?.Limitations ?? []);

			writer.WriteStartObject("limits");
			writer.WriteNumber("maxBars", ServiceLimits.MaxBars);
			writer.WriteNumber("inlineMaxBytes", ServiceLimits.InlineMaxBytes);
			writer.WriteStartArray("dates");
			writer.WriteStringValue(ServiceLimits.MinDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
			writer.WriteStringValue(ServiceLimits.MaxDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
			writer.WriteEndArray();
			writer.WriteEndObject();

			writer.WritePropertyName("selfCheck");
			if (selfCheckReport is not null)
				writer.WriteRawValue(selfCheckReport.ToJson(), skipInputValidation: true);
			else
				writer.WriteNullValue();

			writer.WriteEndObject();
		}

		return Encoding.UTF8.GetString(stream.ToArray());
	}

	private static void WriteGeneratorEntry(Utf8JsonWriter writer, IGeneratorVersion version)
	{
		writer.WriteStartObject();
		writer.WriteString("ref", version.Ref.ToString());
		writer.WriteString("id", version.Ref.Id);
		writer.WriteNumber("version", version.Ref.Version);
		writer.WriteString("summary", version.Descriptor.Summary);

		WriteStringArray(writer, "timeframes", version.Descriptor.Timeframes);

		writer.WriteStartArray("parameters");
		foreach (var parameter in version.Descriptor.Parameters)
			WriteParameter(writer, parameter);
		writer.WriteEndArray();

		writer.WriteStartObject("limits");
		writer.WriteNumber("maxBars", ServiceLimits.MaxBars);
		writer.WriteNumber("inlineMaxBytes", ServiceLimits.InlineMaxBytes);
		writer.WriteEndObject();

		writer.WriteEndObject();
	}

	private static void WriteParameter(Utf8JsonWriter writer, ParameterSpec parameter)
	{
		writer.WriteStartObject();
		writer.WriteString("name", parameter.Name);
		writer.WriteString("type", parameter.Type);
		WriteRawIfPresent(writer, "default", parameter.Default);
		WriteRawIfPresent(writer, "minimum", parameter.Minimum);
		WriteRawIfPresent(writer, "maximum", parameter.Maximum);

		if (parameter.Enum is not null)
			WriteStringArray(writer, "enum", parameter.Enum);

		if (parameter.Pattern is not null)
			writer.WriteString("pattern", parameter.Pattern);

		writer.WriteString("description", parameter.Description);
		writer.WriteEndObject();
	}

	private static void WriteStringArray(Utf8JsonWriter writer, string name, IReadOnlyList<string> values)
	{
		writer.WriteStartArray(name);
		foreach (var value in values)
			writer.WriteStringValue(value);
		writer.WriteEndArray();
	}

	private static void WriteRawIfPresent(Utf8JsonWriter writer, string name, string? rawJson)
	{
		if (rawJson is null)
			return;

		writer.WritePropertyName(name);
		writer.WriteRawValue(rawJson, skipInputValidation: true);
	}
}

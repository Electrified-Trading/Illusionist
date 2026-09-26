using System.Text;
using System.Text.Json;
using Illusionist.Service.Contracts;

namespace Illusionist.Service.SelfCheck;

/// <summary>This host's own architecture, operating system and .NET runtime, as reported by <see cref="System.Runtime.InteropServices.RuntimeInformation"/>.</summary>
/// <param name="Os">The operating system description.</param>
/// <param name="Architecture">The process architecture.</param>
/// <param name="Framework">The .NET runtime description.</param>
public sealed record HostInfo(string Os, string Architecture, string Framework);

/// <summary>One golden case's comparison: the fixture's expected hash against what this host actually produced.</summary>
/// <param name="Ref">The generator version this case belongs to.</param>
/// <param name="Name">The case's fixture name.</param>
/// <param name="Expected">The committed fixture's SHA-256.</param>
/// <param name="Actual">This host's SHA-256, or <c>"error: {ExceptionType}"</c> if generation threw.</param>
/// <param name="Match">Whether <paramref name="Expected"/> equals <paramref name="Actual"/>.</param>
public sealed record SelfCheckCaseResult(string Ref, string Name, string Expected, string Actual, bool Match);

/// <summary>
/// The result of running every registered generator's golden cases against this host (D8). A
/// process either passes every case or it is not ready -- there is no partial credit.
/// </summary>
/// <param name="Host">This host's own architecture, OS and runtime.</param>
/// <param name="Cases">Every case compared, in registry order.</param>
public sealed record SelfCheckReport(HostInfo Host, IReadOnlyList<SelfCheckCaseResult> Cases)
{
	/// <summary>Whether every case matched.</summary>
	public bool IsReady => Cases.Count > 0 && Cases.All(c => c.Match);

	/// <summary>Serializes as <c>{"status":…,"host":{…},"cases":[…]}</c>.</summary>
	public string ToJson()
	{
		using var stream = new MemoryStream();
		using (var writer = new Utf8JsonWriter(stream, IllusionistJson.WriterOptions))
		{
			writer.WriteStartObject();
			writer.WriteString("status", IsReady ? "ready" : "not-ready");

			writer.WriteStartObject("host");
			writer.WriteString("os", Host.Os);
			writer.WriteString("architecture", Host.Architecture);
			writer.WriteString("framework", Host.Framework);
			writer.WriteEndObject();

			writer.WriteStartArray("cases");
			foreach (var @case in Cases)
			{
				writer.WriteStartObject();
				writer.WriteString("ref", @case.Ref);
				writer.WriteString("name", @case.Name);
				writer.WriteString("expected", @case.Expected);
				writer.WriteString("actual", @case.Actual);
				writer.WriteBoolean("match", @case.Match);
				writer.WriteEndObject();
			}

			writer.WriteEndArray();
			writer.WriteEndObject();
		}

		return Encoding.UTF8.GetString(stream.ToArray());
	}
}

using System.Text;
using System.Text.Json;
using Illusionist.Service.Contracts;

namespace Illusionist.Service.SelfCheck;

/// <summary>This host's own architecture, operating system and .NET runtime, as reported by <see cref="System.Runtime.InteropServices.RuntimeInformation"/>.</summary>
/// <param name="Os">The operating system description.</param>
/// <param name="Architecture">The process architecture.</param>
/// <param name="Framework">The .NET runtime description.</param>
/// <param name="Platform">This host's <see cref="Illusionist.Service.SelfCheck.HostPlatform"/>-formatted identifier (e.g. <c>windows-x64</c>), the value a generator version's own <see cref="Illusionist.Service.Generators.IGeneratorVersion.ReferencePlatform"/> is compared against.</param>
public sealed record HostInfo(string Os, string Architecture, string Framework, string Platform);

/// <summary>
/// One golden case's comparison: the fixture's expected hash against what this host actually
/// produced.
/// </summary>
/// <param name="Ref">The generator version this case belongs to.</param>
/// <param name="Name">The case's fixture name.</param>
/// <param name="Expected">The committed fixture's SHA-256.</param>
/// <param name="Actual">This host's SHA-256, or <c>"error: {ExceptionType}"</c> if generation threw.</param>
/// <param name="Match">Whether <paramref name="Expected"/> equals <paramref name="Actual"/>.</param>
/// <param name="ReferencePlatform">The owning version's declared reference platform, or <see langword="null"/> when it has none (proven byte-identical everywhere by construction).</param>
/// <param name="CountsTowardReadiness">
/// Whether <paramref name="Match"/> gates <see cref="SelfCheckReport.IsReady"/>: <see langword="true"/>
/// when <paramref name="ReferencePlatform"/> is <see langword="null"/> or equals this host's own
/// platform, <see langword="false"/> when a version has declared it is not expected to reproduce
/// here. A <see langword="false"/> case still reports its real <paramref name="Match"/> honestly --
/// this field says whether that honesty should also fail the process, not whether the comparison
/// itself was skipped.
/// </param>
public sealed record SelfCheckCaseResult(
	string Ref,
	string Name,
	string Expected,
	string Actual,
	bool Match,
	string? ReferencePlatform,
	bool CountsTowardReadiness);

/// <summary>
/// The result of running every registered generator's golden cases against this host (D8). Among
/// the cases that count toward readiness (<see cref="SelfCheckCaseResult.CountsTowardReadiness"/>),
/// a process either passes every one or it is not ready -- there is no partial credit. A case that
/// does not count (its own version has declared this is not its reference platform) is still
/// reported, but never blocks readiness by itself.
/// </summary>
/// <param name="Host">This host's own architecture, OS, runtime and platform identifier.</param>
/// <param name="Cases">Every case compared, in registry order.</param>
public sealed record SelfCheckReport(HostInfo Host, IReadOnlyList<SelfCheckCaseResult> Cases)
{
	/// <summary>Whether every case that counts toward readiness matched (see <see cref="SelfCheckCaseResult.CountsTowardReadiness"/>). <see langword="false"/> when there are no such cases at all.</summary>
	public bool IsReady
	{
		get
		{
			var gating = Cases.Where(c => c.CountsTowardReadiness).ToList();
			return gating.Count > 0 && gating.All(c => c.Match);
		}
	}

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
			writer.WriteString("platform", Host.Platform);
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
				if (@case.ReferencePlatform is { } referencePlatform)
					writer.WriteString("referencePlatform", referencePlatform);
				else
					writer.WriteNull("referencePlatform");
				writer.WriteBoolean("countsTowardReadiness", @case.CountsTowardReadiness);
				writer.WriteEndObject();
			}

			writer.WriteEndArray();
			writer.WriteEndObject();
		}

		return Encoding.UTF8.GetString(stream.ToArray());
	}
}

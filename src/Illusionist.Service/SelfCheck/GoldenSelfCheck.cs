using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Illusionist.Service.Generators;
using Illusionist.Service.Series;

namespace Illusionist.Service.SelfCheck;

/// <summary>
/// For every registered version and every readiness case (<see cref="IGeneratorVersion.ReadinessCases"/>),
/// renders the case's key through the same <see cref="ISeriesService"/> generate-and-render path
/// the tool and REST use (bypassing only the readiness gate, via
/// <see cref="ISeriesService.GenerateFromKey"/>) and compares its SHA-256 against the case's own
/// pinned hash.
/// </summary>
public sealed class GoldenSelfCheck(IGeneratorRegistry registry, ISeriesService seriesService) : IGoldenSelfCheck
{
	/// <inheritdoc />
	public SelfCheckReport Run()
	{
		var results = new List<SelfCheckCaseResult>();

		foreach (var version in registry.All)
		{
			// A version with a declared reference platform is only held to its golden cases there;
			// elsewhere, a mismatch is reported honestly but never blocks readiness (see
			// IGeneratorVersion.ReferencePlatform and SelfCheckCaseResult.CountsTowardReadiness).
			var countsTowardReadiness = version.ReferencePlatform is null || version.ReferencePlatform == HostPlatform.Current;

			foreach (var goldenCase in version.ReadinessCases)
			{
				var actual = RunCase(version, goldenCase);
				results.Add(new SelfCheckCaseResult(
					version.Ref.ToString(),
					goldenCase.Name,
					goldenCase.Sha256,
					actual,
					string.Equals(actual, goldenCase.Sha256, StringComparison.Ordinal),
					version.ReferencePlatform,
					countsTowardReadiness));
			}
		}

		var host = new HostInfo(
			RuntimeInformation.OSDescription,
			RuntimeInformation.ProcessArchitecture.ToString(),
			RuntimeInformation.FrameworkDescription,
			HostPlatform.Current);

		return new SelfCheckReport(host, results);
	}

	private string RunCase(IGeneratorVersion version, GoldenCase goldenCase)
	{
		try
		{
			var key = goldenCase.ToKey(version.Ref);
			return seriesService.GenerateFromKey(key) switch
			{
				SeriesOutcome.Success success => ComputeSha256(success.Result.Body),
				SeriesOutcome.Failure failure => $"error: {failure.Error.Code}",
				var other => $"error: unrecognized outcome {other.GetType().Name}",
			};
		}
		catch (Exception ex)
		{
			return $"error: {ex.GetType().Name}";
		}
	}

	private static string ComputeSha256(string body)
		=> Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(body)));
}

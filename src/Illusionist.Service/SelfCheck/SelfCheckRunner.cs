using Microsoft.Extensions.Logging;

namespace Illusionist.Service.SelfCheck;

using Illusionist.Service;

/// <summary>
/// Runs the golden self-check, publishes the report to <see cref="SelfCheckState"/>, and logs the
/// outcome (D11) -- shared by the startup gate and every <c>/healthz</c> re-run so both paths log
/// identically.
/// </summary>
public static class SelfCheckRunner
{
	/// <summary>Runs, publishes and logs. Returns the report.</summary>
	public static SelfCheckReport RunAndPublish(IGoldenSelfCheck selfCheck, SelfCheckState state, ILogger logger)
	{
		var report = selfCheck.Run();
		state.Update(report);

		if (report.IsReady)
		{
			Log.SelfCheckPassed(logger, report.Cases.Count, report.Host.Architecture, report.Host.Framework, report.Host.Os);
			return report;
		}

		foreach (var @case in report.Cases.Where(c => !c.Match))
			Log.SelfCheckCaseFailed(logger, @case.Ref, @case.Name, @case.Expected, @case.Actual);

		Log.SelfCheckFailed(
			logger,
			report.Cases.Count(c => !c.Match),
			report.Cases.Count,
			report.Host.Architecture,
			report.Host.Framework,
			report.Host.Os);

		return report;
	}
}

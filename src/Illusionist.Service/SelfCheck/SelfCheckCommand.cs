namespace Illusionist.Service.SelfCheck;

/// <summary>
/// <c>dotnet Illusionist.Service.dll --self-check</c>: runs the golden check without starting the
/// web host, prints the report JSON plus LF to <paramref name="output"/>, and returns Ops's
/// one-line proof for their host -- 0 on pass, 1 on fail.
/// </summary>
public sealed class SelfCheckCommand(IGoldenSelfCheck selfCheck)
{
	/// <summary>Runs the check and writes its report. Returns the process exit code.</summary>
	public int Run(TextWriter output)
	{
		var report = selfCheck.Run();
		output.Write(report.ToJson());
		output.Write('\n');

		return report.IsReady ? 0 : 1;
	}
}

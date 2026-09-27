namespace Illusionist.Service.SelfCheck;

/// <summary>
/// The process-wide, most recent <see cref="SelfCheckReport"/>: a volatile reference swap, updated
/// at startup and on every <c>/healthz</c> call. No lock is needed -- readers only ever observe a
/// complete, immutable report or none yet.
/// </summary>
public sealed class SelfCheckState
{
	private SelfCheckReport? _current;

	/// <summary>The most recently completed report, or <see langword="null"/> before the first run.</summary>
	public SelfCheckReport? Current => Volatile.Read(ref _current);

	/// <summary>Whether the most recent report passed. <see langword="false"/> before the first run.</summary>
	public bool IsReady => Current?.IsReady ?? false;

	/// <summary>Publishes a newly completed report.</summary>
	public void Update(SelfCheckReport report)
		=> Volatile.Write(ref _current, report);
}

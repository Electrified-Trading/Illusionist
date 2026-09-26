namespace Illusionist.Service.SelfCheck;

/// <summary>
/// Proves -- rather than assumes -- that this host reproduces every registered generator's golden
/// fixtures byte for byte. Run at startup (gating readiness) and on every <c>/healthz</c> call
/// (re-run, so tier-1 JIT code is exercised too, not just the cold startup path).
/// </summary>
public interface IGoldenSelfCheck
{
	/// <summary>Runs every registered generator's golden cases through the real generate-and-render path and compares hashes.</summary>
	SelfCheckReport Run();
}

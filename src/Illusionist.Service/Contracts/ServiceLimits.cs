namespace Illusionist.Service.Contracts;

/// <summary>The fixed limits every surface enforces identically. Constants, not configuration.</summary>
public static class ServiceLimits
{
	/// <summary>The most bars any single call may return, in count mode or range mode.</summary>
	public const int MaxBars = 20_000;

	/// <summary>The body-size threshold, in UTF-8 bytes, under which a series result comes back inline.</summary>
	public const int InlineMaxBytes = 16_384;

	/// <summary>The earliest date any request may reference.</summary>
	public static readonly DateOnly MinDate = new(1900, 1, 1);

	/// <summary>The latest date any request may reference.</summary>
	public static readonly DateOnly MaxDate = new(2199, 12, 31);

	/// <summary>The longest a <c>symbol</c> value may be.</summary>
	public const int MaxSymbolLength = 32;
}

using Microsoft.Extensions.Logging;

namespace Illusionist.Service;

/// <summary>The service's structured log events (D11), one JSON object per line on stdout.</summary>
internal static partial class Log
{
	[LoggerMessage(EventId = 1, Level = LogLevel.Information,
		Message = "Golden self-check passed: {Cases} cases on {Architecture}/{Framework} ({Os}).")]
	public static partial void SelfCheckPassed(ILogger logger, int cases, string architecture, string framework, string os);

	[LoggerMessage(EventId = 2, Level = LogLevel.Critical,
		Message = "Golden self-check case failed: {Ref} '{Case}' expected {Expected} got {Actual}.")]
	public static partial void SelfCheckCaseFailed(ILogger logger, string @ref, string @case, string expected, string actual);

	[LoggerMessage(EventId = 3, Level = LogLevel.Critical,
		Message = "Golden self-check failed: {Failed}/{Total} cases on {Architecture}/{Framework} ({Os}).")]
	public static partial void SelfCheckFailed(ILogger logger, int failed, int total, string architecture, string framework, string os);

	[LoggerMessage(EventId = 10, Level = LogLevel.Information,
		Message = "Series served via {Surface}: {Uri} ({Bars} bars, {Bytes} bytes, inline={Inline}, {ElapsedMs}ms).")]
	public static partial void SeriesServed(ILogger logger, string surface, string uri, int bars, int bytes, bool inline, long elapsedMs);

	[LoggerMessage(EventId = 11, Level = LogLevel.Information,
		Message = "Request rejected via {Surface}: {Code} ({Parameter}).")]
	public static partial void RequestRejected(ILogger logger, string surface, string code, string? parameter);

	[LoggerMessage(EventId = 20, Level = LogLevel.Error,
		Message = "Unexpected error via {Surface}.")]
	public static partial void UnexpectedError(ILogger logger, string surface, Exception exception);
}

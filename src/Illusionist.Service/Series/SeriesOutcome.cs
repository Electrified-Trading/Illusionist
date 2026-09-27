namespace Illusionist.Service.Series;

/// <summary>
/// <see cref="ISeriesService"/>'s return shape: a generated <see cref="SeriesResult"/> or an
/// <see cref="IllusionistError"/>, never an exception -- so no surface (MCP tool, MCP resource,
/// REST) ever needs to catch anything for an ordinary input problem (D5: "Tool code never throws
/// for input problems").
/// </summary>
public abstract record SeriesOutcome
{
	private SeriesOutcome()
	{
	}

	/// <summary>Generation and rendering succeeded.</summary>
	public sealed record Success(SeriesResult Result) : SeriesOutcome;

	/// <summary>Something failed validation or generation; report <see cref="Error"/> as-is.</summary>
	public sealed record Failure(IllusionistError Error) : SeriesOutcome;
}

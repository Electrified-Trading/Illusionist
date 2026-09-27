namespace Illusionist.Service.Contracts;

/// <summary>
/// How many bars a series request spans: either a bar <see cref="Count"/> starting at the anchor,
/// or an inclusive date <see cref="Range"/>. A request carries exactly one of these -- never both,
/// never neither (see <c>SeriesKeyParser</c> for the <c>conflicting_parameters</c>/<c>missing_parameter</c>
/// checks that enforce it).
/// </summary>
public abstract record SeriesExtent
{
	private SeriesExtent()
	{
	}

	/// <summary>A number of bars starting at the anchor.</summary>
	/// <param name="BarCount">The number of bars, in [1, <see cref="ServiceLimits.MaxBars"/>].</param>
	public sealed record Count(int BarCount) : SeriesExtent;

	/// <summary>An inclusive date range, before or after the anchor.</summary>
	/// <param name="From">The first date of the range (inclusive).</param>
	/// <param name="To">The last date of the range (inclusive), not before <paramref name="From"/>.</param>
	public sealed record Range(DateOnly From, DateOnly To) : SeriesExtent;
}

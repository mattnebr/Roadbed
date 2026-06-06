namespace Roadbed.Logging;

/// <summary>
/// Lifecycle states an activity row can occupy.
/// </summary>
/// <remarks>
/// The string forms (lowercase enum name) match the values used by the
/// <c>activity.status</c> ENUM column in the canonical MySQL schema. SQLite
/// stores them as TEXT with a CHECK constraint that mirrors the same set.
/// </remarks>
public enum LoggingActivityStatus
{
    /// <summary>
    /// The activity row has been created but its run has not yet begun.
    /// </summary>
    /// <remarks>
    /// Reserved for callers that pre-stage rows; the activity service's
    /// <c>BeginAsync</c> inserts directly as <see cref="Running"/>.
    /// </remarks>
    Pending = 0,

    /// <summary>
    /// The run is in progress.
    /// </summary>
    Running = 1,

    /// <summary>
    /// The run finished without error.
    /// </summary>
    Succeeded = 2,

    /// <summary>
    /// The run threw an exception or otherwise failed.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// The run was canceled (typically via <see cref="System.Threading.CancellationToken"/>).
    /// </summary>
    Canceled = 4,

    /// <summary>
    /// The run was skipped before it began (e.g. a precondition was not met).
    /// </summary>
    Skipped = 5,
}

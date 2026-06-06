namespace Roadbed.Logging;

using System;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Represents a row in the <c>activity_input</c> table — one edge in the
/// lineage DAG indicating that a downstream activity consumed the output of
/// an upstream activity.
/// </summary>
/// <remarks>
/// The composite primary key is (<see cref="ActivityId"/>, <see cref="InputActivityId"/>);
/// duplicate edges are silently coalesced by the database.
/// </remarks>
public sealed class LoggingActivityInput
{
    #region Public Properties

    /// <summary>
    /// Gets or sets the consuming activity's identifier.
    /// </summary>
    [Column("activity_id")]
    public string ActivityId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the upstream input activity's identifier.
    /// </summary>
    [Column("input_activity_id")]
    public string InputActivityId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional role describing what the consumer used the input for.
    /// </summary>
    /// <remarks>
    /// Free-form string. Examples: <c>"places"</c>, <c>"cousubs"</c>,
    /// <c>"hud-centroid"</c>.
    /// </remarks>
    [Column("input_role")]
    public string? InputRole { get; set; }

    /// <summary>
    /// Gets or sets the moment this lineage edge was recorded (UTC).
    /// </summary>
    [Column("created_on")]
    public DateTime CreatedOn { get; set; }

    #endregion Public Properties
}

namespace Roadbed.Logging;

/// <summary>
/// Well-known categories of activity used by consuming applications.
/// </summary>
/// <remarks>
/// Persisted to <c>activity.activity_type</c> as a lowercase string. Custom
/// values may be supplied via <see cref="LoggingActivityBeginRequest.ActivityType"/>
/// when no enum member fits; the column is a VARCHAR, not an ENUM.
/// </remarks>
public enum LoggingActivityType
{
    /// <summary>
    /// Unspecified activity type. Persisted as the literal string <c>unknown</c>.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// A Bronze-tier ingestion run (raw load from an upstream source).
    /// </summary>
    Ingestion = 1,

    /// <summary>
    /// A Silver-tier transformation run (cleansing, conforming, joining).
    /// </summary>
    Transformation = 2,

    /// <summary>
    /// A Gold-tier promotion run (publishing curated data downstream).
    /// </summary>
    Promotion = 3,

    /// <summary>
    /// A maintenance run (partition pruning, vacuum, reindex, etc.).
    /// </summary>
    Maintenance = 4,

    /// <summary>
    /// A run kicked off interactively by an operator.
    /// </summary>
    Manual = 5,

    /// <summary>
    /// A run that does not fit any predefined category.
    /// </summary>
    Custom = 6,
}

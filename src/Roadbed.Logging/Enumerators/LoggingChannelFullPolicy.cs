namespace Roadbed.Logging;

/// <summary>
/// Behavior the in-process log channel applies when its bounded capacity is reached.
/// </summary>
public enum LoggingChannelFullPolicy
{
    /// <summary>
    /// Drop the oldest queued entry to make room for the newest. The default;
    /// preserves recent context and never blocks the calling thread.
    /// </summary>
    /// <remarks>
    /// Dropped entries are counted and surfaced via a periodic <c>Warning</c>
    /// log written to the recursion-safe fallback sink.
    /// </remarks>
    DropOldest = 0,

    /// <summary>
    /// Block the writing thread briefly (capped by the channel's write timeout)
    /// to apply backpressure on log producers.
    /// </summary>
    /// <remarks>
    /// Useful when log fidelity matters more than throughput. Avoid on hot
    /// paths that cannot tolerate even short blocking waits.
    /// </remarks>
    BlockBriefly = 1,
}

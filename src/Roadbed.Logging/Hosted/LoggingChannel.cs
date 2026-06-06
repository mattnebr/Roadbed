namespace Roadbed.Logging;

using System;
using System.Threading;
using System.Threading.Channels;

/// <summary>
/// Bounded in-process queue that decouples the OTel exporter (producer)
/// from the background batch writer (consumer).
/// </summary>
/// <remarks>
/// <para>
/// The channel is a singleton in the DI container; both sides of the
/// pipeline resolve the same instance.
/// </para>
/// <para>
/// When <see cref="LoggingChannelFullPolicy.DropOldest"/> is configured the
/// channel silently drops the oldest queued entry to make room. When
/// <see cref="LoggingChannelFullPolicy.BlockBriefly"/> is configured the
/// producer's <see cref="TryWrite"/> returns <c>false</c> on contention and
/// the wrapper increments a counter that the writer surfaces as a periodic
/// warning.
/// </para>
/// </remarks>
internal sealed class LoggingChannel
{
    #region Private Fields

    private readonly Channel<LoggingLogEntry> _channel;
    private long _droppedCount;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingChannel"/> class.
    /// </summary>
    /// <param name="options">Host-supplied logging options.</param>
    public LoggingChannel(LoggingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        BoundedChannelFullMode fullMode = options.ChannelFullPolicy switch
        {
            LoggingChannelFullPolicy.DropOldest => BoundedChannelFullMode.DropOldest,
            LoggingChannelFullPolicy.BlockBriefly => BoundedChannelFullMode.Wait,
            _ => BoundedChannelFullMode.DropOldest,
        };

        this._channel = Channel.CreateBounded<LoggingLogEntry>(
            new BoundedChannelOptions(Math.Max(1, options.ChannelCapacity))
            {
                FullMode = fullMode,
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false,
            });
    }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>
    /// Gets the channel reader that the background writer drains.
    /// </summary>
    public ChannelReader<LoggingLogEntry> Reader => this._channel.Reader;

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// Attempts to enqueue an entry without blocking the caller.
    /// </summary>
    /// <param name="entry">The entry to enqueue.</param>
    /// <returns>
    /// <c>true</c> when the entry was accepted by the channel. With
    /// <see cref="LoggingChannelFullPolicy.DropOldest"/> this is effectively
    /// always <c>true</c>; with
    /// <see cref="LoggingChannelFullPolicy.BlockBriefly"/> this returns
    /// <c>false</c> on contention and increments the dropped counter.
    /// </returns>
    public bool TryWrite(LoggingLogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        bool accepted = this._channel.Writer.TryWrite(entry);

        if (!accepted)
        {
            Interlocked.Increment(ref this._droppedCount);
        }

        return accepted;
    }

    /// <summary>
    /// Reads the dropped-entry counter and resets it to zero atomically.
    /// </summary>
    /// <returns>The number of entries the channel rejected since the previous call.</returns>
    /// <remarks>
    /// Only meaningful when the policy is
    /// <see cref="LoggingChannelFullPolicy.BlockBriefly"/>; the
    /// <see cref="LoggingChannelFullPolicy.DropOldest"/> path drops
    /// entries silently inside the channel and the counter remains zero.
    /// </remarks>
    public long ConsumeDroppedCount()
    {
        return Interlocked.Exchange(ref this._droppedCount, 0);
    }

    /// <summary>
    /// Marks the writer side complete so the background drainer can finish.
    /// </summary>
    public void CompleteWriter()
    {
        this._channel.Writer.TryComplete();
    }

    #endregion Public Methods
}

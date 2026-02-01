namespace Roadbed.Scheduling;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;

/// <summary>
/// Abstract base class for scheduled jobs providing common functionality.
/// </summary>
/// <typeparam name="T">Class inheriting from BaseSchedulingJob.</typeparam>
/// <remarks>
/// This base class provides two initialization patterns:
/// 1. Constructor injection: Pass name, description, and schedule to base constructor
/// 2. Property override: Override Name, Description, and Schedule properties in derived class
///
/// The Context property is available during job execution and can be used to set result messages
/// via Context.Result, which will be captured by the metrics system.
/// </remarks>
public abstract class BaseSchedulingJob<T>
    : BaseClassWithLoggingFactory<T>, ISchedulingJob
{
    #region Private Fields

    private readonly string? _description;
    private readonly string? _name;
    private readonly SchedulingSchedule? _schedule;
    private IJobExecutionContext? _currentContext;

    #endregion Private Fields

    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseSchedulingJob{T}"/> class.
    /// </summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    /// <remarks>
    /// When using this constructor, derived classes must override Name, Description, and Schedule properties.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    protected BaseSchedulingJob(ILogger logger)
        : base(logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseSchedulingJob{T}"/> class.
    /// </summary>
    /// <param name="name">The unique name for this job.</param>
    /// <param name="description">A description of what this job does.</param>
    /// <param name="schedule">The schedule configuration for this job.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    /// <remarks>
    /// When using this constructor, Name, Description, and Schedule are set from constructor parameters.
    /// Derived classes should NOT override these properties.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when name or description is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when schedule or logger is null.</exception>
    protected BaseSchedulingJob(
        string name,
        string description,
        SchedulingSchedule schedule,
        ILogger logger)
        : base(logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(schedule);

        this._name = name;
        this._description = description;
        this._schedule = schedule;
    }

    #endregion Protected Constructors

    #region Public Properties

    /// <inheritdoc/>
    public virtual string Description
    {
        get
        {
            if (this._description != null)
            {
                return this._description;
            }

            throw new InvalidOperationException(
                $"Description must be provided either through constructor or by overriding the Description property in {this.GetType().Name}.");
        }
    }

    /// <inheritdoc/>
    public virtual string Name
    {
        get
        {
            if (this._name != null)
            {
                return this._name;
            }

            throw new InvalidOperationException(
                $"Name must be provided either through constructor or by overriding the Name property in {this.GetType().Name}.");
        }
    }

    /// <inheritdoc/>
    public virtual SchedulingSchedule Schedule
    {
        get
        {
            if (this._schedule != null)
            {
                return this._schedule;
            }

            throw new InvalidOperationException(
                $"Schedule must be provided either through constructor or by overriding the Schedule property in {this.GetType().Name}.");
        }
    }

    #endregion Public Properties

    #region Protected Properties

    /// <summary>
    /// Gets the current job execution context.
    /// </summary>
    /// <remarks>
    /// Available during job execution. Use to set result message or access execution metadata.
    /// Example: this.Context.Result = "Processed 1,234 records".
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when accessed outside of ExecuteAsync method.</exception>
    protected IJobExecutionContext Context
    {
        get
        {
            if (this._currentContext == null)
            {
                throw new InvalidOperationException(
                    "Context is only available during job execution (within ExecuteAsync method).");
            }

            return this._currentContext;
        }
    }

    #endregion Protected Properties

    #region Public Methods

    /// <summary>
    /// Quartz.NET job execution entry point.
    /// </summary>
    /// <param name="context">Quartz job execution context.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This method implements IJob.Execute explicitly to bridge between Quartz.NET and the
    /// user-facing ExecuteAsync method. It manages the Context property lifecycle, ensuring
    /// it is available during ExecuteAsync execution and cleared afterwards.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
    async Task IJob.Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Store context for duration of execution
        this._currentContext = context;

        try
        {
            // Call the user-implemented ExecuteAsync
            await this.ExecuteAsync(context.CancellationToken);
        }
        finally
        {
            // Clear context after execution (prevents access outside ExecuteAsync)
            this._currentContext = null;
        }
    }

    /// <inheritdoc/>
    public abstract Task ExecuteAsync(CancellationToken cancellationToken);

    #endregion Public Methods
}
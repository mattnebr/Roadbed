namespace Roadbed.Crud;

using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

/// <summary>
/// Base Entity implementation as a Class.
/// </summary>
/// <typeparam name="TId">Data type for the ID.</typeparam>
/// <remarks>
/// This class is marked as abstract to prevent direct instantiation.
/// </remarks>
[Serializable]
public abstract record BaseEntityClass<TId>
    : IEntity<TId>
{
    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseEntityClass{TId}"/> class.
    /// </summary>
    protected BaseEntityClass()
    {
    }

    #endregion Protected Constructors

    #region Public Properties

    /// <inheritdoc />
    [Column("id")]
    [JsonProperty("id")]
    public virtual TId? Id
    {
        get;
        set;
    }

    #endregion Public Properties
}
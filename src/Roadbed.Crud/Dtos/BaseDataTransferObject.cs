/*
 * The namespace Roadbed.Crud.Entities was removed on purpose and replaced with Roadbed.Crud so that no additional using statements are required.
 */
namespace Roadbed.Crud;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

/// <summary>
/// Base Data Transfer Object (DTO) implementation.
/// </summary>
/// <typeparam name="TId">Data type for the ID.</typeparam>
[Serializable]
public abstract record BaseDataTransferObject<TId>
    : IDataTransferObject<TId>
{
    #region Protected Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseDataTransferObject{TId}"/> class.
    /// </summary>
    protected BaseDataTransferObject()
    {
    }

    #endregion Protected Constructors

    #region Public Properties

    /// <inheritdoc />
    public List<string>? Errors
    {
        get;
        internal set;
    }

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
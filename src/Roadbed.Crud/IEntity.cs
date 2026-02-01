namespace Roadbed.Crud;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Data Transfer Object (DTO) Entity With An ID.
/// </summary>
/// <typeparam name="TId">Data type for the ID.</typeparam>
[SuppressMessage(
    "Major Code Smell",
    "S3246:Generic type parameters should be co/contravariant when possible",
    Justification = "Variance provides no practical benefit in this architecture.")]
public interface IEntity<TId>
{
    /// <summary>
    /// Gets the ID of the Data Transfer Object (DTO) entity.
    /// </summary>
    TId? Id
    {
        get;
    }
}
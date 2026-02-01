namespace Roadbed.Crud;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Entity contract for the Create, Retrieve, Update, Delete, and List operations.
/// </summary>
/// <typeparam name="TDtoType">Type of Data Transfer Object (DTO) object.</typeparam>
/// <typeparam name="TIdType">Data type for the ID.</typeparam>
/// <remarks>
/// This is marker interface for all repositories. You can use it to locate implementations
/// with reflection for unit testing or other purposes.
/// </remarks>
[SuppressMessage(
    "Major Code Smell",
    "S2326:Unused type parameters should be removed",
    Justification = "Keeoping to remain consistant with other operations.")]
public interface IRepository<TDtoType, TIdType>
        where TDtoType : IEntity<TIdType>
{
}
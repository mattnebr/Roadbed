/*
 * The namespace Roadbed.Crud.Repositories was removed on purpose and replaced with Roadbed.Crud so that no additional using statements are required.
 */
namespace Roadbed.Crud;

/// <summary>
/// Entity contract for the Create, Retrieve, Update, Delete, and List operations.
/// </summary>
/// <typeparam name="TDtoType">Type of Data Transfer Object (DTO) object.</typeparam>
/// <typeparam name="TIdType">Data type for the ID.</typeparam>
public interface IBaseRepositoryWithCrudl<TDtoType, TIdType>
        : IBaseRepositoryWithCrud<TDtoType, TIdType>,
            IBaseRepositoryWithListOnly<TDtoType, TIdType>
        where TDtoType : IDataTransferObject<TIdType>
{
}
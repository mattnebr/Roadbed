namespace Roadbed.Test.Integration.Net.Mocks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Roadbed.Crud.Services.Async;

/// <summary>
/// Mock Integration Object entity for integration testing.
/// </summary>
internal class IntegrationObject
    : BaseAsyncCrudlService<IntegrationObjectRow, string>
{
    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationObject"/> class.
    /// </summary>
    public IntegrationObject()
        : base(
            new IntegrationObjectRepository(
                NullLogger<IntegrationObjectRepository>.Instance),
            NullLogger<IntegrationObject>.Instance)
    {
    }

    #endregion Public Constructors
}
namespace Roadbed.Test.Unit.Crud.Mocks;

using Roadbed.Crud;

/// <summary>
/// Mock DTO for testing.
/// </summary>
public class MockDto
    : IEntity<int>
{
    #region Public Properties

    /// <summary>
    /// Gets or sets the ID.
    /// </summary>
    public virtual int Id
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the Name.
    /// </summary>
    public string? Name { get; set; }

    #endregion Public Properties
}
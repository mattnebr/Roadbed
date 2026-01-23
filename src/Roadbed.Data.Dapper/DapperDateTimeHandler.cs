namespace Roadbed.Data;

using System;
using System.Data;
using System.Globalization;
using Dapper;

/// <summary>
/// Dapper type handler for converting SQLite TEXT to DateTime.
/// </summary>
/// <remarks>
/// <para>
/// SQLite stores DateTime as TEXT in ISO 8601 format.
/// All DateTime values are treated as UTC to maintain consistency.
/// </para>
/// <para>
/// Example stored format: "2024-01-15 14:30:00".
/// </para>
/// </remarks>
public class DapperDateTimeHandler : SqlMapper.TypeHandler<DateTime>
{
    /// <summary>
    /// Parses a database value into a DateTime.
    /// </summary>
    /// <param name="value">The value from the database (TEXT or DateTime).</param>
    /// <returns>A UTC DateTime.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the value cannot be converted to DateTime.</exception>
    public override DateTime Parse(object value)
    {
        if (value is string textValue)
        {
            // Parse as UTC and ensure Kind is set to UTC
            DateTime parsed = DateTime.Parse(textValue, CultureInfo.InvariantCulture);
            return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
        }

        if (value is DateTime dateTime)
        {
            // Ensure returned DateTime is UTC
            return dateTime.Kind == DateTimeKind.Utc
                ? dateTime
                : dateTime.ToUniversalTime();
        }

        throw new InvalidOperationException($"Cannot convert {value?.GetType()} to DateTime");
    }

    /// <summary>
    /// Sets a DateTime value as a database parameter.
    /// </summary>
    /// <param name="parameter">The database parameter to set.</param>
    /// <param name="value">The DateTime value to store (will be converted to UTC).</param>
    /// <remarks>
    /// Non-UTC DateTime values are automatically converted to UTC before storage.
    /// </remarks>
    public override void SetValue(IDbDataParameter parameter, DateTime value)
    {
        // Convert to UTC before storing
        DateTime utcValue = value.Kind == DateTimeKind.Utc
            ? value
            : value.ToUniversalTime();

        parameter.Value = utcValue.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        parameter.DbType = DbType.String;
    }
}
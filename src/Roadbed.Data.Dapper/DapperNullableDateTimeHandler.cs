namespace Roadbed.Data;

using System;
using System.Data;
using System.Globalization;
using Dapper;

/// <summary>
/// Dapper type handler for converting SQLite TEXT to nullable DateTime.
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
public class DapperNullableDateTimeHandler : SqlMapper.TypeHandler<DateTime?>
{
    /// <summary>
    /// Parses a database value into a nullable DateTime.
    /// </summary>
    /// <param name="value">The value from the database (TEXT, DateTime, or NULL).</param>
    /// <returns>A UTC DateTime if the value is valid, otherwise null.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the value cannot be converted to DateTime.</exception>
    public override DateTime? Parse(object value)
    {
        if (value == null || value is DBNull)
        {
            return null;
        }

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

        throw new InvalidOperationException($"Cannot convert {value?.GetType()} to DateTime?");
    }

    /// <summary>
    /// Sets a nullable DateTime value as a database parameter.
    /// </summary>
    /// <param name="parameter">The database parameter to set.</param>
    /// <param name="value">The nullable DateTime value to store (will be converted to UTC).</param>
    /// <remarks>
    /// Non-UTC DateTime values are automatically converted to UTC before storage.
    /// Null values are stored as DBNull.
    /// </remarks>
    public override void SetValue(IDbDataParameter parameter, DateTime? value)
    {
        if (value.HasValue)
        {
            // Convert to UTC before storing
            DateTime utcValue = value.Value.Kind == DateTimeKind.Utc
                ? value.Value
                : value.Value.ToUniversalTime();

            parameter.Value = utcValue.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            parameter.DbType = DbType.String;
        }
        else
        {
            parameter.Value = DBNull.Value;
        }
    }
}
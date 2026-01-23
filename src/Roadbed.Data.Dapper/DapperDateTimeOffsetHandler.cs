namespace Roadbed.Data;

using System;
using System.Data;
using System.Globalization;
using Dapper;

/// <summary>
/// Dapper type handler for converting SQLite TEXT to DateTimeOffset.
/// </summary>
/// <remarks>
/// <para>
/// SQLite stores DateTimeOffset as TEXT in ISO 8601 format with timezone offset.
/// This preserves the original timezone information, making it ideal for user-specific
/// times, appointments, and events across timezones.
/// </para>
/// <para>
/// Example stored format: "2024-01-15 14:30:00-06:00" or "2024-01-15T14:30:00-06:00".
/// </para>
/// </remarks>
public class DapperDateTimeOffsetHandler : SqlMapper.TypeHandler<DateTimeOffset>
{
    /// <summary>
    /// Parses a database value into a DateTimeOffset.
    /// </summary>
    /// <param name="value">The value from the database (TEXT or DateTimeOffset).</param>
    /// <returns>A DateTimeOffset with timezone information preserved.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the value cannot be converted to DateTimeOffset.</exception>
    public override DateTimeOffset Parse(object value)
    {
        if (value is string textValue)
        {
            return DateTimeOffset.Parse(textValue, CultureInfo.InvariantCulture);
        }

        if (value is DateTimeOffset dateTimeOffset)
        {
            return dateTimeOffset;
        }

        throw new InvalidOperationException($"Cannot convert {value?.GetType()} to DateTimeOffset");
    }

    /// <summary>
    /// Sets a DateTimeOffset value as a database parameter.
    /// </summary>
    /// <param name="parameter">The database parameter to set.</param>
    /// <param name="value">The DateTimeOffset value to store (timezone offset is preserved).</param>
    /// <remarks>
    /// The timezone offset is preserved in the stored format (e.g., "-06:00" for Central Time).
    /// </remarks>
    public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
    {
        // Store in ISO 8601 format with timezone offset
        parameter.Value = value.ToString("yyyy-MM-dd HH:mm:sszzz", CultureInfo.InvariantCulture);
        parameter.DbType = DbType.String;
    }
}
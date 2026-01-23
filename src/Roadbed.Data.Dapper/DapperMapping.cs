namespace Roadbed.Data;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using Dapper;

/// <summary>
/// Configures Dapper type mappings to use [Column] attributes from the System.ComponentModel.DataAnnotations.Schema namespace.
/// </summary>
public static class DapperMapping
{
    private static readonly HashSet<Type> _configuredTypes = new HashSet<Type>();
    private static readonly object _lock = new object();

    /// <summary>
    /// Configures Dapper to use [Column] attributes for the specified types.
    /// This method is thread-safe and will only configure each type once.
    /// </summary>
    /// <param name="types">The types to configure for Dapper mapping.</param>
    public static void Configure(params Type[] types)
    {
        lock (_lock)
        {
            foreach (Type type in types)
            {
                if (!_configuredTypes.Contains(type))
                {
                    ConfigureTypeMap(type);
                    _configuredTypes.Add(type);
                }
            }
        }
    }

    /// <summary>
    /// Configures Dapper to use [Column] attributes for the specified types.
    /// This method is thread-safe and will only configure each type once.
    /// </summary>
    /// <param name="types">The types to configure for Dapper mapping.</param>
    public static void Configure(IEnumerable<Type> types)
    {
        Configure(types.ToArray());
    }

    /// <summary>
    /// Configures Dapper type mapping for a specific type to use [Column] attributes.
    /// </summary>
    /// <param name="type">The type to configure.</param>
    private static void ConfigureTypeMap(Type type)
    {
        SqlMapper.SetTypeMap(
            type,
            new CustomPropertyTypeMap(
                type,
                FindPropertyByColumnAttribute));
    }

    /// <summary>
    /// Finds a property on a type that matches the given database column name using the [Column] attribute.
    /// Falls back to case-insensitive property name matching if no [Column] attribute is found.
    /// </summary>
    /// <param name="type">The type to search for properties.</param>
    /// <param name="columnName">The database column name to match.</param>
    /// <returns>The matching PropertyInfo.</returns>
    private static PropertyInfo FindPropertyByColumnAttribute(Type type, string columnName)
    {
        PropertyInfo[] properties = type.GetProperties();

        // First, try to find by [Column] attribute
        foreach (PropertyInfo property in properties)
        {
            ColumnAttribute[] columnAttributes = property
                .GetCustomAttributes(false)
                .OfType<ColumnAttribute>()
                .ToArray();

            foreach (ColumnAttribute attribute in columnAttributes)
            {
                if (attribute.Name == columnName)
                {
                    return property;
                }
            }
        }

        // Fallback: try to match by property name (case-insensitive)
        PropertyInfo? matchByName = properties.FirstOrDefault(
            p => string.Equals(p.Name, columnName, StringComparison.OrdinalIgnoreCase));

        if (matchByName != null)
        {
            return matchByName;
        }

        // If no match found, throw exception
        throw new InvalidOperationException(
            $"Could not find property for column '{columnName}' on type '{type.Name}'.");
    }
}
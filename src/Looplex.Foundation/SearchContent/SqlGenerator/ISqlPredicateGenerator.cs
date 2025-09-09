using System.Collections.Generic;
using System.Data;
using Looplex.Foundation.SearchContent.AST;

namespace Looplex.Foundation.SearchContent.SqlGenerator;

/// <summary>
/// Interface for generating SQL predicates from SCIM filter AST
/// 
/// Converts SCIM filter expressions into parameterized SQL WHERE clauses
/// that can be used with various database systems
/// </summary>
public interface ISqlPredicateGenerator
{
    /// <summary>
    /// Generate a parameterized SQL predicate from an AST node
    /// </summary>
    /// <param name="astNode">Root node of the filter AST</param>
    /// <returns>SQL predicate result with SQL and parameters</returns>
    SqlPredicateResult GeneratePredicate(IAstNode astNode);

    /// <summary>
    /// Generate SQL predicate with custom field mappings
    /// </summary>
    /// <param name="astNode">Root node of the filter AST</param>
    /// <param name="fieldMapping">Custom mapping from SCIM attributes to SQL column names</param>
    /// <returns>SQL predicate result with SQL and parameters</returns>
    SqlPredicateResult GeneratePredicate(IAstNode astNode, Dictionary<string, string> fieldMapping);

    /// <summary>
    /// Generate SQL predicate with table alias
    /// </summary>
    /// <param name="astNode">Root node of the filter AST</param>
    /// <param name="tableAlias">Table alias to prefix column names</param>
    /// <returns>SQL predicate result with SQL and parameters</returns>
    SqlPredicateResult GeneratePredicate(IAstNode astNode, string tableAlias);

    /// <summary>
    /// Generate SQL predicate with full customization options
    /// </summary>
    /// <param name="astNode">Root node of the filter AST</param>
    /// <param name="options">Generation options</param>
    /// <returns>SQL predicate result with SQL and parameters</returns>
    SqlPredicateResult GeneratePredicate(IAstNode astNode, SqlGenerationOptions options);
}

/// <summary>
/// Result of SQL predicate generation
/// </summary>
public class SqlPredicateResult
{
    /// <summary>
    /// Generated SQL WHERE clause (without the WHERE keyword)
    /// </summary>
    public string Sql { get; set; } = string.Empty;

    /// <summary>
    /// SQL parameters to prevent injection attacks
    /// </summary>
    public System.Collections.Generic.IReadOnlyDictionary<string, object?> Parameters { get; set; } = new System.Collections.Generic.Dictionary<string, object?>();

    /// <summary>
    /// Check if the result contains any conditions
    /// </summary>
    public bool HasConditions => !string.IsNullOrWhiteSpace(Sql);

    /// <summary>
    /// Get SQL with WHERE keyword if conditions exist
    /// </summary>
    public string GetWhereClause() => HasConditions ? $"WHERE {Sql}" : string.Empty;
}

/// <summary>
/// Options for SQL generation
/// </summary>
public class SqlGenerationOptions
{
    /// <summary>
    /// Custom mapping from SCIM attributes to SQL column names
    /// </summary>
    public Dictionary<string, string> FieldMapping { get; set; } = new(System.StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Table alias to prefix column names
    /// </summary>
    public string? TableAlias { get; set; }

    /// <summary>
    /// Whether to use case-sensitive string comparisons
    /// </summary>
    public bool CaseSensitive { get; set; } = true;

    /// <summary>
    /// Parameter prefix for generated parameter names
    /// </summary>
    public string ParameterPrefix { get; set; } = "p";

    /// <summary>
    /// Whether to include null checks for present (pr) operations
    /// </summary>
    public bool IncludeNullChecks { get; set; } = true;

    /// <summary>
    /// Database-specific SQL dialect settings
    /// </summary>
    public SqlDialect Dialect { get; set; } = SqlDialect.Standard;

    /// <summary>
    /// Whether to generate parameterized SQL (true) or inline values (false)
    /// Set to false for stored procedure compatibility
    /// </summary>
    public bool UseParameters { get; set; } = true;

    /// <summary>
    /// Whether to escape string values for inline SQL generation
    /// Only used when UseParameters is false
    /// </summary>
    public bool EscapeStrings { get; set; } = true;

    /// <summary>
    /// Table name to use for complex filter EXISTS queries
    /// Must be configured by the consuming application
    /// </summary>
    public string? ComplexFilterTableName { get; set; }

    /// <summary>
    /// Enables debug logging for troubleshooting SCIM filter processing.
    /// When enabled, outputs detailed information about key matching and SQL generation.
    /// Defaults to false for production use to avoid log pollution.
    /// </summary>
    public bool EnableDebugLogging { get; set; } = false;
}

/// <summary>
/// Supported SQL dialects
/// </summary>
public enum SqlDialect
{
    /// <summary>
    /// Standard SQL (ANSI SQL)
    /// </summary>
    Standard,

    /// <summary>
    /// Microsoft SQL Server
    /// </summary>
    SqlServer,

    /// <summary>
    /// PostgreSQL
    /// </summary>
    PostgreSql,

    /// <summary>
    /// MySQL
    /// </summary>
    MySql,

    /// <summary>
    /// SQLite
    /// </summary>
    SQLite,

    /// <summary>
    /// Oracle Database
    /// </summary>
    Oracle
}


using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Security;
using Looplex.Foundation.SearchContent;
using Looplex.Foundation.SearchContent.Parser;
using Looplex.Foundation.SearchContent.SqlGenerator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Looplex.Foundation.SCIMv2.Queries
{
    /// <summary>
    /// Configuration options for SCIM filter processing
    /// </summary>
    public class ScimFilterConfiguration
    {
        /// <summary>
        /// Default SQL dialect to use when not explicitly specified
        /// </summary>
        public SqlDialect DefaultDialect { get; set; } = SqlDialect.SqlServer;

        /// <summary>
        /// Default field mapping for SCIM attributes to database columns
        /// </summary>
        public Dictionary<string, string> DefaultFieldMapping { get; set; } = new();
    }

    /// <summary>
    /// Service for SCIM filter processing with configurable dialect
    /// </summary>
    public interface IScimFilterService
    {
        /// <summary>
        /// Converts SCIM filter to SQL with configurable dialect
        /// </summary>
        (string Sql, Dictionary<string, object> Parameters) ToSqlPredicateWithParameters(
            string? filter, 
            Dictionary<string, string>? schemaMapping = null,
            SqlDialect? dialect = null);
    }

    /// <summary>
    /// Implementation of SCIM filter service with configurable dialect
    /// </summary>
    public class ScimFilterService : IScimFilterService
    {
        private readonly ScimFilterConfiguration _config;
        private readonly ISearchContentService _searchService;

        public ScimFilterService(IOptions<ScimFilterConfiguration> config, ISearchContentService searchService)
        {
            _config = config.Value;
            _searchService = searchService;
        }

        public (string Sql, Dictionary<string, object> Parameters) ToSqlPredicateWithParameters(
            string? filter, 
            Dictionary<string, string>? schemaMapping = null,
            SqlDialect? dialect = null)
        {
            if (string.IsNullOrEmpty(filter)) 
                return (string.Empty, new Dictionary<string, object>());

            try
            {
                // Priority: 1. Explicit dialect, 2. DI config, 3. Default SqlServer
                var selectedDialect = dialect ?? _config.DefaultDialect;
                var selectedMapping = schemaMapping ?? _config.DefaultFieldMapping;

                var options = new SqlGenerationOptions
                {
                    FieldMapping = selectedMapping,
                    Dialect = selectedDialect
                };

                var result = _searchService.ConvertToSql(filter, options);
                var sanitizedSql = ValidateAndSanitizeSql(result.Sql);

                return (sanitizedSql, result.Parameters ?? new Dictionary<string, object>());
            }
            catch (FilterParseException ex)
            {
                throw new InvalidOperationException($"Invalid SCIM filter: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to convert SCIM filter to SQL: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validates and sanitizes SQL to prevent SQL injection
        /// </summary>
        private static string ValidateAndSanitizeSql(string sql)
        {
            if (string.IsNullOrEmpty(sql)) return string.Empty;
            
            // Remove SQL comments
            sql = Regex.Replace(sql, @"--.*$", "", RegexOptions.Multiline);
            
            // Remove multiple spaces and normalize whitespace
            sql = Regex.Replace(sql, @"\s+", " ");
            
            // Ensure it's only a WHERE clause
            sql = sql.Trim();
            if (!sql.StartsWith("WHERE ", StringComparison.OrdinalIgnoreCase))
            {
                sql = "WHERE " + sql;
            }
            
            // Reject dangerous SQL keywords
            var dangerousKeywords = new[] { "DROP", "DELETE", "TRUNCATE", "ALTER", "CREATE", "EXEC", "EXECUTE", "INSERT", "UPDATE" };
            foreach (var keyword in dangerousKeywords)
            {
                if (sql.ToLowerInvariant().Contains(keyword.ToLowerInvariant()))
                {
                    throw new SecurityException($"SQL contains dangerous keyword: {keyword}");
                }
            }
            
            // Reject multiple statements
            if (sql.ToLowerInvariant().Contains(";"))
            {
                throw new SecurityException("SQL contains multiple statements");
            }
            
            return sql;
        }
    }

    /// <summary>
    /// Extension methods for SCIM filter processing that maintain backward compatibility
    /// with existing client applications while providing enhanced security and functionality.
    /// </summary>
    public static class FilterExtensions
    {
        /// <summary>
        /// Converts a SCIM filter expression to a SQL WHERE clause string.
        /// This method maintains backward compatibility with existing client applications.
        /// </summary>
        /// <param name="filter">The SCIM filter expression to convert</param>
        /// <returns>A SQL WHERE clause string, or null if the filter is null or empty</returns>
        /// <exception cref="InvalidOperationException">Thrown when the filter is invalid or cannot be parsed</exception>
        public static string? ToSqlPredicate(this string? filter)
        {
            if (string.IsNullOrEmpty(filter)) return null;
            
            try
            {
                var service = new SearchContentService();
                var result = service.ConvertToSql(filter);
                
                // Validate and sanitize the generated SQL
                return ValidateAndSanitizeSql(result.Sql);
            }
            catch (FilterParseException ex)
            {
                // Security: Reject malformed filters
                throw new InvalidOperationException($"Invalid SCIM filter: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                // Security: Log and reject any other errors
                throw new InvalidOperationException($"Failed to convert SCIM filter to SQL: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Converts a SCIM filter expression to a SQL WHERE clause string with schema mapping.
        /// This method maintains backward compatibility with existing client applications.
        /// </summary>
        /// <param name="filter">The SCIM filter expression to convert</param>
        /// <param name="schemaMapping">Dictionary mapping SCIM attribute names to database column names</param>
        /// <returns>A SQL WHERE clause string, or null if the filter is null or empty</returns>
        /// <exception cref="InvalidOperationException">Thrown when the filter is invalid or cannot be parsed</exception>
        public static string? ToSqlPredicate(this string? filter, Dictionary<string, string> schemaMapping)
        {
            if (string.IsNullOrEmpty(filter)) return null;
            
            try
            {
                var options = new SqlGenerationOptions
                {
                    FieldMapping = schemaMapping ?? new Dictionary<string, string>()
                };
                
                var service = new SearchContentService();
                var result = service.ConvertToSql(filter, options);
                
                // Validate and sanitize the generated SQL
                return ValidateAndSanitizeSql(result.Sql);
            }
            catch (FilterParseException ex)
            {
                // Security: Reject malformed filters
                throw new InvalidOperationException($"Invalid SCIM filter with schema mapping: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                // Security: Log and reject any other errors
                throw new InvalidOperationException($"Failed to convert SCIM filter to SQL with schema mapping: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Converts a SCIM filter expression to a SQL WHERE clause with parameters for enhanced security.
        /// This is the recommended approach for new implementations.
        /// Uses SqlServer dialect by default when not specified.
        /// </summary>
        /// <param name="filter">The SCIM filter expression to convert</param>
        /// <returns>A tuple containing the SQL WHERE clause and parameters dictionary</returns>
        /// <exception cref="InvalidOperationException">Thrown when the filter is invalid or cannot be parsed</exception>
        public static (string Sql, Dictionary<string, object> Parameters) ToSqlPredicateWithParameters(this string? filter)
        {
            if (string.IsNullOrEmpty(filter)) 
                return (string.Empty, new Dictionary<string, object>());
            
            try
            {
                var service = new SearchContentService();
                var result = service.ConvertToSql(filter);
                
                // Validate and sanitize the generated SQL
                var sanitizedSql = ValidateAndSanitizeSql(result.Sql);
                
                return (sanitizedSql, result.Parameters ?? new Dictionary<string, object>());
            }
            catch (FilterParseException ex)
            {
                // Security: Reject malformed filters
                throw new InvalidOperationException($"Invalid SCIM filter: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                // Security: Log and reject any other errors
                throw new InvalidOperationException($"Failed to convert SCIM filter to SQL with parameters: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Converts a SCIM filter expression to a SQL WHERE clause with parameters and schema mapping.
        /// This is the recommended approach for new implementations.
        /// Uses SqlServer dialect by default when not specified.
        /// </summary>
        /// <param name="filter">The SCIM filter expression to convert</param>
        /// <param name="schemaMapping">Dictionary mapping SCIM attribute names to database column names</param>
        /// <param name="dialect">SQL dialect to use (optional, defaults to SqlServer)</param>
        /// <returns>A tuple containing the SQL WHERE clause and parameters dictionary</returns>
        /// <exception cref="InvalidOperationException">Thrown when the filter is invalid or cannot be parsed</exception>
        public static (string Sql, Dictionary<string, object> Parameters) ToSqlPredicateWithParameters(
            this string? filter, 
            Dictionary<string, string>? schemaMapping = null,
            SqlDialect? dialect = null)
        {
            if (string.IsNullOrEmpty(filter)) 
                return (string.Empty, new Dictionary<string, object>());
            
            try
            {
                // Priority: 1. Explicit dialect, 2. Default SqlServer
                var selectedDialect = dialect ?? SqlDialect.SqlServer;
                
                var options = new SqlGenerationOptions
                {
                    FieldMapping = schemaMapping ?? new Dictionary<string, string>(),
                    Dialect = selectedDialect
                };
                
                var service = new SearchContentService();
                var result = service.ConvertToSql(filter, options);
                
                // Validate and sanitize the generated SQL
                var sanitizedSql = ValidateAndSanitizeSql(result.Sql);
                
                return (sanitizedSql, result.Parameters ?? new Dictionary<string, object>());
            }
            catch (FilterParseException ex)
            {
                // Security: Reject malformed filters
                throw new InvalidOperationException($"Invalid SCIM filter with schema mapping: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                // Security: Log and reject any other errors
                throw new InvalidOperationException($"Failed to convert SCIM filter to SQL with parameters and schema mapping: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Validates and sanitizes SQL to prevent SQL injection and ensure it's a safe WHERE clause.
        /// </summary>
        /// <param name="sql">The SQL string to validate and sanitize</param>
        /// <returns>A sanitized SQL WHERE clause</returns>
        /// <exception cref="SecurityException">Thrown when dangerous SQL constructs are detected</exception>
        private static string ValidateAndSanitizeSql(string sql)
        {
            if (string.IsNullOrEmpty(sql)) return string.Empty;
            
            // Remove SQL comments
            sql = Regex.Replace(sql, @"--.*$", "", RegexOptions.Multiline);
            
            // Remove multiple spaces and normalize whitespace
            sql = Regex.Replace(sql, @"\s+", " ");
            
            // Ensure it's only a WHERE clause
            sql = sql.Trim();
            if (!sql.StartsWith("WHERE ", StringComparison.OrdinalIgnoreCase))
            {
                sql = "WHERE " + sql;
            }
            
            // Reject dangerous SQL keywords
            var dangerousKeywords = new[] { "DROP", "DELETE", "TRUNCATE", "ALTER", "CREATE", "EXEC", "EXECUTE", "INSERT", "UPDATE" };
            foreach (var keyword in dangerousKeywords)
            {
                if (sql.ToLowerInvariant().Contains(keyword.ToLowerInvariant()))
                {
                    throw new SecurityException($"SQL contains dangerous keyword: {keyword}");
                }
            }
            
            // Reject multiple statements
            if (sql.ToLowerInvariant().Contains(";"))
            {
                throw new SecurityException("SQL contains multiple statements");
            }
            
            return sql;
        }
    }
}

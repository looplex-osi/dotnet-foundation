using System.Collections.Generic;
using Looplex.Foundation.SearchContent.SqlGenerator;

namespace Looplex.Foundation.SearchContent;

/// <summary>
/// Interface for SCIM filter to SQL predicate conversion service
/// </summary>
public interface ISearchContentService
{
    /// <summary>
    /// Convert SCIM filter to SQL predicate with default options
    /// </summary>
    /// <param name="scimFilter">SCIM filter expression</param>
    /// <returns>SQL predicate result</returns>
    SqlPredicateResult ConvertToSql(string scimFilter);

    /// <summary>
    /// Convert SCIM filter to SQL predicate with field mapping
    /// </summary>
    /// <param name="scimFilter">SCIM filter expression</param>
    /// <param name="fieldMapping">Field name mapping dictionary</param>
    /// <returns>SQL predicate result</returns>
    SqlPredicateResult ConvertToSql(string scimFilter, Dictionary<string, string> fieldMapping);

    /// <summary>
    /// Convert SCIM filter to SQL predicate with table alias
    /// </summary>
    /// <param name="scimFilter">SCIM filter expression</param>
    /// <param name="tableAlias">Table alias to use</param>
    /// <returns>SQL predicate result</returns>
    SqlPredicateResult ConvertToSql(string scimFilter, string tableAlias);

    /// <summary>
    /// Convert SCIM filter to SQL predicate with custom options
    /// </summary>
    /// <param name="scimFilter">SCIM filter expression</param>
    /// <param name="options">SQL generation options</param>
    /// <returns>SQL predicate result</returns>
    SqlPredicateResult ConvertToSql(string scimFilter, SqlGenerationOptions options);

    /// <summary>
    /// Convert SCIM filter to SQL predicate for stored procedure compatibility
    /// Generates non-parameterized SQL with inline values
    /// </summary>
    /// <param name="scimFilter">SCIM filter expression</param>
    /// <returns>SQL predicate result with inline SQL (no parameters)</returns>
    SqlPredicateResult ConvertToSqlForStoredProcedure(string scimFilter);

    /// <summary>
    /// Convert SCIM filter to SQL predicate for stored procedure compatibility with field mapping
    /// Generates non-parameterized SQL with inline values
    /// </summary>
    /// <param name="scimFilter">SCIM filter expression</param>
    /// <param name="fieldMapping">Field name mapping dictionary</param>
    /// <returns>SQL predicate result with inline SQL (no parameters)</returns>
    SqlPredicateResult ConvertToSqlForStoredProcedure(string scimFilter, Dictionary<string, string> fieldMapping);

    /// <summary>
    /// Try to convert SCIM filter to SQL predicate without throwing exceptions
    /// </summary>
    /// <param name="scimFilter">SCIM filter expression</param>
    /// <param name="result">SQL predicate result if successful</param>
    /// <returns>True if conversion was successful, false otherwise</returns>
    bool TryConvertToSql(string scimFilter, out SqlPredicateResult? result);

    /// <summary>
    /// Validate if a SCIM filter expression is syntactically correct
    /// </summary>
    /// <param name="scimFilter">SCIM filter expression to validate</param>
    /// <returns>True if the filter is valid, false otherwise</returns>
    bool IsValidFilter(string scimFilter);

    /// <summary>
    /// Get list of supported SCIM operators
    /// </summary>
    /// <returns>Collection of supported operator names</returns>
    IEnumerable<string> GetSupportedOperators();

    /// <summary>
    /// Get examples of SCIM filter expressions for reference
    /// </summary>
    /// <returns>Dictionary of example names and their corresponding SCIM filters</returns>
    Dictionary<string, string> GetFilterExamples();
}

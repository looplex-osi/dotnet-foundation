using System;
using System.Collections.Generic;
using System.Linq;
using Looplex.Foundation.SearchContent.Parser;
using Looplex.Foundation.SearchContent.SqlGenerator;

namespace Looplex.Foundation.SearchContent;

/// <summary>
/// Main service for converting SCIM filter expressions to SQL predicates
/// 
/// Provides a high-level API for SCIM filter processing with support for:
/// - Sub-attributes (name.givenName)
/// - Schema prefixes (urn:ietf:params:scim:schemas:core:2.0:User:userName)  
/// - Null values (manager eq null)
/// - Complex expressions with AND, OR, NOT
/// - Very long expressions (50+ conditions)
/// - Nested parentheses
/// </summary>
public class SearchContentService : ISearchContentService
{
    private readonly IFilterParser _parser;
    private readonly ISqlPredicateGenerator _sqlGenerator;
    
    // Static instances for better performance when using default constructor
    private static readonly IFilterParser DefaultParser = new EnhancedScimFilterParser();
    private static readonly ISqlPredicateGenerator DefaultSqlGenerator = new SqlPredicateGenerator();

    /// <summary>
    /// Default constructor using enhanced parser
    /// </summary>
    public SearchContentService()
    {
        _parser = DefaultParser;
        _sqlGenerator = DefaultSqlGenerator;
    }

    /// <summary>
    /// Constructor with custom parser
    /// </summary>
    /// <param name="parser">Custom filter parser</param>
    public SearchContentService(IFilterParser parser)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _sqlGenerator = new SqlPredicateGenerator();
    }

    /// <summary>
    /// Constructor with custom parser and SQL generator
    /// </summary>
    /// <param name="parser">Custom filter parser</param>
    /// <param name="sqlGenerator">Custom SQL generator</param>
    public SearchContentService(IFilterParser parser, ISqlPredicateGenerator sqlGenerator)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _sqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
    }

    public SqlPredicateResult ConvertToSql(string scimFilter)
    {
        if (string.IsNullOrWhiteSpace(scimFilter))
            throw new FilterParseException("SCIM filter cannot be null or empty", 0);

        var ast = _parser.Parse(scimFilter);
        return _sqlGenerator.GeneratePredicate(ast);
    }

    public SqlPredicateResult ConvertToSql(string scimFilter, Dictionary<string, string> fieldMapping)
    {
        if (string.IsNullOrWhiteSpace(scimFilter))
            throw new FilterParseException("SCIM filter cannot be null or empty", 0);
        if (fieldMapping is null)
            throw new ArgumentNullException(nameof(fieldMapping));

        var ast = _parser.Parse(scimFilter);
        return _sqlGenerator.GeneratePredicate(ast, fieldMapping);
    }

    public SqlPredicateResult ConvertToSql(string scimFilter, string tableAlias)
    {
        if (string.IsNullOrWhiteSpace(scimFilter))
            throw new FilterParseException("SCIM filter cannot be null or empty", 0);
        if (string.IsNullOrWhiteSpace(tableAlias))
            throw new ArgumentException("Table alias cannot be null or whitespace.", nameof(tableAlias));

        var ast = _parser.Parse(scimFilter);
        return _sqlGenerator.GeneratePredicate(ast, tableAlias);
    }

    public SqlPredicateResult ConvertToSql(string scimFilter, SqlGenerationOptions options)
    {
        if (string.IsNullOrWhiteSpace(scimFilter))
            throw new FilterParseException("SCIM filter cannot be null or empty", 0);

        var ast = _parser.Parse(scimFilter);
        return _sqlGenerator.GeneratePredicate(ast, options);
    }

    /// <summary>
    /// Convert SCIM filter to SQL predicate for stored procedure compatibility
    /// Generates non-parameterized SQL with inline values
    /// </summary>
    /// <param name="scimFilter">SCIM filter expression</param>
    /// <returns>SQL predicate result with inline SQL (no parameters)</returns>
    public SqlPredicateResult ConvertToSqlForStoredProcedure(string scimFilter)
    {
        if (string.IsNullOrWhiteSpace(scimFilter))
            throw new FilterParseException("SCIM filter cannot be null or empty", 0);

        var options = new SqlGenerationOptions
        {
            UseParameters = false,
            EscapeStrings = true
        };

        var ast = _parser.Parse(scimFilter);
        return _sqlGenerator.GeneratePredicate(ast, options);
    }

    /// <summary>
    /// Convert SCIM filter to SQL predicate for stored procedure compatibility with field mapping
    /// </summary>
    /// <param name="scimFilter">SCIM filter expression</param>
    /// <param name="fieldMapping">Custom field mapping</param>
    /// <returns>SQL predicate result with inline SQL (no parameters)</returns>
    public SqlPredicateResult ConvertToSqlForStoredProcedure(string scimFilter, Dictionary<string, string> fieldMapping)
    {
        if (string.IsNullOrWhiteSpace(scimFilter))
            throw new FilterParseException("SCIM filter cannot be null or empty", 0);
        if (fieldMapping is null)
            throw new ArgumentNullException(nameof(fieldMapping));

        var options = new SqlGenerationOptions
        {
            UseParameters = false,
            EscapeStrings = true,
            FieldMapping = fieldMapping
        };

        var ast = _parser.Parse(scimFilter);
        return _sqlGenerator.GeneratePredicate(ast, options);
    }

    public bool TryConvertToSql(string scimFilter, out SqlPredicateResult? result)
    {
        result = null;
        try
        {
            result = ConvertToSql(scimFilter);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool IsValidFilter(string scimFilter)
    {
        if (string.IsNullOrWhiteSpace(scimFilter))
            return false;

        return _parser.TryParse(scimFilter, out _);
    }

    public IEnumerable<string> GetSupportedOperators()
    {
        return new[]
        {
            "eq",   // Equal
            "ne",   // Not equal
            "co",   // Contains
            "sw",   // Starts with
            "ew",   // Ends with
            "gt",   // Greater than
            "ge",   // Greater than or equal
            "lt",   // Less than
            "le",   // Less than or equal
            "pr",   // Present
            "and",  // Logical AND
            "or",   // Logical OR
            "not"   // Logical NOT
        };
    }

    public Dictionary<string, string> GetFilterExamples()
    {
        return new Dictionary<string, string>
        {
            // Basic comparisons
            ["Simple equality"] = "userName eq \"john\"",
            ["Not equal"] = "userName ne \"john\"",
            ["Contains"] = "displayName co \"John\"",
            ["Starts with"] = "userName sw \"j\"",
            ["Ends with"] = "email ew \"@example.com\"",
            ["Greater than"] = "age gt 25",
            ["Present check"] = "emails pr",
            
            // Sub-attributes
            ["Sub-attribute access"] = "name.givenName eq \"John\"",
            ["Sub-attribute contains"] = "name.familyName co \"Smith\"",
            
            // Schema prefixes  
            ["Schema prefix"] = "urn:ietf:params:scim:schemas:core:2.0:User:userName eq \"john\"",
            ["Complex schema path"] = "urn:ietf:params:scim:schemas:core:2.0:User:name.givenName eq \"John\"",
            
            // Null values
            ["Null equality"] = "manager eq null",
            ["Not null"] = "manager ne null",
            
            // Logical operations
            ["AND operation"] = "userName eq \"john\" and age gt 25",
            ["OR operation"] = "department eq \"IT\" or role eq \"admin\"",
            ["NOT operation"] = "not (department eq \"HR\")",
            
            // Complex expressions
            ["Complex AND/OR"] = "(userName eq \"john\" or userName eq \"jane\") and department eq \"IT\"",
            ["Nested NOT"] = "not ((department eq \"HR\" or department eq \"Finance\") and role eq \"manager\")",
            ["Multiple conditions"] = "age gt 18 and age lt 65 and department eq \"IT\" and role ne \"intern\"",
            
            // Long expressions
            ["Very long expression"] = "userName eq \"user1\" or userName eq \"user2\" or userName eq \"user3\" or userName eq \"user4\" or userName eq \"user5\" or department eq \"IT\" or department eq \"HR\" or department eq \"Finance\" or department eq \"Marketing\" or department eq \"Sales\"",
            
            // Different data types
            ["Boolean value"] = "active eq true",
            ["Decimal value"] = "salary ge 50000.50",
            ["Date comparison"] = "lastLogin gt \"2023-01-01\"",
            
            // Mixed complexity
            ["Enterprise search"] = "(name.givenName co \"John\" or name.familyName co \"Smith\") and (department eq \"IT\" or role eq \"admin\") and active eq true and manager ne null"
        };
    }
}
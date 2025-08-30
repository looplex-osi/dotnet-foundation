using Looplex.Foundation.SearchContent.AST;

namespace Looplex.Foundation.SearchContent.Parser;

/// <summary>
/// Interface for SCIM filter parsers
/// 
/// Defines the contract for parsing SCIM filter expressions into AST
/// Based on RFC 7644 Section 3.4.2.2 - Filtering
/// </summary>
public interface IFilterParser
{
    /// <summary>
    /// Parse a SCIM filter expression into an AST
    /// </summary>
    /// <param name="filterExpression">SCIM filter expression string</param>
    /// <returns>Root node of the parsed AST</returns>
    /// <exception cref="FilterParseException">Thrown when the filter expression is invalid</exception>
    IAstNode Parse(string filterExpression);

    /// <summary>
    /// Try to parse a SCIM filter expression into an AST
    /// </summary>
    /// <param name="filterExpression">SCIM filter expression string</param>
    /// <param name="astNode">Resulting AST node if parsing succeeds</param>
    /// <returns>True if parsing succeeds, false otherwise</returns>
    bool TryParse(string filterExpression, out IAstNode? astNode);
}



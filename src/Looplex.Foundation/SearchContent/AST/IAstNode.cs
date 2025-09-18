using Looplex.Foundation.SearchContent.AST;

namespace Looplex.Foundation.SearchContent.AST;

/// <summary>
/// Base interface for all nodes in the Abstract Syntax Tree (AST)
/// 
/// Implements the Visitor pattern to allow different operations
/// on AST nodes without modifying the node classes themselves.
/// 
/// Based on RFC 7644 - SCIM Filter Syntax
/// https://tools.ietf.org/rfc/rfc7644.html#section-3.4.2.2
/// </summary>
public interface IAstNode
{
    /// <summary>
    /// Accepts a visitor and returns the result of visiting this node
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the visitor</typeparam>
    /// <param name="visitor">The visitor to accept</param>
    /// <returns>The result of visiting this node</returns>
    TResult Accept<TResult>(IAstVisitor<TResult> visitor);
}

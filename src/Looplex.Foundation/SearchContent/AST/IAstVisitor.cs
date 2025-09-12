using Looplex.Foundation.SearchContent.AST.Nodes;

namespace Looplex.Foundation.SearchContent.AST;

/// <summary>
/// Visitor interface for traversing AST nodes
/// 
/// Implements the Visitor pattern for type-safe traversal of SCIM filter AST
/// </summary>
/// <typeparam name="T">Return type of visit operations</typeparam>
public interface IAstVisitor<T>
{
    /// <summary>
    /// Visit a binary expression node (AND, OR)
    /// </summary>
    T VisitBinaryExpression(BinaryExpressionNode node);

    /// <summary>
    /// Visit a unary expression node (NOT)
    /// </summary>
    T VisitUnaryExpression(UnaryExpressionNode node);

    /// <summary>
    /// Visit a comparison expression node (eq, ne, co, etc.)
    /// </summary>
    T VisitComparisonExpression(ComparisonExpressionNode node);

    /// <summary>
    /// Visit a parenthesized expression node
    /// </summary>
    T VisitParenthesizedExpression(ParenthesizedExpressionNode node);

    /// <summary>
    /// Visit an identifier node (attribute name)
    /// </summary>
    T VisitIdentifier(IdentifierNode node);

    /// <summary>
    /// Visit a literal value node (string, number, boolean, null)
    /// </summary>
    T VisitLiteralValue(LiteralValueNode node);
}



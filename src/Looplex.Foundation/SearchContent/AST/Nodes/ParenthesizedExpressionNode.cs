using System;

namespace Looplex.Foundation.SearchContent.AST.Nodes;

/// <summary>
/// Represents a parenthesized expression in the SCIM filter AST
/// 
/// Examples:
/// - (userName eq "john" AND age gt 25)
/// - NOT (department eq "IT" OR role eq "admin")
/// - ((a eq 1) OR (b eq 2)) AND (c eq 3)
/// </summary>
public class ParenthesizedExpressionNode : IAstNode
{
    /// <summary>
    /// The expression inside the parentheses
    /// </summary>
    public IAstNode Expression { get; }

    public ParenthesizedExpressionNode(IAstNode expression)
    {
        Expression = expression ?? throw new ArgumentNullException(nameof(expression));
    }

    public T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitParenthesizedExpression(this);
    }

    public override string ToString()
    {
        return $"({Expression})";
    }
}



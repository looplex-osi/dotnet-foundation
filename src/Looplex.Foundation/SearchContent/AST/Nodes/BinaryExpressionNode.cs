using System;

namespace Looplex.Foundation.SearchContent.AST.Nodes;

/// <summary>
/// Represents a binary expression (AND, OR) in the SCIM filter AST
/// 
/// Examples:
/// - userName eq "john" AND age gt 25
/// - department eq "IT" OR role eq "admin"
/// </summary>
public class BinaryExpressionNode : IAstNode
{
    /// <summary>
    /// Left operand of the binary expression
    /// </summary>
    public IAstNode Left { get; }

    /// <summary>
    /// Right operand of the binary expression
    /// </summary>
    public IAstNode Right { get; }

    /// <summary>
    /// Binary operator (AND, OR)
    /// </summary>
    public BinaryOperator Operator { get; }

    public BinaryExpressionNode(IAstNode left, IAstNode right, BinaryOperator @operator)
    {
        Left = left ?? throw new ArgumentNullException(nameof(left));
        Right = right ?? throw new ArgumentNullException(nameof(right));
        Operator = @operator;
    }

    public T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitBinaryExpression(this);
    }

    public override string ToString()
    {
        return $"({Left} {Operator} {Right})";
    }
}

/// <summary>
/// Binary operators supported in SCIM filters
/// </summary>
public enum BinaryOperator
{
    /// <summary>
    /// Logical AND operator
    /// </summary>
    And,

    /// <summary>
    /// Logical OR operator
    /// </summary>
    Or
}



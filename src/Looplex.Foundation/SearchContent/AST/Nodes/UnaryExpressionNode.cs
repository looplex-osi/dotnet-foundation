using System;

namespace Looplex.Foundation.SearchContent.AST.Nodes;

/// <summary>
/// Represents a unary expression (NOT) in the SCIM filter AST
/// 
/// Examples:
/// - NOT (userName eq "john")
/// - NOT (department eq "IT" AND role eq "admin")
/// </summary>
public class UnaryExpressionNode : IAstNode
{
    /// <summary>
    /// Operand of the unary expression
    /// </summary>
    public IAstNode Operand { get; }

    /// <summary>
    /// Unary operator (NOT)
    /// </summary>
    public UnaryOperator Operator { get; }

    public UnaryExpressionNode(IAstNode operand, UnaryOperator @operator)
    {
        Operand = operand ?? throw new ArgumentNullException(nameof(operand));
        Operator = @operator;
    }

    public T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitUnaryExpression(this);
    }

    public override string ToString()
    {
        return $"{Operator} {Operand}";
    }
}

/// <summary>
/// Unary operators supported in SCIM filters
/// </summary>
public enum UnaryOperator
{
    /// <summary>
    /// Logical NOT operator
    /// </summary>
    Not
}



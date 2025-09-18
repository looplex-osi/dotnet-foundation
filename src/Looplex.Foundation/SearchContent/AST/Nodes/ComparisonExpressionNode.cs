using System;

namespace Looplex.Foundation.SearchContent.AST.Nodes;

/// <summary>
/// Represents a comparison expression in the SCIM filter AST
/// 
/// Examples:
/// - userName eq "john"
/// - age gt 25
/// - name.givenName co "John"
/// - urn:ietf:params:scim:schemas:core:2.0:User:userName sw "j"
/// - manager eq null
/// - emails pr
/// </summary>
public class ComparisonExpressionNode : IAstNode
{
    /// <summary>
    /// Left operand (attribute identifier)
    /// </summary>
    public IdentifierNode Attribute { get; }

    /// <summary>
    /// Comparison operator
    /// </summary>
    public ComparisonOperator Operator { get; }

    /// <summary>
    /// Right operand (value)
    /// </summary>
    public IAstNode Value { get; }

    public ComparisonExpressionNode(IdentifierNode attribute, ComparisonOperator @operator, IAstNode value)
    {
        Attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
        Operator = @operator;
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitComparisonExpression(this);
    }

    public override string ToString()
    {
        return $"{Attribute} {Operator} {Value}";
    }
}

/// <summary>
/// Comparison operators supported in SCIM filters
/// Based on RFC 7644 Section 3.4.2.2
/// </summary>
public enum ComparisonOperator
{
    /// <summary>
    /// Equal (eq) - The attribute and operator values must be identical for a match
    /// </summary>
    Equal,

    /// <summary>
    /// Not equal (ne) - The attribute and operator values are not identical
    /// </summary>
    NotEqual,

    /// <summary>
    /// Contains (co) - The entire operator value must be a substring of the attribute value for a match
    /// </summary>
    Contains,

    /// <summary>
    /// Starts with (sw) - The entire operator value must be a substring of the attribute value, starting at the beginning for a match
    /// </summary>
    StartsWith,

    /// <summary>
    /// Ends with (ew) - The entire operator value must be a substring of the attribute value, matching at the end for a match
    /// </summary>
    EndsWith,

    /// <summary>
    /// Greater than (gt) - The attribute value must be greater than the operator value for a match
    /// </summary>
    GreaterThan,

    /// <summary>
    /// Greater than or equal (ge) - The attribute value must be greater than or equal to the operator value for a match
    /// </summary>
    GreaterThanOrEqual,

    /// <summary>
    /// Less than (lt) - The attribute value must be less than the operator value for a match
    /// </summary>
    LessThan,

    /// <summary>
    /// Less than or equal (le) - The attribute value must be less than or equal to the operator value for a match
    /// </summary>
    LessThanOrEqual,

    /// <summary>
    /// Present (pr) - The attribute must have a non-null value for a match
    /// </summary>
    Present
}



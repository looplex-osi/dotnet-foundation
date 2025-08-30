using System;

namespace Looplex.Foundation.SearchContent.AST.Nodes;

/// <summary>
/// Represents a literal value in the SCIM filter AST
/// 
/// Supports all SCIM value types:
/// - Strings: "john", "IT Department"
/// - Numbers: 25, 3.14
/// - Booleans: true, false
/// - Null: null
/// </summary>
public class LiteralValueNode : IAstNode
{
    /// <summary>
    /// The literal value
    /// </summary>
    public object? Value { get; }

    /// <summary>
    /// Type of the literal value
    /// </summary>
    public LiteralType Type { get; }

    public LiteralValueNode(object? value, LiteralType type)
    {
        Value = value;
        Type = type;
    }

    /// <summary>
    /// Constructor for string values
    /// </summary>
    public LiteralValueNode(string value) : this(value, LiteralType.String) { }

    /// <summary>
    /// Constructor for integer values
    /// </summary>
    public LiteralValueNode(int value) : this(value, LiteralType.Integer) { }

    /// <summary>
    /// Constructor for decimal values
    /// </summary>
    public LiteralValueNode(decimal value) : this(value, LiteralType.Decimal) { }

    /// <summary>
    /// Constructor for boolean values
    /// </summary>
    public LiteralValueNode(bool value) : this(value, LiteralType.Boolean) { }

    public T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitLiteralValue(this);
    }

    /// <summary>
    /// Get the value as a string for SQL generation
    /// </summary>
    public string GetSqlValue()
    {
        return Type switch
        {
            LiteralType.String => $"'{Value?.ToString()?.Replace("'", "''")}'",
            LiteralType.Integer => Value?.ToString() ?? "NULL",
            LiteralType.Decimal => Value?.ToString() ?? "NULL",
            LiteralType.Boolean => (bool)(Value ?? false) ? "1" : "0",
            LiteralType.Null => "NULL",
            _ => throw new ArgumentException($"Unsupported literal type: {Type}")
        };
    }

    /// <summary>
    /// Check if this is a null value
    /// </summary>
    public bool IsNull => Type == LiteralType.Null || Value == null;

    public override string ToString()
    {
        return Type switch
        {
            LiteralType.String => $"\"{Value}\"",
            LiteralType.Null => "null",
            _ => Value?.ToString() ?? "null"
        };
    }
}

/// <summary>
/// Types of literal values supported in SCIM filters
/// Based on RFC 7644 Section 2.3 - Attribute Data Types
/// </summary>
public enum LiteralType
{
    /// <summary>
    /// String value (Unicode characters)
    /// </summary>
    String,

    /// <summary>
    /// Integer value (whole numbers)
    /// </summary>
    Integer,

    /// <summary>
    /// Decimal value (floating-point numbers)
    /// </summary>
    Decimal,

    /// <summary>
    /// Boolean value (true/false)
    /// </summary>
    Boolean,

    /// <summary>
    /// Null value (absence of value)
    /// </summary>
    Null
}



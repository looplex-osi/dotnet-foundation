using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Looplex.Foundation.SearchContent.AST;
using Looplex.Foundation.SearchContent.AST.Nodes;

namespace Looplex.Foundation.SearchContent.Parser;

/// <summary>
/// Enhanced SCIM filter parser that supports all SCIM features including:
/// - Sub-attributes (name.givenName)
/// - Schema prefixes (urn:ietf:params:scim:schemas:core:2.0:User:name)
/// - Null values (manager eq null)
/// - Very long expressions (50+ conditions)
/// - Complex NOT with nested parentheses
/// 
/// Based on RFC 7644 - SCIM Filter Syntax
/// https://tools.ietf.org/rfc/rfc7644.html#section-3.4.2.2
/// </summary>
public class EnhancedScimFilterParser : IFilterParser
{
    private readonly Dictionary<string, ComparisonOperator> _comparisonOperators;
    private readonly Dictionary<string, BinaryOperator> _logicalOperators;
    
    // Static compiled regex for better performance
    // Must start with a letter; may contain letters, digits, underscores, and dots; cannot end with a dot
    private static readonly Regex AttributePatternRegex = new(@"^[a-zA-Z](?:[a-zA-Z0-9_\.]*[a-zA-Z0-9_])?$", RegexOptions.Compiled);
    
    public EnhancedScimFilterParser()
    {
        _comparisonOperators = new Dictionary<string, ComparisonOperator>(StringComparer.OrdinalIgnoreCase)
        {
            { "eq", ComparisonOperator.Equal },
            { "ne", ComparisonOperator.NotEqual },
            { "co", ComparisonOperator.Contains },
            { "sw", ComparisonOperator.StartsWith },
            { "ew", ComparisonOperator.EndsWith },
            { "gt", ComparisonOperator.GreaterThan },
            { "ge", ComparisonOperator.GreaterThanOrEqual },
            { "lt", ComparisonOperator.LessThan },
            { "le", ComparisonOperator.LessThanOrEqual },
            { "pr", ComparisonOperator.Present }
        };

        _logicalOperators = new Dictionary<string, BinaryOperator>(StringComparer.OrdinalIgnoreCase)
        {
            { "and", BinaryOperator.And },
            { "or", BinaryOperator.Or }
        };
    }

    public IAstNode Parse(string filterExpression)
    {
        if (string.IsNullOrWhiteSpace(filterExpression))
            throw new FilterParseException("Filter expression cannot be null or empty");

        // Validate input length to prevent DoS
        if (filterExpression.Length > 10000)
            throw new FilterParseException("Filter expression too long (maximum 10000 characters)");

        // Validate parentheses balance
        ValidateParenthesesBalance(filterExpression);
        
        // Validate quote balance
        ValidateQuoteBalance(filterExpression);
        
        var tokens = Tokenize(filterExpression);
        var parser = new TokenParser(tokens, _comparisonOperators, _logicalOperators);
        return parser.ParseExpression();
    }

    /// <summary>
    /// Validates that parentheses are properly balanced
    /// </summary>
    private void ValidateParenthesesBalance(string expression)
    {
        var stack = new Stack<int>();
        bool inQuotes = false;
        for (int i = 0; i < expression.Length; i++)
        {
            var c = expression[i];
            // Toggle only on unescaped quotes
            if (c == '"' && (i == 0 || expression[i - 1] != '\\'))
            {
                inQuotes = !inQuotes;
                continue;
            }
            if (inQuotes) continue;
            if (c == '(') stack.Push(i);
            else if (c == ')')
            {
                if (stack.Count == 0)
                    throw new FilterParseException($"Unmatched closing parenthesis at position {i}", i);
                stack.Pop();
            }
        }
        if (stack.Count > 0)
        {
            var firstUnmatched = stack.Pop();
            throw new FilterParseException($"Unmatched opening parenthesis at position {firstUnmatched}", firstUnmatched);
        }
    }

    /// <summary>
    /// Validates that quotes are properly balanced and formatted
    /// </summary>
    private void ValidateQuoteBalance(string expression)
    {
        bool inQuotes = false;
        bool escaped = false;
        for (int i = 0; i < expression.Length; i++)
        {
            var c = expression[i];
            if (escaped) { escaped = false; continue; }
            if (c == '\\') { escaped = true; continue; }
            if (c == '"') 
            {
                // Check for triple quotes (invalid)
                if (i + 2 < expression.Length && 
                    expression[i + 1] == '"' && 
                    expression[i + 2] == '"')
                {
                    throw new FilterParseException($"Triple quotes not allowed at position {i}", i);
                }
                inQuotes = !inQuotes;
            }
        }
        if (inQuotes)
            throw new FilterParseException("Unmatched quotes in filter expression");
    }

    public bool TryParse(string filterExpression, out IAstNode? astNode)
    {
        astNode = null;
        try
        {
            astNode = Parse(filterExpression);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Tokenize the filter expression into meaningful tokens
    /// </summary>
    private List<Token> Tokenize(string expression)
    {
        var tokens = new List<Token>();
        var i = 0;
        
        while (i < expression.Length)
        {
            // Skip whitespace
            if (char.IsWhiteSpace(expression[i]))
            {
                i++;
                continue;
            }

            // Handle parentheses
            if (expression[i] == '(')
            {
                tokens.Add(new Token(TokenType.LeftParen, "(", i));
                i++;
                continue;
            }
            
            if (expression[i] == ')')
            {
                tokens.Add(new Token(TokenType.RightParen, ")", i));
                i++;
                continue;
            }

            // Handle quoted strings
            if (expression[i] == '"')
            {
                var (stringValue, endIndex) = ReadQuotedString(expression, i);
                tokens.Add(new Token(TokenType.String, stringValue, i));
                i = endIndex + 1;
                continue;
            }

            // Handle identifiers, operators, and values
            var (token, nextIndex) = ReadToken(expression, i);
            if (token != null)
            {
                tokens.Add(token);
                i = nextIndex;
                continue;
            }

            throw new FilterParseException($"Unexpected character '{expression[i]}' at position {i}");
        }

        return tokens;
    }

    private (string value, int endIndex) ReadQuotedString(string expression, int startIndex)
    {
        var sb = new StringBuilder();
        var i = startIndex + 1; // Skip opening quote
        
        while (i < expression.Length)
        {
            if (expression[i] == '"')
            {
                return (sb.ToString(), i);
            }
            
            if (expression[i] == '\\' && i + 1 < expression.Length)
            {
                // Handle escape sequences
                switch (expression[i + 1])
                {
                    case '"': sb.Append('"'); break;
                    case '\'': sb.Append('\''); break; // Allow escaped single quote
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    default: 
                        throw new FilterParseException($"Invalid escape sequence '\\{expression[i + 1]}' at position {i + 1}", i + 1);
                }
                i += 2;
            }
            else if (expression[i] == '\\')
            {
                // Lone backslash at end of string
                throw new FilterParseException($"Invalid escape sequence at position {i}", i);
            }
            else
            {
                sb.Append(expression[i]);
                i++;
            }
        }
        
        throw new FilterParseException($"Unterminated string starting at position {startIndex}", startIndex);
    }

    private (Token? token, int nextIndex) ReadToken(string expression, int startIndex)
    {
        var sb = new StringBuilder();
        var i = startIndex;
        
        // Check for Value Path Filter syntax: identities[condition].Value
        if (i < expression.Length && char.IsLetter(expression[i]))
        {
            // Read attribute name (support URN prefixes)
            while (i < expression.Length && 
                   (char.IsLetterOrDigit(expression[i]) || expression[i] == '_' || expression[i] == '.' || expression[i] == ':'))
            {
                sb.Append(expression[i]);
                i++;
            }
            
            // Check for opening bracket
            if (i < expression.Length && expression[i] == '[')
            {
                var attributeName = sb.ToString();
                sb.Clear();
                
                // Read the entire Value Path Filter (quote-aware)
                var bracketCount = 0;
                var inQuotes = false;
                var escaped = false;
                var valuePathFilter = new StringBuilder();
                while (i < expression.Length)
                {
                    var c = expression[i];
                    valuePathFilter.Append(c);

                    // Handle escape sequences so we don't toggle inQuotes on an escaped quote
                    if (!escaped && c == '\\')
                    {
                        escaped = true;
                        i++;
                        continue;
                    }

                    // Toggle quote state when seeing an unescaped double-quote
                    if (!escaped && c == '"')
                        inQuotes = !inQuotes;

                    // Only count brackets when not inside a string literal
                    if (!inQuotes)
                    {
                        if (c == '[')
                        {
                            bracketCount++;
                        }
                        else if (c == ']')
                        {
                            bracketCount--;
                            if (bracketCount == 0)
                            {
                                i++;
                                // Optionally capture a single sub-attribute segment (e.g., ".Value")
                                if (i < expression.Length && expression[i] == '.')
                                {
                                    valuePathFilter.Append('.');
                                    i++;
                                    while (i < expression.Length &&
                                           (char.IsLetterOrDigit(expression[i]) || expression[i] == '_'))
                                    {
                                        valuePathFilter.Append(expression[i]);
                                        i++;
                                    }
                                }
                                break;
                            }
                        }
                    }

                    // Reset escape flag and advance
                    escaped = false;
                    i++;
                }

                // If we exited the loop without closing all brackets, fail fast with a precise error.
                if (bracketCount != 0)
                {
                    throw new FilterParseException($"Unterminated value path filter starting at position {startIndex}", startIndex);
                }
                
                return (new Token(TokenType.ValuePathFilter, $"{attributeName}{valuePathFilter}", startIndex), i);
            }
        }
        
        // Read until whitespace, parentheses, or end of string
        while (i < expression.Length && 
               !char.IsWhiteSpace(expression[i]) && 
               expression[i] != '(' && 
               expression[i] != ')' &&
               expression[i] != '"')
        {
            sb.Append(expression[i]);
            i++;
        }
        
        if (sb.Length == 0)
            return (null, startIndex + 1);
            
        var value = sb.ToString();
        var tokenType = DetermineTokenType(value);
        
        return (new Token(tokenType, value, startIndex), i);
    }

    private TokenType DetermineTokenType(string value)
    {
        // Check if it's a logical operator
        if (_logicalOperators.ContainsKey(value))
            return TokenType.LogicalOperator;
            
        // Check if it's a comparison operator
        if (_comparisonOperators.ContainsKey(value))
            return TokenType.ComparisonOperator;
            
        // Check if it's "not"
        if (value.Equals("not", StringComparison.OrdinalIgnoreCase))
            return TokenType.Not;
            
        // Check if it's "null"
        if (value.Equals("null", StringComparison.OrdinalIgnoreCase))
            return TokenType.Null;
            
        // Check if it's a boolean
        if (value.Equals("true", StringComparison.OrdinalIgnoreCase) || 
            value.Equals("false", StringComparison.OrdinalIgnoreCase))
            return TokenType.Boolean;
            
        // Check if it's a number
        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
            return TokenType.Number;
            
        // Validate identifier (attribute name) format
        if (IsValidIdentifier(value))
            return TokenType.Identifier;
            
        throw new FilterParseException($"Invalid identifier format: '{value}'. Identifiers must start with a letter and contain only letters, numbers, dots, and colons.", 0);
    }

    /// <summary>
    /// Validates that an identifier follows SCIM naming conventions
    /// </summary>
    private bool IsValidIdentifier(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        // Check for consecutive dots (invalid)
        if (value.Contains(".."))
            return false;

        // Check for schema prefix (URN format)
        if (value.StartsWith("urn:"))
        {
            // Validate URN format for SCIM schema prefixes
            // Examples: 
            // - urn:ietf:params:scim:schemas:core:2.0:User:userName
            // - urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:department
            // - urn:ietf:params:scim:schemas:core:2.0:User:name.givenName
            // Allow any valid URN format with at least 6 colons and ending with an attribute name
            var colonCount = value.Count(c => c == ':');
            if (colonCount < 6)
                return false;
                
            // Must end with a valid attribute name
            var lastColonIndex = value.LastIndexOf(':');
            if (lastColonIndex == -1 || lastColonIndex == value.Length - 1)
                return false;
                
            var attributeName = value.Substring(lastColonIndex + 1);
            if (string.IsNullOrEmpty(attributeName))
                return false;
        }
        else
        {
            // Regular attribute name validation
            // Must start with a letter and contain only letters, numbers, dots, and underscores
            // Cannot end with a dot
            if (!AttributePatternRegex.IsMatch(value))
                return false;
        }

        return true;
    }
}

/// <summary>
/// Token types for SCIM filter parsing
/// </summary>
public enum TokenType
{
    Identifier,
    ComparisonOperator,
    LogicalOperator,
    String,
    Number,
    Boolean,
    Null,
    Not,
    LeftParen,
    RightParen,
    ValuePathFilter
}

/// <summary>
/// Represents a token in the filter expression
/// </summary>
public class Token
{
    public TokenType Type { get; }
    public string Value { get; }
    public int Position { get; }

    public Token(TokenType type, string value, int position)
    {
        Type = type;
        Value = value;
        Position = position;
    }

    public override string ToString() => $"{Type}: {Value}";
}

/// <summary>
/// Parser that converts tokens into AST nodes
/// </summary>
internal class TokenParser
{
    private readonly List<Token> _tokens;
    private readonly Dictionary<string, ComparisonOperator> _comparisonOperators;
    private readonly Dictionary<string, BinaryOperator> _logicalOperators;
    private int _position;

    public TokenParser(List<Token> tokens, 
                      Dictionary<string, ComparisonOperator> comparisonOperators,
                      Dictionary<string, BinaryOperator> logicalOperators)
    {
        _tokens = tokens;
        _comparisonOperators = comparisonOperators;
        _logicalOperators = logicalOperators;
        _position = 0;
    }

    public IAstNode ParseExpression()
    {
        return ParseOrExpression();
    }

    private IAstNode ParseOrExpression()
    {
        var left = ParseAndExpression();
        
        while (CurrentToken?.Type == TokenType.LogicalOperator && 
               CurrentToken.Value.Equals("or", StringComparison.OrdinalIgnoreCase))
        {
            Advance(); // Skip "or"
            var right = ParseAndExpression();
            left = new BinaryExpressionNode(left, right, BinaryOperator.Or);
        }
        
        return left;
    }

    private IAstNode ParseAndExpression()
    {
        var left = ParseNotExpression();
        
        while (CurrentToken?.Type == TokenType.LogicalOperator && 
               CurrentToken.Value.Equals("and", StringComparison.OrdinalIgnoreCase))
        {
            Advance(); // Skip "and"
            var right = ParseNotExpression();
            left = new BinaryExpressionNode(left, right, BinaryOperator.And);
        }
        
        return left;
    }

    private IAstNode ParseNotExpression()
    {
        if (CurrentToken?.Type == TokenType.Not)
        {
            Advance(); // Skip "not"
            // Allow chaining: not not A  => NOT (NOT A)
            var operand = ParseNotExpression();
            return new UnaryExpressionNode(operand, UnaryOperator.Not);
        }
        return ParsePrimaryExpression();
    }

    private IAstNode ParsePrimaryExpression()
    {
        if (CurrentToken?.Type == TokenType.LeftParen)
        {
            Advance(); // Skip "("
            var expression = ParseOrExpression();
            
            if (CurrentToken?.Type != TokenType.RightParen)
                throw new FilterParseException($"Expected ')' at position {CurrentPosition}");
                
            Advance(); // Skip ")"
            return new ParenthesizedExpressionNode(expression);
        }
        
        return ParseComparison();
    }

    private IAstNode ParseComparison()
    {
        var attribute = ParseAttribute();
        
        if (CurrentToken?.Type == TokenType.ComparisonOperator)
        {
            var operatorToken = CurrentToken;
            Advance();
            
            if (operatorToken.Value.Equals("pr", StringComparison.OrdinalIgnoreCase))
            {
                // Present operator doesn't need a value
                var presentValue = new LiteralValueNode("present", LiteralType.String);
                return new ComparisonExpressionNode(attribute, ComparisonOperator.Present, presentValue);
            }
            else
            {
                var value = ParseValue();
                var comparisonOp = _comparisonOperators[operatorToken.Value];
                return new ComparisonExpressionNode(attribute, comparisonOp, value);
            }
        }
        
        throw new FilterParseException($"Expected comparison operator at position {CurrentPosition}");
    }

    private IdentifierNode ParseAttribute()
    {
        if (CurrentToken?.Type != TokenType.Identifier && CurrentToken?.Type != TokenType.ValuePathFilter)
            throw new FilterParseException($"Expected attribute name at position {CurrentPosition}");
        
        var attributeValue = CurrentToken.Value;
        Advance();
        
        // Handle Value Path Filter
        if (attributeValue.Contains("[") && attributeValue.Contains("]"))
        {
            return ParseValuePathFilterAttribute(attributeValue);
        }
        
        // Parse complex attribute paths with schema prefixes and sub-attributes
        return ParseComplexAttributePath(attributeValue);
    }

    private IdentifierNode ParseValuePathFilterAttribute(string attributeValue)
    {
        // Extract main attribute name (before the bracket)
        var bracketIndex = attributeValue.IndexOf('[');
        var mainAttribute = attributeValue.Substring(0, bracketIndex);
        
        // Extract the condition inside brackets
        var startBracket = attributeValue.IndexOf('[');
        var endBracket = attributeValue.LastIndexOf(']');
        var condition = attributeValue.Substring(startBracket + 1, endBracket - startBracket - 1);
        
        // Extract sub-attribute (after the closing bracket)
        var subAttribute = "";
        if (endBracket + 1 < attributeValue.Length && attributeValue[endBracket + 1] == '.')
        {
            var dotIndex = attributeValue.IndexOf('.', endBracket);
            if (dotIndex > 0)
            {
                subAttribute = attributeValue.Substring(dotIndex + 1);
            }
        }
        
        return new IdentifierNode(mainAttribute)
        {
            SubAttribute = subAttribute,
            ValuePathFilter = condition
        };
    }

    /// <summary>
    /// Parse complex attribute paths supporting:
    /// - Simple attributes: userName
    /// - Sub-attributes: name.givenName
    /// - Schema prefixes: urn:ietf:params:scim:schemas:core:2.0:User:userName
    /// - Complex paths: urn:ietf:params:scim:schemas:core:2.0:User:name.givenName
    /// </summary>
    private IdentifierNode ParseComplexAttributePath(string attributeValue)
    {
        string? schemaPrefix = null;
        string attributeName;
        string? subAttribute = null;
        
        // Check for schema prefix (contains "urn:" and ends with ":")
        if (attributeValue.StartsWith("urn:"))
        {
            var lastColonIndex = attributeValue.LastIndexOf(':');
            if (lastColonIndex > 0 && lastColonIndex < attributeValue.Length - 1)
            {
                schemaPrefix = attributeValue.Substring(0, lastColonIndex);
                attributeValue = attributeValue.Substring(lastColonIndex + 1);
            }
        }
        
        // Check for sub-attribute (contains ".")
        if (attributeValue.Contains('.'))
        {
            var dotIndex = attributeValue.IndexOf('.');
            attributeName = attributeValue.Substring(0, dotIndex);
            subAttribute = attributeValue.Substring(dotIndex + 1);
        }
        else
        {
            attributeName = attributeValue;
        }
        
        return new IdentifierNode(attributeName)
        {
            SchemaPrefix = schemaPrefix,
            SubAttribute = subAttribute
        };
    }

    private IAstNode ParseValue()
    {
        if (CurrentToken == null)
            throw new FilterParseException($"Expected value at position {CurrentPosition}");
        
        switch (CurrentToken.Type)
        {
            case TokenType.String:
                var stringValue = CurrentToken.Value;
                Advance();
                return new LiteralValueNode(stringValue, LiteralType.String);
                
            case TokenType.Number:
                var numberValue = CurrentToken.Value;
                Advance();
                
                if (decimal.TryParse(numberValue, 
                                     NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                                     CultureInfo.InvariantCulture,
                                     out var decimalVal))
                {
                    var isInteger = decimal.Truncate(decimalVal) == decimalVal;
                    if (isInteger && decimalVal >= int.MinValue && decimalVal <= int.MaxValue)
                        return new LiteralValueNode((int)decimalVal, LiteralType.Integer);
                    return new LiteralValueNode(decimalVal, LiteralType.Decimal);
                }
                throw new FilterParseException($"Invalid number format: {numberValue}");
                
            case TokenType.Boolean:
                var boolValue = CurrentToken.Value.Equals("true", StringComparison.OrdinalIgnoreCase);
                Advance();
                return new LiteralValueNode(boolValue, LiteralType.Boolean);
                
            case TokenType.Null:
                Advance();
                return new LiteralValueNode(null, LiteralType.Null);
                
            default:
                throw new FilterParseException($"Unexpected token type {CurrentToken.Type} at position {CurrentPosition}");
        }
    }

    private Token? CurrentToken => _position < _tokens.Count ? _tokens[_position] : null;
    private int CurrentPosition => CurrentToken?.Position ?? (_tokens.LastOrDefault()?.Position + 1 ?? 0);

    private void Advance()
    {
        if (_position < _tokens.Count)
            _position++;
    }
}

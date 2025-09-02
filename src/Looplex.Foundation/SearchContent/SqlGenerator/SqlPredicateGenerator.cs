using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Looplex.Foundation.SearchContent.AST;
using Looplex.Foundation.SearchContent.AST.Nodes;

namespace Looplex.Foundation.SearchContent.SqlGenerator;

/// <summary>
/// Generates SQL predicates from SCIM filter AST nodes
/// 
/// Converts SCIM filter expressions into parameterized SQL WHERE clauses
/// Supports all SCIM operators and advanced features like sub-attributes and schema prefixes
/// </summary>
public class SqlPredicateGenerator : ISqlPredicateGenerator, IAstVisitor<string>
{
    private SqlGenerationOptions _options = new();
    private readonly object _lockObject = new object();
    private Dictionary<string, object?> _parameters = new();
    private int _parameterCounter = 0;
    private int _recursionDepth = 0;
    private const int MAX_RECURSION_DEPTH = 100;

    public SqlPredicateResult GeneratePredicate(IAstNode astNode)
    {
        return GeneratePredicate(astNode, new SqlGenerationOptions());
    }

    public SqlPredicateResult GeneratePredicate(IAstNode astNode, Dictionary<string, string> fieldMapping)
    {
        var options = new SqlGenerationOptions { FieldMapping = fieldMapping };
        return GeneratePredicate(astNode, options);
    }

    public SqlPredicateResult GeneratePredicate(IAstNode astNode, string tableAlias)
    {
        var options = new SqlGenerationOptions { TableAlias = tableAlias };
        return GeneratePredicate(astNode, options);
    }

    public SqlPredicateResult GeneratePredicate(IAstNode astNode, SqlGenerationOptions options)
    {
        lock (_lockObject)
        {
            _options = options ?? new SqlGenerationOptions();
            _parameters = new Dictionary<string, object?>();
            _parameterCounter = 0;
            _recursionDepth = 0;

            var sql = astNode.Accept(this);

            return new SqlPredicateResult
            {
                Sql = sql,
                Parameters = new Dictionary<string, object?>(_parameters)
            };
        }
    }

    public string VisitBinaryExpression(BinaryExpressionNode node)
    {
        // Check recursion depth to prevent stack overflow
        if (++_recursionDepth > MAX_RECURSION_DEPTH)
        {
            throw new InvalidOperationException($"Maximum recursion depth ({MAX_RECURSION_DEPTH}) exceeded. Expression too complex.");
        }

        try
        {
            var left = node.Left.Accept(this);
            var right = node.Right.Accept(this);
            
            var operatorSymbol = node.Operator switch
            {
                BinaryOperator.And => "AND",
                BinaryOperator.Or => "OR",
                _ => throw new ArgumentException($"Unsupported binary operator: {node.Operator}")
            };

            return $"({left} {operatorSymbol} {right})";
        }
        finally
        {
            _recursionDepth--;
        }
    }

    public string VisitUnaryExpression(UnaryExpressionNode node)
    {
        // Check recursion depth to prevent stack overflow
        if (++_recursionDepth > MAX_RECURSION_DEPTH)
        {
            throw new InvalidOperationException($"Maximum recursion depth ({MAX_RECURSION_DEPTH}) exceeded. Expression too complex.");
        }

        try
        {
            var operand = node.Operand.Accept(this);
            
            return node.Operator switch
            {
                UnaryOperator.Not => $"NOT ({operand})",
                _ => throw new ArgumentException($"Unsupported unary operator: {node.Operator}")
            };
        }
        finally
        {
            _recursionDepth--;
        }
    }

    public string VisitComparisonExpression(ComparisonExpressionNode node)
    {
        var fieldName = GetSqlFieldName(node.Attribute);
        var parameterName = GetNextParameterName();
        
        // Handle Value Path Filter EXISTS queries specially
        if (fieldName.StartsWith("EXISTS", StringComparison.OrdinalIgnoreCase))
        {
            var vpfValueNode = node.Value as LiteralValueNode;
            var rawValue = GetParameterValue(vpfValueNode);

            // Strip the closing ')' to append additional predicate
            var closeIdx = fieldName.LastIndexOf(')');
            var baseExists = closeIdx >= 0
                ? fieldName.Substring(0, closeIdx)
                : fieldName;

            // Try to extract the table alias used inside EXISTS (defaults to 'i')
            var alias = ExtractExistsAlias(fieldName) ?? "i";
            var columnExpr = _options.CaseSensitive
                ? $"{alias}.dsValor"
                : $"LOWER({alias}.dsValor)";

            if (_options.UseParameters)
            {
                var paramName = GetNextParameterName();
                object? paramVal = rawValue;
                if (paramVal is string s)
                {
                    paramVal = node.Operator switch
                    {
                        ComparisonOperator.Contains    => $"%{s}%",
                        ComparisonOperator.StartsWith   => $"{s}%",
                        ComparisonOperator.EndsWith     => $"%{s}",
                        _                               => s
                    };
                }
                lock (_lockObject) { _parameters[paramName] = paramVal; }

                var rhs = _options.CaseSensitive
                    ? $"@{paramName}"
                    : $"LOWER(@{paramName})";
                var op = node.Operator switch
                {
                    ComparisonOperator.Equal       => "=",
                    ComparisonOperator.NotEqual    => "!=",
                    ComparisonOperator.Contains    => "LIKE",
                    ComparisonOperator.StartsWith   => "LIKE",
                    ComparisonOperator.EndsWith     => "LIKE",
                    _                               => throw new ArgumentException($"Unsupported operator for Value Path Filter: {node.Operator}")
                };
                return $"{baseExists} AND {columnExpr} {op} {rhs})";
            }
            else
            {
                var inlined = rawValue;
                if (inlined is string s)
                {
                    inlined = node.Operator switch
                    {
                        ComparisonOperator.Contains    => $"%{s}%",
                        ComparisonOperator.StartsWith   => $"{s}%",
                        ComparisonOperator.EndsWith     => $"%{s}",
                        _                               => s
                    };
                }
                var rhs = EscapeValue(inlined);
                if (!_options.CaseSensitive)
                {
                    rhs = $"LOWER({rhs})";
                }
                var op = node.Operator switch
                {
                    ComparisonOperator.Equal       => "=",
                    ComparisonOperator.NotEqual    => "!=",
                    ComparisonOperator.Contains    => "LIKE",
                    ComparisonOperator.StartsWith   => "LIKE",
                    ComparisonOperator.EndsWith     => "LIKE",
                    _                               => throw new ArgumentException($"Unsupported operator for Value Path Filter: {node.Operator}")
                };
                return $"{baseExists} AND {columnExpr} {op} {rhs})";
            }
        }
        
        // Handle present operator specially
        if (node.Operator == ComparisonOperator.Present)
        {
            if (_options.IncludeNullChecks)
            {
                return $"({fieldName} IS NOT NULL AND {fieldName} != '')";
            }
            return $"{fieldName} IS NOT NULL";
        }
        
        // Handle null values
        var valueNode = node.Value as LiteralValueNode;
        if (valueNode?.IsNull == true)
        {
            return node.Operator switch
            {
                ComparisonOperator.Equal => $"{fieldName} IS NULL",
                ComparisonOperator.NotEqual => $"{fieldName} IS NOT NULL",
                _ => throw new ArgumentException($"Null values not supported with operator: {node.Operator}")
            };
        }

        // Get the value
        var value = GetParameterValue(valueNode);
        
        if (_options.UseParameters)
        {
            // Add to parameters for parameterized SQL (thread-safe)
            lock (_lockObject)
            {
                _parameters[parameterName] = value;
            }

            // Generate parameterized SQL based on operator
            return node.Operator switch
            {
                ComparisonOperator.Equal => GenerateEqualExpression(fieldName, parameterName, value),
                ComparisonOperator.NotEqual => GenerateNotEqualExpression(fieldName, parameterName, value),
                ComparisonOperator.Contains => GenerateContainsExpression(fieldName, parameterName),
                ComparisonOperator.StartsWith => GenerateStartsWithExpression(fieldName, parameterName),
                ComparisonOperator.EndsWith => GenerateEndsWithExpression(fieldName, parameterName),
                ComparisonOperator.GreaterThan => $"{fieldName} > @{parameterName}",
                ComparisonOperator.GreaterThanOrEqual => $"{fieldName} >= @{parameterName}",
                ComparisonOperator.LessThan => $"{fieldName} < @{parameterName}",
                ComparisonOperator.LessThanOrEqual => $"{fieldName} <= @{parameterName}",
                _ => throw new ArgumentException($"Unsupported comparison operator: {node.Operator}")
            };
        }
        else
        {
            // Generate inline SQL with escaped values
            return node.Operator switch
            {
                ComparisonOperator.Equal => GenerateInlineEqualExpression(fieldName, value),
                ComparisonOperator.NotEqual => GenerateInlineNotEqualExpression(fieldName, value),
                ComparisonOperator.Contains => GenerateInlineContainsExpression(fieldName, value),
                ComparisonOperator.StartsWith => GenerateInlineStartsWithExpression(fieldName, value),
                ComparisonOperator.EndsWith => GenerateInlineEndsWithExpression(fieldName, value),
                ComparisonOperator.GreaterThan => GenerateInlineComparisonExpression(fieldName, ">", value),
                ComparisonOperator.GreaterThanOrEqual => GenerateInlineComparisonExpression(fieldName, ">=", value),
                ComparisonOperator.LessThan => GenerateInlineComparisonExpression(fieldName, "<", value),
                ComparisonOperator.LessThanOrEqual => GenerateInlineComparisonExpression(fieldName, "<=", value),
                _ => throw new ArgumentException($"Unsupported comparison operator: {node.Operator}")
            };
        }
    }

    public string VisitParenthesizedExpression(ParenthesizedExpressionNode node)
    {
        return $"({node.Expression.Accept(this)})";
    }

    public string VisitIdentifier(IdentifierNode node)
    {
        return GetSqlFieldName(node);
    }

    public string VisitLiteralValue(LiteralValueNode node)
    {
        var parameterName = GetNextParameterName();
        var value = GetParameterValue(node);
        _parameters[parameterName] = value;
        return $"@{parameterName}";
    }

    /// <summary>
    /// Generate SQL field name from AST identifier (AST PURE - SQL logic moved from AST to generator)
    /// </summary>
    private string GetSqlFieldName(IdentifierNode field)
    {
        var fieldName = field.Name;

        // Handle Value Path Filters (e.g., identities[IdentityProviderType eq "CPF"].Value)
        if (!string.IsNullOrEmpty(field.ValuePathFilter))
        {
            return ProcessValuePathFilter(field.Name, field.ValuePathFilter, field.SubAttribute);
        }

        // Handle schema prefix (ignore for SQL generation)
        // Schema prefixes are typically used for SCIM protocol, not database queries

        // First, check if there's a direct mapping for the full attribute path (e.g., "Owner.Name")
        var fullAttributePath = fieldName;
        if (!string.IsNullOrEmpty(field.SubAttribute))
        {
            fullAttributePath = $"{fieldName}.{field.SubAttribute}";
        }

        // Check for custom field mapping with the full path first
        if (_options.FieldMapping.ContainsKey(fullAttributePath))
        {
            fieldName = _options.FieldMapping[fullAttributePath];
        }
        else
        {
            // Fallback to sub-attribute conversion (e.g., name.givenName -> name_givenName)
            if (!string.IsNullOrEmpty(field.SubAttribute))
            {
                fieldName = $"{fieldName}_{field.SubAttribute}";
            }

            // Check for custom field mapping with the converted name
            if (_options.FieldMapping.ContainsKey(fieldName))
            {
                fieldName = _options.FieldMapping[fieldName];
            }
        }

        // Add table alias if specified
        if (!string.IsNullOrEmpty(_options.TableAlias))
        {
            fieldName = $"{_options.TableAlias}.{fieldName}";
        }

        return fieldName;
    }

    /// <summary>
    /// Process Value Path Filters like identities[IdentityProviderType eq "CPF"].Value
    /// </summary>
    private string ProcessValuePathFilter(string mainAttribute, string condition, string? subAttribute)
    {
        // Parse the condition (e.g., "IdentityProviderType eq \"CPF\"")
        var conditionParts = ParseValuePathCondition(condition);
        
        // Generate EXISTS subquery
        return GenerateValuePathExistsQuery(mainAttribute, conditionParts, subAttribute ?? "");
    }

    /// <summary>
    /// Parse condition inside Value Path Filter brackets
    /// </summary>
    private (string attribute, string op, string value) ParseValuePathCondition(string condition)
    {
        // Simple parsing for conditions like "IdentityProviderType eq \"CPF\""
        var parts = condition.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 3)
        {
            var attribute = parts[0];
            var op = parts[1];
            var value = parts[2].Trim('"');
            return (attribute, op, value);
        }
        
        throw new ArgumentException($"Invalid Value Path Filter condition: {condition}");
    }

    /// <summary>
    /// Generate EXISTS subquery for Value Path Filters
    /// </summary>
    private string GenerateValuePathExistsQuery(string mainAttribute, (string attribute, string op, string value) condition, string subAttribute)
    {
        // Map main attribute to table alias
        var tableAlias = mainAttribute.ToLower() switch
        {
            "identities" => "i",
            "owner.identities" => "oi",
            "caseclient.identities" => "cci",
            "participants.identities" => "pi",
            _ => "i" // default
        };
        
        // Map condition attribute to column
        var columnName = condition.attribute.ToLower() switch
        {
            "identityprovidertype" => "dsTipoProvedor",
            "value" => "dsValor",
            "id" => "id",
            _ => condition.attribute
        };
        
        // Map operator
        var sqlOperator = condition.op.ToLower() switch
        {
            "eq" => "=",
            "ne" => "!=",
            "co" => "LIKE",
            "sw" => "LIKE",
            "ew" => "LIKE",
            _ => condition.op
        };
        
        // Use parameters to prevent SQL injection
        if (_options.UseParameters)
        {
            var paramName = GetNextParameterName();
            var paramValue = condition.op.ToLower() switch
            {
                "co" => $"%{condition.value}%",
                "sw" => $"{condition.value}%",
                "ew" => $"%{condition.value}",
                _ => condition.value
            };
            
            lock (_lockObject) { _parameters[paramName] = paramValue; }
            
            // Generate EXISTS subquery with parameter
            var existsQuery = $"EXISTS (SELECT 1 FROM TIdentidade {tableAlias} WHERE {tableAlias}.cdProcesso = p.cdProcesso AND {tableAlias}.{columnName} {sqlOperator} @{paramName}";
            
            // Add sub-attribute condition if specified
            if (!string.IsNullOrEmpty(subAttribute))
            {
                var subColumnName = subAttribute.ToLower() switch
                {
                    "value" => "dsValor",
                    "id" => "id",
                    _ => subAttribute
                };
                existsQuery += $" AND {tableAlias}.{subColumnName} IS NOT NULL";
            }
            
            existsQuery += ")";
            return existsQuery;
        }
        else
        {
            // Escape value for inline SQL (less secure, but maintains backward compatibility)
            var escapedValue = EscapeValue(condition.op.ToLower() switch
            {
                "co" => $"%{condition.value}%",
                "sw" => $"{condition.value}%",
                "ew" => $"%{condition.value}",
                _ => condition.value
            });
            
            // Generate EXISTS subquery with escaped value
            var existsQuery = $"EXISTS (SELECT 1 FROM TIdentidade {tableAlias} WHERE {tableAlias}.cdProcesso = p.cdProcesso AND {tableAlias}.{columnName} {sqlOperator} {escapedValue}";
            
            // Add sub-attribute condition if specified
            if (!string.IsNullOrEmpty(subAttribute))
            {
                var subColumnName = subAttribute.ToLower() switch
                {
                    "value" => "dsValor",
                    "id" => "id",
                    _ => subAttribute
                };
                existsQuery += $" AND {tableAlias}.{subColumnName} IS NOT NULL";
            }
            
            existsQuery += ")";
            return existsQuery;
        }
    }

    private string GenerateEqualExpression(string fieldName, string parameterName, object? value)
    {
        if (!_options.CaseSensitive && value is string)
        {
            return _options.Dialect switch
            {
                SqlDialect.SqlServer => $"LOWER({fieldName}) = LOWER(@{parameterName})",
                SqlDialect.PostgreSql => $"LOWER({fieldName}) = LOWER(@{parameterName})",
                SqlDialect.MySql => $"LOWER({fieldName}) = LOWER(@{parameterName})",
                SqlDialect.SQLite => $"LOWER({fieldName}) = LOWER(@{parameterName})",
                SqlDialect.Oracle => $"LOWER({fieldName}) = LOWER(@{parameterName})",
                _ => $"{fieldName} = @{parameterName}"
            };
        }
        
        return $"{fieldName} = @{parameterName}";
    }

    private string GenerateNotEqualExpression(string fieldName, string parameterName, object? value)
    {
        if (!_options.CaseSensitive && value is string)
        {
            return _options.Dialect switch
            {
                SqlDialect.SqlServer => $"LOWER({fieldName}) != LOWER(@{parameterName})",
                SqlDialect.PostgreSql => $"LOWER({fieldName}) != LOWER(@{parameterName})",
                SqlDialect.MySql => $"LOWER({fieldName}) != LOWER(@{parameterName})",
                SqlDialect.SQLite => $"LOWER({fieldName}) != LOWER(@{parameterName})",
                SqlDialect.Oracle => $"LOWER({fieldName}) != LOWER(@{parameterName})",
                _ => $"{fieldName} != @{parameterName}"
            };
        }
        
        return $"{fieldName} != @{parameterName}";
    }

    private string GenerateContainsExpression(string fieldName, string parameterName)
    {
        // Update parameter value to include wildcards
        if (_parameters.TryGetValue(parameterName, out var value) && value is string stringValue)
        {
            _parameters[parameterName] = $"%{stringValue}%";
        }

        if (!_options.CaseSensitive)
        {
            return _options.Dialect switch
            {
                SqlDialect.SqlServer => $"LOWER({fieldName}) LIKE LOWER(@{parameterName})",
                SqlDialect.PostgreSql => $"LOWER({fieldName}) LIKE LOWER(@{parameterName})",
                SqlDialect.MySql => $"LOWER({fieldName}) LIKE LOWER(@{parameterName})",
                SqlDialect.SQLite => $"LOWER({fieldName}) LIKE LOWER(@{parameterName})",
                SqlDialect.Oracle => $"LOWER({fieldName}) LIKE LOWER(@{parameterName})",
                SqlDialect.Standard => $"LOWER({fieldName}) LIKE LOWER(@{parameterName})",
                _ => $"LOWER({fieldName}) LIKE LOWER(@{parameterName})"
            };
        }
        
        return $"{fieldName} LIKE @{parameterName}";
    }

    private string GenerateStartsWithExpression(string fieldName, string parameterName)
    {
        // Update parameter value to include trailing wildcard
        if (_parameters.TryGetValue(parameterName, out var value) && value is string stringValue)
        {
            _parameters[parameterName] = $"{stringValue}%";
        }

        if (!_options.CaseSensitive)
        {
            return _options.Dialect switch
            {
                SqlDialect.SqlServer => $"LOWER({fieldName}) LIKE LOWER(@{parameterName})",
                SqlDialect.PostgreSql => $"LOWER({fieldName}) LIKE LOWER(@{parameterName})",
                SqlDialect.MySql => $"LOWER({fieldName}) LIKE LOWER(@{parameterName})",
                SqlDialect.SQLite => $"LOWER({fieldName}) LIKE LOWER(@{parameterName})",
                SqlDialect.Oracle => $"LOWER({fieldName}) LIKE LOWER(@{parameterName})",
                SqlDialect.Standard => $"LOWER({fieldName}) LIKE LOWER(@{parameterName})",
                _ => $"LOWER({fieldName}) LIKE LOWER(@{parameterName})"
            };
        }
        
        return $"{fieldName} LIKE @{parameterName}";
    }

    private string GenerateEndsWithExpression(string fieldName, string parameterName)
    {
        // Update parameter value to include leading wildcard
        if (_parameters.TryGetValue(parameterName, out var value) && value is string stringValue)
        {
            _parameters[parameterName] = $"%{stringValue}";
        }

        if (!_options.CaseSensitive)
        {
            return _options.Dialect switch
            {
                SqlDialect.SqlServer => $"LOWER({fieldName}) LIKE LOWER(@{parameterName})",
                SqlDialect.PostgreSql => $"LOWER({fieldName}) LIKE LOWER(@{parameterName})",
                SqlDialect.MySql => $"LOWER({fieldName}) LIKE LOWER(@{parameterName})",
                SqlDialect.SQLite => $"LOWER({fieldName}) LIKE LOWER(@{parameterName})",
                SqlDialect.Oracle => $"LOWER({fieldName}) LIKE LOWER(@{parameterName})",
                SqlDialect.Standard => $"LOWER({fieldName}) LIKE LOWER(@{parameterName})",
                _ => $"LOWER({fieldName}) LIKE LOWER(@{parameterName})"
            };
        }
        
        return $"{fieldName} LIKE @{parameterName}";
    }

    /// <summary>
    /// Extract parameter value from AST node (AST PURE - no SQL logic in AST)
    /// </summary>
    private object? GetParameterValue(LiteralValueNode? valueNode)
    {
        if (valueNode == null) 
            return null;
            
        return valueNode.Type switch
        {
            LiteralType.String => valueNode.Value?.ToString(),
            LiteralType.Integer => valueNode.Value,
            LiteralType.Decimal => valueNode.Value,
            LiteralType.Boolean => valueNode.Value,
            LiteralType.Null => null,
            _ => valueNode.Value?.ToString()
        };
    }

    private string GetNextParameterName()
    {
        return $"{_options.ParameterPrefix}{++_parameterCounter}";
    }

    // Inline SQL generation methods for stored procedure compatibility
    private string GenerateInlineEqualExpression(string fieldName, object? value)
    {
        var escapedValue = EscapeValue(value);
        if (!_options.CaseSensitive && value is string)
        {
            return $"LOWER({fieldName}) = LOWER({escapedValue})";
        }
        return $"{fieldName} = {escapedValue}";
    }

    private string GenerateInlineNotEqualExpression(string fieldName, object? value)
    {
        var escapedValue = EscapeValue(value);
        if (!_options.CaseSensitive && value is string)
        {
            return $"LOWER({fieldName}) != LOWER({escapedValue})";
        }
        return $"{fieldName} != {escapedValue}";
    }

    private string GenerateInlineContainsExpression(string fieldName, object? value)
    {
        var escapedValue = EscapeValue(value is string str ? $"%{str}%" : value);
        if (!_options.CaseSensitive)
        {
            return $"LOWER({fieldName}) LIKE LOWER({escapedValue})";
        }
        return $"{fieldName} LIKE {escapedValue}";
    }

    private string GenerateInlineStartsWithExpression(string fieldName, object? value)
    {
        var escapedValue = EscapeValue(value is string str ? $"{str}%" : value);
        if (!_options.CaseSensitive)
        {
            return $"LOWER({fieldName}) LIKE LOWER({escapedValue})";
        }
        return $"{fieldName} LIKE {escapedValue}";
    }

    private static string? ExtractExistsAlias(string existsSql)
    {
        const string marker = "FROM TIdentidade ";
        var idx = existsSql.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        idx += marker.Length;
        // alias ends at first whitespace or ')'
        var end = existsSql.IndexOfAny(new[] { ' ', ')', '\n', '\r', '\t' }, idx);
        end = end < 0 ? existsSql.Length : end;
        var alias = existsSql.Substring(idx, end - idx).Trim();
        return string.IsNullOrEmpty(alias) ? null : alias;
    }

    private string GenerateInlineEndsWithExpression(string fieldName, object? value)
    {
        var escapedValue = EscapeValue(value is string str ? $"%{str}" : value);
        if (!_options.CaseSensitive)
        {
            return $"LOWER({fieldName}) LIKE LOWER({escapedValue})";
        }
        return $"{fieldName} LIKE {escapedValue}";
    }

    private string GenerateInlineComparisonExpression(string fieldName, string operatorSymbol, object? value)
    {
        var escapedValue = EscapeValue(value);
        return $"{fieldName} {operatorSymbol} {escapedValue}";
    }

    private string EscapeValue(object? value)
    {
        if (value == null)
            return "NULL";

        if (value is string stringValue)
        {
            // Check if the string represents a numeric value (for enum conversions)
            if (int.TryParse(stringValue, out var intValue))
            {
                return intValue.ToString();
            }
            
            if (decimal.TryParse(stringValue, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var decimalValue))
            {
                return decimalValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            if (_options.EscapeStrings)
            {
                // Escape single quotes by doubling them
                var escaped = stringValue.Replace("'", "''");
                return $"'{escaped}'";
            }
            return $"'{stringValue}'";
        }

        if (value is bool boolValue)
            return boolValue ? "1" : "0";

        if (value is DateTime dateTimeValue)
            return $"'{dateTimeValue:yyyy-MM-ddTHH:mm:ss.fffZ}'";

        // For numbers and other types, return as string
        return value.ToString() ?? "NULL";
    }
}


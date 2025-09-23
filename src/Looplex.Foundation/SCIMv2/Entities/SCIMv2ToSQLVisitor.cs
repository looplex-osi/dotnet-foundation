using System;
using System.Collections.Generic;

using Looplex.Foundation.SCIMv2.Antlr;

namespace Looplex.Foundation.SCIMv2.Entities;

public class SCIMv2ToSQLVisitor : ScimFilterBaseVisitor<(string Sql, Dictionary<string, object> Parameters)>
{
  public HashSet<string>? AllowedAttributes { set; get; }
  public IDictionary<string, string>? AttributeMapper { set; get; }
  
  private int _parameterIndex = 0;
  private Dictionary<string, object> _parameters = new();
  
  public override (string Sql, Dictionary<string, object> Parameters) VisitOperatorExp(ScimFilterParser.OperatorExpContext context)
  {
    var attr = context.attrPath().GetText();
    var op = context.COMPAREOPERATOR().GetText().ToLower();
    var value = context.VALUE().GetText();

    // Validar atributo
    if (!AllowedAttributes?.Contains(attr) ?? false)
      throw new InvalidOperationException($"Cannot filter by {attr}");

    // Mapear atributo
    if (AttributeMapper?.TryGetValue(attr, out var mapped) ?? false)
      attr = mapped;

    // Gerar parâmetro
    var paramName = $"@param_{_parameterIndex++}";
    var sanitizedValue = SanitizeValue(value, op);
    _parameters[paramName] = sanitizedValue;

    // Gerar SQL com parâmetro
    string sqlOp = op switch
    {
      "eq" => "=",
      "ne" => "!=",
      "co" => "LIKE",
      "sw" => "LIKE",
      "ew" => "LIKE",
      "gt" => ">",
      "ge" => ">=",
      "lt" => "<",
      "le" => "<=",
      _ => throw new NotImplementedException($"Operator {op} not implemented")
    };

    return ($"{attr} {sqlOp} {paramName}", _parameters);
  }

  public override (string Sql, Dictionary<string, object> Parameters) VisitPresentExp(ScimFilterParser.PresentExpContext context)
  {
    var attr = context.attrPath().GetText();
    
    // Validar atributo
    if (!AllowedAttributes?.Contains(attr) ?? false)
      throw new InvalidOperationException($"Cannot filter by {attr}");

    // Mapear atributo
    if (AttributeMapper?.TryGetValue(attr, out var mapped) ?? false)
      attr = mapped;
    
    return ($"{attr} IS NOT NULL", _parameters);
  }

  public override (string Sql, Dictionary<string, object> Parameters) VisitAndExp(ScimFilterParser.AndExpContext context)
  {
    var left = Visit(context.filter(0));
    var right = Visit(context.filter(1));
    
    // Merge parameters from both sides
    foreach (var param in right.Parameters)
    {
      _parameters[param.Key] = param.Value;
    }
    
    return ($"({left.Sql} AND {right.Sql})", _parameters);
  }

  public override (string Sql, Dictionary<string, object> Parameters) VisitOrExp(ScimFilterParser.OrExpContext context)
  {
    var left = Visit(context.filter(0));
    var right = Visit(context.filter(1));
    
    // Merge parameters from both sides
    foreach (var param in right.Parameters)
    {
      _parameters[param.Key] = param.Value;
    }
    
    return ($"({left.Sql} OR {right.Sql})", _parameters);
  }

  public override (string Sql, Dictionary<string, object> Parameters) VisitBraceExp(ScimFilterParser.BraceExpContext context)
  {
    var inner = Visit(context.filter());
    return context.NOT() != null ? ($"NOT ({inner.Sql})", inner.Parameters) : ($"({inner.Sql})", inner.Parameters);
  }

  public override (string Sql, Dictionary<string, object> Parameters) VisitValPathExp(ScimFilterParser.ValPathExpContext context)
  {
    var attr = context.attrPath().GetText();
    var condition = this.Visit(context.valPathFilter());
    
    // Merge parameters from condition
    foreach (var param in condition.Parameters)
    {
      _parameters[param.Key] = param.Value;
    }
    
    return ($"EXISTS (SELECT 1 FROM {attr} x WHERE {condition.Sql})", _parameters);
  }


  private static object SanitizeValue(string value, string operation)
  {
    if (string.IsNullOrEmpty(value))
      return DBNull.Value;

    // Remover aspas e escapar caracteres perigosos
    var cleanValue = value.Trim('"', '\'');
    
    // Escapar caracteres SQL perigosos
    cleanValue = cleanValue.Replace("'", "''")
                          .Replace(";", "")
                          .Replace("--", "")
                          .Replace("/*", "")
                          .Replace("*/", "");

    return operation switch
    {
      "co" => $"%{cleanValue}%",
      "sw" => $"{cleanValue}%",
      "ew" => $"%{cleanValue}",
      _ => double.TryParse(cleanValue, out _) ? Convert.ToDecimal(cleanValue) : cleanValue
    };
  }

}
using System;

using Looplex.Foundation.SCIMv2.Antlr;

namespace Looplex.Foundation.SCIMv2.Entities;

public class SCIMv2ToSQLVisitor : ScimFilterBaseVisitor<string>
{
  public override string VisitOperatorExp(ScimFilterParser.OperatorExpContext context)
  {
    var attr = context.attrPath().GetText();
    var op = context.COMPAREOPERATOR().GetText().ToLower();
    var value = context.VALUE().GetText();

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

    if (op == "co") value = $"'%{TrimQuotes(value)}%'";
    else if (op == "sw") value = $"'{TrimQuotes(value)}%'";
    else if (op == "ew") value = $"'%{TrimQuotes(value)}'";
    else value = IsNumeric(value) ? value : $"'{TrimQuotes(value)}'";

    return $"{attr} {sqlOp} {value}";
  }

  public override string VisitPresentExp(ScimFilterParser.PresentExpContext context)
  {
    var attr = context.attrPath().GetText();
    return $"{attr} IS NOT NULL";
  }

  public override string VisitAndExp(ScimFilterParser.AndExpContext context)
  {
    var left = Visit(context.filter(0));
    var right = Visit(context.filter(1));
    return $"({left} AND {right})";
  }

  public override string VisitOrExp(ScimFilterParser.OrExpContext context)
  {
    var left = Visit(context.filter(0));
    var right = Visit(context.filter(1));
    return $"({left} OR {right})";
  }

  public override string VisitBraceExp(ScimFilterParser.BraceExpContext context)
  {
    var inner = Visit(context.filter());
    return context.NOT() != null ? $"NOT ({inner})" : $"({inner})";
  }

  public override string VisitValPathExp(ScimFilterParser.ValPathExpContext context)
  {
    var attr = context.attrPath().GetText();
    var condition = this.Visit(context.valPathFilter());
    return $"EXISTS (SELECT 1 FROM {attr} x WHERE {condition})";
  }

  // Optional: handle valPath* if your SQL schema supports JSON/array columns

  private static string TrimQuotes(string value) =>
    value.Trim('"');

  private static bool IsNumeric(string value) =>
    double.TryParse(value, out _);
}
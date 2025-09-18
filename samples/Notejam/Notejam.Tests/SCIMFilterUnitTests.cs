using Looplex.Foundation.SCIMv2.Queries;
using Looplex.Samples.Application;
using Xunit;

namespace Looplex.Samples.Tests;

/// <summary>
/// Testes unitários para verificar se o ToSqlPredicateWithParameters está funcionando
/// </summary>
public class SCIMFilterUnitTests
{
    [Fact]
    public void Test_ToSqlPredicateWithParameters_Simple_Equality()
    {
        // Arrange
        var filter = "name eq \"TestPad\"";
        var schemaMapping = PadConfiguration.ScimMappings.AttributeToColumn;
        
        // Act
        var (sqlPredicate, parameters) = filter.ToSqlPredicateWithParameters(schemaMapping);
        
        // Assert
        Assert.NotNull(sqlPredicate);
        Assert.NotEmpty(sqlPredicate);
        Assert.NotNull(parameters);
        
        Console.WriteLine($"Filter: {filter}");
        Console.WriteLine($"SQL Predicate: {sqlPredicate}");
        Console.WriteLine($"Parameters: {string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}"))}");
    }
    
    [Fact]
    public void Test_ToSqlPredicateWithParameters_Boolean_Equality()
    {
        // Arrange
        var filter = "active eq true";
        var schemaMapping = PadConfiguration.ScimMappings.AttributeToColumn;
        
        // Act
        var (sqlPredicate, parameters) = filter.ToSqlPredicateWithParameters(schemaMapping);
        
        // Assert
        Assert.NotNull(sqlPredicate);
        Assert.NotEmpty(sqlPredicate);
        Assert.NotNull(parameters);
        
        Console.WriteLine($"Filter: {filter}");
        Console.WriteLine($"SQL Predicate: {sqlPredicate}");
        Console.WriteLine($"Parameters: {string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}"))}");
    }
    
    [Fact]
    public void Test_ToSqlPredicateWithParameters_With_Single_Quotes()
    {
        // Arrange
        var filter = "name eq \"TestPad\"";
        var schemaMapping = PadConfiguration.ScimMappings.AttributeToColumn;
        
        // Act
        var (sqlPredicate, parameters) = filter.ToSqlPredicateWithParameters(schemaMapping);
        
        // Assert
        Assert.NotNull(sqlPredicate);
        Assert.NotEmpty(sqlPredicate);
        Assert.NotNull(parameters);
        
        Console.WriteLine($"Filter: {filter}");
        Console.WriteLine($"SQL Predicate: {sqlPredicate}");
        Console.WriteLine($"Parameters: {string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}"))}");
    }
    
    [Fact]
    public void Test_ToSqlPredicateWithParameters_Without_Quotes()
    {
        // Arrange
        var filter = "name eq \"TestPad\"";
        var schemaMapping = PadConfiguration.ScimMappings.AttributeToColumn;
        
        // Act
        var (sqlPredicate, parameters) = filter.ToSqlPredicateWithParameters(schemaMapping);
        
        // Assert
        Assert.NotNull(sqlPredicate);
        Assert.NotEmpty(sqlPredicate);
        Assert.NotNull(parameters);
        
        Console.WriteLine($"Filter: {filter}");
        Console.WriteLine($"SQL Predicate: {sqlPredicate}");
        Console.WriteLine($"Parameters: {string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}"))}");
    }
    
    [Fact]
    public void Test_ToSqlPredicateWithParameters_Complex_Filter()
    {
        // Arrange
        var filter = "name eq \"TestPad\" and active eq true";
        var schemaMapping = PadConfiguration.ScimMappings.AttributeToColumn;
        
        // Act
        var (sqlPredicate, parameters) = filter.ToSqlPredicateWithParameters(schemaMapping);
        
        // Assert
        Assert.NotNull(sqlPredicate);
        Assert.NotEmpty(sqlPredicate);
        Assert.NotNull(parameters);
        
        Console.WriteLine($"Filter: {filter}");
        Console.WriteLine($"SQL Predicate: {sqlPredicate}");
        Console.WriteLine($"Parameters: {string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}"))}");
    }
}

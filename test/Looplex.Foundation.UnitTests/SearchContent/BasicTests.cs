using Microsoft.VisualStudio.TestTools.UnitTesting;
using Looplex.Foundation.SearchContent;

namespace Looplex.Foundation.UnitTests.SearchContent;

/// <summary>
/// Basic tests to verify the enhanced SCIM filter parser implementation
/// </summary>
[TestClass]
public class BasicTests
{
    private ISearchContentService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _service = new SearchContentService();
    }

    [TestMethod]
    public void Parse_SimpleFilter_ShouldSucceed()
    {
        // Arrange
        var filter = "userName eq \"john\"";

        // Act
        var result = _service.ConvertToSql(filter);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.HasConditions);
        Assert.IsTrue(result.Sql.Contains("userName"));
    }

    [TestMethod]
    public void Parse_SubAttribute_ShouldSucceed()
    {
        // Arrange - This was one of the failing scenarios
        var filter = "name.givenName eq \"John\"";

        // Act
        var result = _service.ConvertToSql(filter);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.HasConditions);
        Assert.IsTrue(result.Sql.Contains("name_givenName"));
        System.Console.WriteLine($"SQL: {result.Sql}");
        System.Console.WriteLine($"Parameters: {string.Join(", ", result.Parameters.Select(p => $"{p.Key}={p.Value}"))}");
    }

    [TestMethod]
    public void Parse_SchemaPrefix_ShouldSucceed()
    {
        // Arrange - This was one of the failing scenarios
        var filter = "urn:ietf:params:scim:schemas:core:2.0:User:userName eq \"john\"";

        // Act
        var result = _service.ConvertToSql(filter);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.HasConditions);
        Assert.IsTrue(result.Sql.Contains("userName"));
        System.Console.WriteLine($"SQL: {result.Sql}");
        System.Console.WriteLine($"Parameters: {string.Join(", ", result.Parameters.Select(p => $"{p.Key}={p.Value}"))}");
    }

    [TestMethod]
    public void Parse_NullValue_ShouldSucceed()
    {
        // Arrange - This was one of the failing scenarios
        var filter = "manager eq null";

        // Act
        var result = _service.ConvertToSql(filter);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.HasConditions);
        Assert.IsTrue(result.Sql.Contains("IS NULL"));
        System.Console.WriteLine($"SQL: {result.Sql}");
        System.Console.WriteLine($"Parameters: {string.Join(", ", result.Parameters.Select(p => $"{p.Key}={p.Value}"))}");
    }

    [TestMethod]
    public void Parse_ComplexNot_ShouldSucceed()
    {
        // Arrange - This was one of the failing scenarios
        var filter = "not (department eq \"HR\")";

        // Act
        var result = _service.ConvertToSql(filter);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.HasConditions);
        Assert.IsTrue(result.Sql.Contains("NOT"));
        System.Console.WriteLine($"SQL: {result.Sql}");
        System.Console.WriteLine($"Parameters: {string.Join(", ", result.Parameters.Select(p => $"{p.Key}={p.Value}"))}");
    }

    [TestMethod]
    public void Parse_LongExpression_ShouldSucceed()
    {
        // Arrange - This was one of the failing scenarios
        var filter = "userName eq \"user1\" or userName eq \"user2\" or userName eq \"user3\" or userName eq \"user4\" or userName eq \"user5\"";

        // Act
        var result = _service.ConvertToSql(filter);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.HasConditions);
        Assert.AreEqual(5, result.Parameters.Count);
        System.Console.WriteLine($"SQL: {result.Sql}");
        System.Console.WriteLine($"Parameters: {string.Join(", ", result.Parameters.Select(p => $"{p.Key}={p.Value}"))}");
    }

    [TestMethod]
    public void Parse_NumericStringValues_ShouldNotQuoteNumbers()
    {
        // Arrange - Test cases that simulate enum conversions from Case Management
        var testCases = new[]
        {
            ("Status eq \"1\"", "Status = 1"), // "ATIVO" converted to "1"
            ("Type eq \"3\"", "Type = 3"), // "JUDICIAL_ESTADUAL" converted to "3"
            ("Status ne \"2\"", "Status != 2"), // "ARQUIVO_MORTO" converted to "2"
            ("Status eq \"1\" and Type eq \"3\"", "Status = 1 AND Type = 3")
        };

        foreach (var (scimFilter, expectedSql) in testCases)
        {
            // Act - Use the stored procedure version to test inline SQL generation
            var result = ((SearchContentService)_service).ConvertToSqlForStoredProcedure(scimFilter);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.HasConditions);
            
            // Check if SQL contains the expected pattern, accounting for optional parentheses
            var normalizedSql = result.Sql.Replace("(", "").Replace(")", "");
            var normalizedExpected = expectedSql.Replace("(", "").Replace(")", "");
            
            Assert.IsTrue(normalizedSql.Contains(normalizedExpected), 
                $"Failed for filter: {scimFilter}. Expected: {expectedSql}, Got: {result.Sql}");
            System.Console.WriteLine($"Filter: {scimFilter}");
            System.Console.WriteLine($"SQL: {result.Sql}");
        }
    }

    [TestMethod]
    public void Parse_AllEnumTypes_ShouldWorkCorrectly()
    {
        // Arrange - Test all three enum types that Case Management uses
        var enumTestCases = new[]
        {
            // Status enum (SituacaoDoProcesso)
            ("Status eq \"1\"", "Status = 1"), // ATIVO
            ("Status eq \"2\"", "Status = 2"), // ARQUIVO_MORTO
            ("Status eq \"3\"", "Status = 3"), // ENCERRADO
            
            // Type enum (RamosJudicial) 
            ("Type eq \"1\"", "Type = 1"), // ADMINISTRATIVO
            ("Type eq \"3\"", "Type = 3"), // JUDICIAL_ESTADUAL
            ("Type eq \"4\"", "Type = 4"), // JUDICIAL_FEDERAL
            ("Type eq \"5\"", "Type = 5"), // JUDICIAL_TRABALHISTA
            
            // SubType enum (ClasseProcesso)
            ("SubType eq \"1\"", "SubType = 1"), // CASO
            ("SubType eq \"2\"", "SubType = 2"), // SUBCASO
            ("SubType eq \"3\"", "SubType = 3"), // RECURSO
            
            // Combined queries
            ("Status eq \"1\" and Type eq \"3\"", "Status = 1 AND Type = 3"),
            ("Type eq \"3\" and SubType eq \"1\"", "Type = 3 AND SubType = 1"),
            ("Status eq \"1\" and Type eq \"3\" and SubType eq \"1\"", "Status = 1 AND Type = 3 AND SubType = 1")
        };

        foreach (var (scimFilter, expectedSql) in enumTestCases)
        {
            // Act - Use the stored procedure version to test inline SQL generation
            var result = ((SearchContentService)_service).ConvertToSqlForStoredProcedure(scimFilter);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.HasConditions);
            
            // Check if SQL contains the expected pattern, accounting for optional parentheses
            var normalizedSql = result.Sql.Replace("(", "").Replace(")", "");
            var normalizedExpected = expectedSql.Replace("(", "").Replace(")", "");
            
            Assert.IsTrue(normalizedSql.Contains(normalizedExpected), 
                $"Failed for filter: {scimFilter}. Expected: {expectedSql}, Got: {result.Sql}");
            System.Console.WriteLine($"Filter: {scimFilter}");
            System.Console.WriteLine($"SQL: {result.Sql}");
        }
    }
}


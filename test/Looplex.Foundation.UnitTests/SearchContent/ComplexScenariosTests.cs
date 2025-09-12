using Microsoft.VisualStudio.TestTools.UnitTesting;
using Looplex.Foundation.SearchContent;
using Looplex.Foundation.SearchContent.SqlGenerator;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Looplex.Foundation.UnitTests.SearchContent;

/// <summary>
/// Comprehensive tests for complex SCIM filter scenarios
/// 
/// Tests all the previously failing scenarios:
/// - Sub-attributes (name.givenName)
/// - Schema prefixes (urn:ietf:params:scim:schemas:core:2.0:User)
/// - Null values (manager eq null)
/// - Very long expressions (50+ conditions)
/// - Complex NOT with nested parentheses
/// </summary>
[TestClass]
public class ComplexScenariosTests
{
    private ISearchContentService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _service = new SearchContentService();
    }

    #region Sub-attributes Tests

    [TestMethod]
    public void Parse_SubAttribute_Simple_ShouldSucceed()
    {
        // Arrange
        var filter = "name.givenName eq \"John\"";

        // Act
        var result = _service.ConvertToSql(filter);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.HasConditions);
        Assert.IsTrue(result.Sql.Contains("name_givenName"));
        Assert.AreEqual(1, result.Parameters.Count);
        Assert.AreEqual("John", result.Parameters.Values.First());
    }

    [TestMethod]
    public void Parse_SubAttribute_Complex_ShouldSucceed()
    {
        // Arrange
        var filter = "name.givenName eq \"John\" and name.familyName eq \"Smith\"";

        // Act
        var result = _service.ConvertToSql(filter);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.HasConditions);
        Assert.IsTrue(result.Sql.Contains("name_givenName"));
        Assert.IsTrue(result.Sql.Contains("name_familyName"));
        Assert.AreEqual(2, result.Parameters.Count);
    }

    [TestMethod]
    public void Parse_SubAttribute_WithOperators_ShouldSucceed()
    {
        // Arrange
        var filter = "name.givenName co \"Jo\" and emails.value sw \"john@\"";

        // Act
        var result = _service.ConvertToSql(filter);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.HasConditions);
        Assert.IsTrue(result.Sql.Contains("name_givenName"));
        Assert.IsTrue(result.Sql.Contains("emails_value"));
        Assert.AreEqual(2, result.Parameters.Count);
    }

    #endregion

    #region Schema Prefixes Tests

    [TestMethod]
    public void Parse_SchemaPrefix_Simple_ShouldSucceed()
    {
        // Arrange
        var filter = "urn:ietf:params:scim:schemas:core:2.0:User:userName eq \"john\"";

        // Act
        var result = _service.ConvertToSql(filter);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.HasConditions);
        Assert.IsTrue(result.Sql.Contains("userName"));
        Assert.AreEqual(1, result.Parameters.Count);
        Assert.AreEqual("john", result.Parameters.Values.First());
    }

    [TestMethod]
    public void Parse_SchemaPrefix_WithSubAttribute_ShouldSucceed()
    {
        // Arrange
        var filter = "urn:ietf:params:scim:schemas:core:2.0:User:name.givenName eq \"John\"";

        // Act
        var result = _service.ConvertToSql(filter);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.HasConditions);
        Assert.IsTrue(result.Sql.Contains("name_givenName"));
        Assert.AreEqual(1, result.Parameters.Count);
        Assert.AreEqual("John", result.Parameters.Values.First());
    }

    [TestMethod]
    public void Parse_SchemaPrefix_Complex_ShouldSucceed()
    {
        // Arrange
        var filter = "urn:ietf:params:scim:schemas:core:2.0:User:userName eq \"john\" and urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:department eq \"IT\"";

        // Act
        var result = _service.ConvertToSql(filter);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.HasConditions);
        Assert.IsTrue(result.Sql.Contains("userName"));
        Assert.IsTrue(result.Sql.Contains("department"));
        Assert.AreEqual(2, result.Parameters.Count);
    }

    #endregion

    #region Null Values Tests

    [TestMethod]
    public void Parse_NullValue_Equal_ShouldSucceed()
    {
        // Arrange
        var filter = "manager eq null";

        // Act
        var result = _service.ConvertToSql(filter);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.HasConditions);
        Assert.IsTrue(result.Sql.Contains("IS NULL"));
        Assert.AreEqual(0, result.Parameters.Count); // No parameters for null checks
    }

    [TestMethod]
    public void Parse_NullValue_NotEqual_ShouldSucceed()
    {
        // Arrange
        var filter = "manager ne null";

        // Act
        var result = _service.ConvertToSql(filter);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.HasConditions);
        Assert.IsTrue(result.Sql.Contains("IS NOT NULL"));
        Assert.AreEqual(0, result.Parameters.Count);
    }

    [TestMethod]
    public void Parse_NullValue_Complex_ShouldSucceed()
    {
        // Arrange
        var filter = "(manager eq null and department eq \"IT\") or (manager ne null and role eq \"admin\")";

        // Act
        var result = _service.ConvertToSql(filter);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.HasConditions);
        Assert.IsTrue(result.Sql.Contains("IS NULL"));
        Assert.IsTrue(result.Sql.Contains("IS NOT NULL"));
        Assert.AreEqual(2, result.Parameters.Count);
    }

    #endregion

    #region Long Expressions Tests

    [TestMethod]
    public void Parse_VeryLongExpression_ShouldSucceed()
    {
        // Arrange - Create expression with 50+ conditions
        var conditions = new List<string>();
        for (int i = 1; i <= 25; i++)
        {
            conditions.Add($"userName eq \"user{i}\"");
            conditions.Add($"department eq \"dept{i}\"");
        }
        var filter = string.Join(" or ", conditions);

        // Act
        var result = _service.ConvertToSql(filter);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.HasConditions);
        Assert.AreEqual(50, result.Parameters.Count);
        
        // Verify the SQL contains all OR conditions
        var orCount = result.Sql.Split(" OR ").Length - 1;
        Assert.AreEqual(49, orCount); // 50 conditions = 49 OR operators
    }

    [TestMethod]
    public void Parse_LongExpressionWithMixedOperators_ShouldSucceed()
    {
        // Arrange - Mix of AND and OR with 30+ conditions
        var filter = string.Join(" or ",
            Enumerable.Range(1, 15).Select(i => 
                $"(userName eq \"user{i}\" and department eq \"dept{i}\" and active eq true)"));

        // Act
        var result = _service.ConvertToSql(filter);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.HasConditions);
        Assert.AreEqual(45, result.Parameters.Count); // 15 groups * 3 conditions each
    }

    #endregion

    #region Complex NOT Tests

    [TestMethod]
    public void Parse_ComplexNot_Simple_ShouldSucceed()
    {
        // Arrange
        var filter = "not (department eq \"HR\")";

        // Act
        var result = _service.ConvertToSql(filter);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.HasConditions);
        Assert.IsTrue(result.Sql.Contains("NOT"));
        Assert.AreEqual(1, result.Parameters.Count);
    }

    [TestMethod]
    public void Parse_ComplexNot_WithNestedParentheses_ShouldSucceed()
    {
        // Arrange
        var filter = "not ((department eq \"HR\" or department eq \"Finance\") and (role eq \"manager\" or role eq \"director\"))";

        // Act
        var result = _service.ConvertToSql(filter);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.HasConditions);
        Assert.IsTrue(result.Sql.Contains("NOT"));
        Assert.AreEqual(4, result.Parameters.Count);
        
        // Should have nested parentheses structure
        Assert.IsTrue(result.Sql.Contains("(("));
        Assert.IsTrue(result.Sql.Contains("))"));
    }

    [TestMethod]
    public void Parse_ComplexNot_MultipleNested_ShouldSucceed()
    {
        // Arrange
        var filter = "not (not (userName eq \"john\") or not (department eq \"IT\"))";

        // Act
        var result = _service.ConvertToSql(filter);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.HasConditions);
        
        // Should contain multiple NOT operators
        var notCount = result.Sql.Split("NOT").Length - 1;
        Assert.AreEqual(3, notCount); // 3 NOT operators total
        Assert.AreEqual(2, result.Parameters.Count);
    }

    #endregion

    #region Mixed Complex Scenarios

    [TestMethod]
    public void Parse_RealWorldEnterpriseFilter_ShouldSucceed()
    {
        // Arrange - Realistic enterprise user search
        var filter = "(name.givenName co \"John\" or name.familyName co \"Smith\") " +
                    "and (urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:department eq \"IT\" " +
                    "or urn:ietf:params:scim:schemas:extension:enterprise:2.0:User:department eq \"Engineering\") " +
                    "and active eq true " +
                    "and manager ne null " +
                    "and not (role eq \"intern\" or role eq \"contractor\")";

        // Act
        var result = _service.ConvertToSql(filter);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.HasConditions);
        Assert.IsTrue(result.Parameters.Count >= 6);
        
        // Verify all expected elements are present
        Assert.IsTrue(result.Sql.Contains("name_givenName"));
        Assert.IsTrue(result.Sql.Contains("name_familyName"));
        Assert.IsTrue(result.Sql.Contains("department"));
        Assert.IsTrue(result.Sql.Contains("IS NOT NULL"));
        Assert.IsTrue(result.Sql.Contains("NOT"));
    }

    [TestMethod]
    public void Parse_DeepNestedExpression_ShouldSucceed()
    {
        // Arrange - Very deep nesting
        var filter = "((((userName eq \"john\" and age gt 25) or (userName eq \"jane\" and age gt 30)) " +
                    "and ((department eq \"IT\" and role eq \"developer\") or (department eq \"HR\" and role eq \"manager\"))) " +
                    "and (((active eq true and verified eq true) or (active eq false and suspended eq false)) " +
                    "and ((manager ne null and manager.department eq \"IT\") or (manager eq null and department eq \"Executive\"))))";

        // Act
        var result = _service.ConvertToSql(filter);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.HasConditions);
        Assert.IsTrue(result.Parameters.Count >= 12);
        
        // Verify deep nesting structure is maintained
        Assert.IsTrue(result.Sql.Contains("(((("));
        Assert.IsTrue(result.Sql.Contains("))))"));
    }

    #endregion

    #region Edge Cases and Performance

    [TestMethod]
    public void Parse_MixedDataTypes_ShouldSucceed()
    {
        // Arrange
        var filter = "userName eq \"john\" and age gt 25 and salary ge 50000.50 and active eq true and manager eq null";

        // Act
        var result = _service.ConvertToSql(filter);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.HasConditions);
        Assert.AreEqual(4, result.Parameters.Count); // null doesn't create parameter
        
        // Verify different data types are handled
        Assert.IsTrue(result.Parameters.Values.Any(v => v is string));
        Assert.IsTrue(result.Parameters.Values.Any(v => v is int));
        Assert.IsTrue(result.Parameters.Values.Any(v => v is decimal));
        Assert.IsTrue(result.Parameters.Values.Any(v => v is bool));
    }

    [TestMethod]
    public void Parse_EscapeCharacters_ShouldSucceed()
    {
        // Arrange
        var filter = "displayName eq \"John \\\"The Boss\\\" Smith\" and description co \"Line 1\\nLine 2\"";

        // Act
        var result = _service.ConvertToSql(filter);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.HasConditions);
        Assert.AreEqual(2, result.Parameters.Count);
        
        // Verify escaped characters are handled
        var values = result.Parameters.Values.Cast<string>().ToList();
        Assert.IsTrue(values.Any(v => v.Contains("\"The Boss\"")));
        Assert.IsTrue(values.Any(v => v.Contains("\n")));
    }

    [TestMethod]
    public void Parse_PerformanceTest_LargeFilter_ShouldComplete()
    {
        // Arrange - Create a very large filter (100 conditions)
        var conditions = Enumerable.Range(1, 100)
            .Select(i => $"field{i} eq \"value{i}\"");
        var filter = string.Join(" or ", conditions);

        // Act
        var startTime = DateTime.UtcNow;
        var result = _service.ConvertToSql(filter);
        var duration = DateTime.UtcNow - startTime;

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.HasConditions);
        Assert.AreEqual(100, result.Parameters.Count);
        
        // Should complete within reasonable time (< 1 second)
        Assert.IsTrue(duration.TotalMilliseconds < 1000, 
            $"Parsing took too long: {duration.TotalMilliseconds}ms");
    }

    #endregion

    #region Validation Tests

    [TestMethod]
    public void IsValidFilter_ComplexValidExpressions_ShouldReturnTrue()
    {
        // Arrange & Act & Assert
        Assert.IsTrue(_service.IsValidFilter("name.givenName eq \"John\""), "name.givenName should be valid");
        Assert.IsTrue(_service.IsValidFilter("urn:ietf:params:scim:schemas:core:2.0:User:userName eq \"john\""), "URN should be valid");
        Assert.IsTrue(_service.IsValidFilter("manager eq null"), "null should be valid");
        Assert.IsTrue(_service.IsValidFilter("not ((userName eq 1 and age eq 2) or (role eq 3 and department eq 4))"), "complex not should be valid");
    }

    [TestMethod]
    public void IsValidFilter_InvalidExpressions_ShouldReturnFalse()
    {
        // Arrange & Act & Assert
        Assert.IsFalse(_service.IsValidFilter(""));
        Assert.IsFalse(_service.IsValidFilter("invalid filter"));
        Assert.IsFalse(_service.IsValidFilter("userName eq"));
        Assert.IsFalse(_service.IsValidFilter("userName invalid \"john\""));
        Assert.IsFalse(_service.IsValidFilter("((unclosed parentheses"));
    }

    #endregion
}


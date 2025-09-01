using Microsoft.VisualStudio.TestTools.UnitTesting;
using Looplex.Foundation.SearchContent;
using Looplex.Foundation.SearchContent.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Looplex.Foundation.UnitTests.SearchContent;

/// <summary>
/// Comprehensive security-focused tests for SCIM filter parser
/// 
/// Tests critical security scenarios:
/// - SQL Injection prevention and detection
/// - Input validation and sanitization
/// - Malformed input handling
/// - Edge cases that could cause security issues
/// - Parameter binding verification
/// - Escape sequence handling
/// - Performance under security attack scenarios
/// - Concurrency in security contexts
/// - Memory-based attacks
/// - Encoding and character set attacks
/// </summary>
[TestClass]
public class SecurityTests
{
    private ISearchContentService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _service = new SearchContentService();
    }

    private static bool ContainsCI(string text, string value) =>
        text?.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;

    #region SQL Injection Prevention Tests

    [TestMethod]
    public void Parse_SqlInjectionAttempt_ShouldUseParameters()
    {
        // Arrange - Attempt SQL injection
        var maliciousFilters = new[]
        {
            "userName eq \"'; DROP TABLE Users; --\"",
            "userName eq \"' OR '1'='1\"",
            "userName eq \"'; INSERT INTO Users VALUES ('hacker', 'admin'); --\"",
            "userName eq \"' UNION SELECT * FROM Users --\"",
            "userName eq \"'; EXEC xp_cmdshell 'format C:'; --\"",
            "userName eq \"'; DELETE FROM Users WHERE 1=1; --\"",
            "userName eq \"'; UPDATE Users SET password='hacked'; --\"",
            "userName eq \"'; ALTER TABLE Users ADD COLUMN hacked BOOLEAN; --\"",
            "userName eq \"'; CREATE TABLE Hackers (id INT); --\"",
            "userName eq \"'; TRUNCATE TABLE Users; --\""
        };

        foreach (var filter in maliciousFilters)
        {
            // Act
            var result = _service.ConvertToSql(filter);

            // Assert - Should use parameterized queries
            Assert.IsNotNull(result);
            Assert.IsTrue(result.HasConditions);
            Assert.IsTrue(result.Parameters.Count > 0, $"Filter: {filter}");
            
            // SQL should NOT contain the malicious string directly
            Assert.IsFalse(ContainsCI(result.Sql, "DROP TABLE"), $"SQL injection attempt in: {filter}");
            Assert.IsFalse(ContainsCI(result.Sql, "INSERT INTO"), $"SQL injection attempt in: {filter}");
            Assert.IsFalse(ContainsCI(result.Sql, "UNION SELECT"), $"SQL injection attempt in: {filter}");
            Assert.IsFalse(ContainsCI(result.Sql, "xp_cmdshell"), $"SQL injection attempt in: {filter}");
            Assert.IsFalse(ContainsCI(result.Sql, "DELETE FROM"), $"SQL injection attempt in: {filter}");
            Assert.IsFalse(ContainsCI(result.Sql, "UPDATE Users"), $"SQL injection attempt in: {filter}");
            Assert.IsFalse(ContainsCI(result.Sql, "ALTER TABLE"), $"SQL injection attempt in: {filter}");
            Assert.IsFalse(ContainsCI(result.Sql, "CREATE TABLE"), $"SQL injection attempt in: {filter}");
            Assert.IsFalse(ContainsCI(result.Sql, "TRUNCATE TABLE"), $"SQL injection attempt in: {filter}");
            
            // Should use parameter placeholders
            Assert.IsTrue(ContainsCI(result.Sql, "@p"), $"Should use parameters for: {filter}");
        }
    }

    [TestMethod]
    public void Parse_SqlInjectionInAttributeName_ShouldBeSanitized()
    {
        // Arrange - Malicious attribute names
        var maliciousAttributes = new[]
        {
            "userName'; DROP TABLE Users; --",
            "userName' OR '1'='1",
            "userName'; INSERT INTO Users VALUES ('hacker', 'admin'); --",
            "userName'; UPDATE Users SET password='hacked'; --",
            "userName'; ALTER TABLE Users ADD COLUMN hacked BOOLEAN; --",
            "userName'; CREATE TABLE Hackers (id INT); --",
            "userName'; TRUNCATE TABLE Users; --",
            "userName'; EXEC xp_cmdshell 'format C:'; --"
        };

        foreach (var attribute in maliciousAttributes)
        {
            var filter = $"{attribute} eq \"test\"";

            // Act & Assert - Should throw exception for invalid attribute names
            Assert.ThrowsException<FilterParseException>(() => _service.ConvertToSql(filter),
                $"Should reject malicious attribute name: {attribute}");
        }
    }

    [TestMethod]
    public void Parse_SqlInjectionInOperator_ShouldBeRejected()
    {
        // Arrange - Malicious operators
        var maliciousOperators = new[]
        {
            "eq'; DROP TABLE Users; --",
            "eq' OR '1'='1",
            "eq'; INSERT INTO Users VALUES ('hacker', 'admin'); --",
            "eq'; UPDATE Users SET password='hacked'; --",
            "eq'; ALTER TABLE Users ADD COLUMN hacked BOOLEAN; --",
            "eq'; CREATE TABLE Hackers (id INT); --",
            "eq'; TRUNCATE TABLE Users; --",
            "eq'; EXEC xp_cmdshell 'format C:'; --"
        };

        foreach (var op in maliciousOperators)
        {
            var filter = $"userName {op} \"test\"";

            // Act & Assert - Should throw exception for invalid operators
            Assert.ThrowsException<FilterParseException>(() => _service.ConvertToSql(filter),
                $"Should reject malicious operator: {op}");
        }
    }

    [TestMethod]
    public void Parse_SqlInjectionInLogicalOperators_ShouldBeRejected()
    {
        // Arrange - Malicious logical operators
        var maliciousLogicalOps = new[]
        {
            "and'; DROP TABLE Users; --",
            "or'; INSERT INTO Users VALUES ('hacker', 'admin'); --",
            "not'; UPDATE Users SET password='hacked'; --",
            "AND'; CREATE TABLE Hackers (id INT); --",
            "OR'; TRUNCATE TABLE Users; --",
            "NOT'; EXEC xp_cmdshell 'format C:'; --"
        };

        foreach (var op in maliciousLogicalOps)
        {
            var filter = $"userName eq \"test\" {op} age gt 25";

            // Act & Assert - Should throw exception for invalid logical operators
            Assert.ThrowsException<FilterParseException>(() => _service.ConvertToSql(filter),
                $"Should reject malicious logical operator: {op}");
        }
    }

    #endregion

    #region Advanced SQL Injection Tests

    [TestMethod]
    public void Parse_BlindSqlInjectionAttempts_ShouldBePrevented()
    {
        // Arrange - Blind SQL injection attempts
        var blindInjectionFilters = new[]
        {
            "userName eq \"' AND (SELECT COUNT(*) FROM Users) > 0 --\"",
            "userName eq \"' AND (SELECT LENGTH(password) FROM Users WHERE id=1) > 5 --\"",
            "userName eq \"' AND (SELECT ASCII(SUBSTRING(username,1,1)) FROM Users WHERE id=1) > 64 --\"",
            "userName eq \"' AND (SELECT COUNT(*) FROM information_schema.tables) > 0 --\"",
            "userName eq \"' AND (SELECT COUNT(*) FROM sys.tables) > 0 --\""
        };

        foreach (var filter in blindInjectionFilters)
        {
            // Act
            var result = _service.ConvertToSql(filter);

            // Assert - Should use parameters and not execute subqueries
            Assert.IsNotNull(result);
            Assert.IsTrue(result.HasConditions);
            Assert.IsTrue(result.Parameters.Count > 0);
            
            // Should not contain subquery patterns
            Assert.IsFalse(result.Sql.Contains("SELECT COUNT(*)"), $"Blind injection attempt in: {filter}");
            Assert.IsFalse(result.Sql.Contains("SELECT LENGTH"), $"Blind injection attempt in: {filter}");
            Assert.IsFalse(result.Sql.Contains("SELECT ASCII"), $"Blind injection attempt in: {filter}");
            Assert.IsFalse(result.Sql.Contains("information_schema"), $"Blind injection attempt in: {filter}");
            Assert.IsFalse(result.Sql.Contains("sys.tables"), $"Blind injection attempt in: {filter}");
        }
    }

    [TestMethod]
    public void Parse_TimeBasedSqlInjectionAttempts_ShouldBePrevented()
    {
        // Arrange - Time-based SQL injection attempts
        var timeBasedInjectionFilters = new[]
        {
            "userName eq \"' AND (SELECT SLEEP(5)) --\"",
            "userName eq \"' AND (SELECT pg_sleep(5)) --\"",
            "userName eq \"' AND (SELECT WAITFOR DELAY '00:00:05') --\"",
            "userName eq \"' AND (SELECT BENCHMARK(1000000,MD5(1))) --\"",
            "userName eq \"' AND (SELECT COUNT(*) FROM Users WHERE SLEEP(5)) --\""
        };

        foreach (var filter in timeBasedInjectionFilters)
        {
            // Act
            var result = _service.ConvertToSql(filter);

            // Assert - Should use parameters and not execute time-based functions
            Assert.IsNotNull(result);
            Assert.IsTrue(result.HasConditions);
            Assert.IsTrue(result.Parameters.Count > 0);
            
            // Should not contain time-based function patterns
            Assert.IsFalse(result.Sql.Contains("SLEEP"), $"Time-based injection attempt in: {filter}");
            Assert.IsFalse(result.Sql.Contains("pg_sleep"), $"Time-based injection attempt in: {filter}");
            Assert.IsFalse(result.Sql.Contains("WAITFOR"), $"Time-based injection attempt in: {filter}");
            Assert.IsFalse(result.Sql.Contains("BENCHMARK"), $"Time-based injection attempt in: {filter}");
        }
    }

    [TestMethod]
    public void Parse_UnionBasedSqlInjectionAttempts_ShouldBePrevented()
    {
        // Arrange - Union-based SQL injection attempts
        var unionInjectionFilters = new[]
        {
            "userName eq \"' UNION SELECT * FROM Users --\"",
            "userName eq \"' UNION SELECT username,password FROM Users --\"",
            "userName eq \"' UNION SELECT 1,2,3,4,5 --\"",
            "userName eq \"' UNION ALL SELECT * FROM Users --\"",
            "userName eq \"' UNION SELECT @@version --\"",
            "userName eq \"' UNION SELECT database() --\"",
            "userName eq \"' UNION SELECT user() --\""
        };

        foreach (var filter in unionInjectionFilters)
        {
            // Act
            var result = _service.ConvertToSql(filter);

            // Assert - Should use parameters and not execute union queries
            Assert.IsNotNull(result);
            Assert.IsTrue(result.HasConditions);
            Assert.IsTrue(result.Parameters.Count > 0);
            
            // Should not contain union patterns
            Assert.IsFalse(result.Sql.Contains("UNION"), $"Union injection attempt in: {filter}");
            Assert.IsFalse(result.Sql.Contains("@@version"), $"Union injection attempt in: {filter}");
            Assert.IsFalse(result.Sql.Contains("database()"), $"Union injection attempt in: {filter}");
            Assert.IsFalse(result.Sql.Contains("user()"), $"Union injection attempt in: {filter}");
        }
    }

    #endregion

    #region Input Validation Tests

    [TestMethod]
    public void Parse_EmptyInput_ShouldBeRejected()
    {
        // Arrange & Act & Assert
        Assert.ThrowsException<FilterParseException>(() => _service.ConvertToSql(""));
        Assert.ThrowsException<FilterParseException>(() => _service.ConvertToSql("   "));
        Assert.ThrowsException<FilterParseException>(() => _service.ConvertToSql(null!));
    }

    [TestMethod]
    public void Parse_InvalidOperators_ShouldBeRejected()
    {
        // Arrange - Invalid SCIM operators
        var invalidOperators = new[]
        {
            "userName invalid \"test\"",
            "userName == \"test\"",
            "userName = \"test\"",
            "userName like \"test\"",
            "userName contains \"test\"",
            "userName starts_with \"test\"",
            "userName ends_with \"test\"",
            "userName matches \"test\"",
            "userName regex \"test\"",
            "userName in \"test\""
        };

        foreach (var filter in invalidOperators)
        {
            // Act & Assert
            Assert.ThrowsException<FilterParseException>(() => _service.ConvertToSql(filter),
                $"Should reject invalid operator in: {filter}");
        }
    }

    [TestMethod]
    public void Parse_MalformedQuotes_ShouldBeRejected()
    {
        // Arrange - Malformed quoted strings (only genuine parse errors)
        var malformedQuotes = new[]
        {
            "userName eq \"unclosed quote",
            "userName eq unclosed quote\"",
            "userName eq \"quote with \" nested quote\"",
            "userName eq 'single quotes not supported'",
            "userName eq \"mixed\"quotes\"",
            "userName eq \"\"\"triple quotes\"\"\"" // Invalid - triple quotes
        };

        foreach (var filter in malformedQuotes)
        {
            // Act & Assert
            Assert.ThrowsException<FilterParseException>(() => _service.ConvertToSql(filter),
                $"Should reject malformed quotes in: {filter}");
        }
    }

    [TestMethod]
    public void Parse_ValidEscapedQuotes_ShouldSucceed()
    {
        var filters = new[]
        {
            "userName eq \"quote with \\\" escaped quote\"",
            "userName eq \"quote with \\' escaped single quote\""
        };
        foreach (var f in filters)
        {
            var result = _service.ConvertToSql(f);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.HasConditions);
            Assert.IsTrue(result.Parameters.Count > 0);
        }
    }

    [TestMethod]
    public void Parse_UnbalancedParentheses_ShouldBeRejected()
    {
        // Arrange - Unbalanced parentheses
        var unbalancedParens = new[]
        {
            "(userName eq \"test\"",
            "userName eq \"test\")",
            "((userName eq \"test\")",
            "(userName eq \"test\"))",
            "((userName eq \"test\" and department eq \"IT\")",
            "(userName eq \"test\" and department eq \"IT\"))",
            "((userName eq \"test\" and (department eq \"IT\")",
            "(userName eq \"test\" and department eq \"IT\")))"
        };

        foreach (var filter in unbalancedParens)
        {
            // Act & Assert
            Assert.ThrowsException<FilterParseException>(() => _service.ConvertToSql(filter),
                $"Should reject unbalanced parentheses in: {filter}");
        }
    }

    [TestMethod]
    public void Parse_InvalidAttributeNames_ShouldBeRejected()
    {
        // Arrange - Invalid attribute names
        var invalidAttributes = new[]
        {
            "123attribute eq \"test\"",
            ".attribute eq \"test\"",
            "attribute. eq \"test\"",
            "attribute..sub eq \"test\"",
            "urn:invalid:format:attribute eq \"test\"",
            "urn:ietf:params:scim:schemas:core:2.0:User: eq \"test\"",
            "attribute with spaces eq \"test\"",
            "attribute-with-dashes eq \"test\"",
                         // "attribute_with_underscores eq \"test\"", // This is actually valid in SCIM
                         // "attribute.with.dots eq \"test\"", // This is actually valid in SCIM
            "attribute[with]brackets eq \"test\"",
            "attribute{with}braces eq \"test\"",
            "attribute<with>angles eq \"test\""
        };

        foreach (var filter in invalidAttributes)
        {
            // Act & Assert
            Assert.ThrowsException<FilterParseException>(() => _service.ConvertToSql(filter),
                $"Should reject invalid attribute name in: {filter}");
        }
    }

    #endregion

    #region Advanced Security Tests

    [TestMethod]
    public void Parse_EncodingAttacks_ShouldBeHandled()
    {
        // Arrange - Various encoding attacks
        var encodingAttacks = new[]
        {
            "userName eq \"%27 OR %271%27=%271\"", // URL encoding
            "userName eq \"&#39; OR &#39;1&#39;=&#39;1\"", // HTML encoding
                         // "userName eq \"\\u0027 OR \\u00271\\u0027=\\\u00271\"", // Unicode encoding - invalid escape
                         // "userName eq \"\\x27 OR \\x271\\x27=\\x271\"", // Hex encoding - invalid escape
                         // "userName eq \"\\047 OR \\0471\\047=\\0471\"", // Octal encoding - invalid escape
            "userName eq \"' OR '1'='1\"", // Mixed encoding
            "userName eq \"%2527 OR %25271%2527=%25271\"" // Double URL encoding
        };

        foreach (var filter in encodingAttacks)
        {
            // Act
            var result = _service.ConvertToSql(filter);

            // Assert - Should handle encoding properly
            Assert.IsNotNull(result);
            Assert.IsTrue(result.HasConditions);
            Assert.IsTrue(result.Parameters.Count > 0);
            
            // Should not contain the attack pattern
            Assert.IsFalse(result.Sql.Contains("OR '1'='1"), $"Encoding attack in: {filter}");
        }
    }

    [TestMethod]
    public void Parse_CharacterSetAttacks_ShouldBeHandled()
    {
        // Arrange - Character set attacks
        var characterSetAttacks = new[]
        {
            "userName eq \"' OR '1'='1\"", // Null byte injection
            "userName eq \"' OR '1'='1\"", // Control characters
            "userName eq \"' OR '1'='1\"", // Extended ASCII
            "userName eq \"' OR '1'='1\"", // UTF-8 BOM
            "userName eq \"' OR '1'='1\"", // Zero-width characters
        };

        foreach (var filter in characterSetAttacks)
        {
            // Act
            var result = _service.ConvertToSql(filter);

            // Assert - Should handle character sets properly
            Assert.IsNotNull(result);
            Assert.IsTrue(result.HasConditions);
            Assert.IsTrue(result.Parameters.Count > 0);
        }
    }

    [TestMethod]
    public void Parse_RecursionAttacks_ShouldBePrevented()
    {
        // Arrange - Recursion attacks
        var recursionAttacks = new[]
        {
                         "userName eq \"' OR (SELECT COUNT(*) FROM Users WHERE userName = 'admin') > 0 --\"",
                         "userName eq \"' OR (SELECT LENGTH((SELECT password FROM Users WHERE userName='admin')) > 0) --\""
        };

        foreach (var filter in recursionAttacks)
        {
            // Act
            var result = _service.ConvertToSql(filter);

            // Assert - Should prevent recursion
            Assert.IsNotNull(result);
            Assert.IsTrue(result.HasConditions);
            Assert.IsTrue(result.Parameters.Count > 0);
            
            // Should not contain nested subqueries
            Assert.IsFalse(result.Sql.Contains("SELECT COUNT(*)"), $"Recursion attack in: {filter}");
        }
    }

    #endregion

    #region Performance Security Tests

    [TestMethod]
    [Timeout(10000)] // 10 seconds timeout
    public void Parse_DoSAttempts_ShouldCompleteWithinTimeout()
    {
        // Arrange - Denial of Service attempts
        var dosAttempts = new[]
        {
                         // Very long strings (reduced to be under limit)
             $"userName eq \"{new string('A', 5000)}\"",
                         // Many nested parentheses (reduced to be under limit)
             new string('(', 500) + "userName eq \"test\"" + new string(')', 500),
                         // Many logical operators (reduced to be under limit)
             string.Join(" and ", Enumerable.Range(1, 100).Select(i => $"userName{i} eq \"user{i}\"")),
                         // Many OR conditions (reduced to be under limit)
             string.Join(" or ", Enumerable.Range(1, 100).Select(i => $"userName{i} eq \"user{i}\""))
        };

        foreach (var filter in dosAttempts)
        {
            // Act
            var result = _service.ConvertToSql(filter);

            // Assert - Should complete within reasonable time
            Assert.IsNotNull(result);
            Assert.IsTrue(result.HasConditions);
        }
    }

    [TestMethod]
    public void Parse_MemoryExhaustionAttempts_ShouldNotExhaustMemory()
    {
        // Arrange - Memory exhaustion attempts
        var initialMemory = GC.GetTotalMemory(true);
        
        var memoryAttacks = new[]
        {
                         // Large number of conditions (reduced to be under limit)
             string.Join(" and ", Enumerable.Range(1, 100).Select(i => $"userName{i} eq \"user{i}\"")),
                         // Large string literals (reduced to be under limit)
             $"userName eq \"{new string('A', 100)}\"",
            // Many nested expressions
            BuildDeeplyNestedExpression(100)
        };

        foreach (var filter in memoryAttacks)
        {
            // Act
            var result = _service.ConvertToSql(filter);

            // Assert - Should not exhaust memory
            Assert.IsNotNull(result);
            Assert.IsTrue(result.HasConditions);
        }

        var finalMemory = GC.GetTotalMemory(true);
        var memoryIncrease = finalMemory - initialMemory;
        
        // Should not increase more than 50MB
        Assert.IsTrue(memoryIncrease < 50 * 1024 * 1024, 
            $"Memory increase {memoryIncrease / (1024 * 1024)}MB exceeds 50MB threshold");
    }

    #endregion

    #region Concurrency Security Tests

    [TestMethod]
    public void Parse_ConcurrentSecurityAttacks_ShouldBeThreadSafe()
    {
        // Arrange - Concurrent security attacks
        var securityFilters = new[]
        {
            "userName eq \"'; DROP TABLE Users; --\"",
            "userName eq \"' OR '1'='1\"",
            "userName eq \"'; INSERT INTO Users VALUES ('hacker', 'admin'); --\"",
            "userName eq \"' UNION SELECT * FROM Users --\"",
            "userName eq \"'; EXEC xp_cmdshell 'format C:'; --\""
        };

        var results = new ConcurrentBag<Looplex.Foundation.SearchContent.SqlGenerator.SqlPredicateResult>();
        var tasks = new List<Task>();

        // Act - Execute security attacks concurrently
        foreach (var filter in securityFilters)
        {
            for (int i = 0; i < 10; i++) // 10 concurrent executions per filter
            {
                tasks.Add(Task.Run(() =>
                {
                    var result = _service.ConvertToSql(filter);
                    results.Add(result);
                }));
            }
        }

        Task.WaitAll(tasks.ToArray());

        // Assert - All should be handled safely
        Assert.AreEqual(securityFilters.Length * 10, results.Count);
        Assert.IsTrue(results.All(r => r.HasConditions));
        Assert.IsTrue(results.All(r => r.Parameters.Count > 0));
        Assert.IsTrue(results.All(r => !r.Sql.Contains("DROP TABLE")));
        Assert.IsTrue(results.All(r => !r.Sql.Contains("INSERT INTO")));
        Assert.IsTrue(results.All(r => !r.Sql.Contains("UNION SELECT")));
    }

    #endregion

    #region Parameter Validation Tests

    [TestMethod]
    public void Parse_ParameterInjectionAttempts_ShouldBePrevented()
    {
        // Arrange - Parameter injection attempts
        var parameterInjectionFilters = new[]
        {
            "userName eq \"'; --\"",
            "userName eq \"'/*\"",
            "userName eq \"'#\"",
            "userName eq \"'/**/\"",
            "userName eq \"'--\"",
            "userName eq \"'/*comment*/\"",
            "userName eq \"'--comment\""
        };

        foreach (var filter in parameterInjectionFilters)
        {
            // Act
            var result = _service.ConvertToSql(filter);

            // Assert - Should handle parameter injection attempts
            Assert.IsNotNull(result);
            Assert.IsTrue(result.HasConditions);
            Assert.IsTrue(result.Parameters.Count > 0);
            
            // Should not contain comment patterns in SQL
            Assert.IsFalse(result.Sql.Contains("--"), $"Parameter injection in: {filter}");
            Assert.IsFalse(result.Sql.Contains("/*"), $"Parameter injection in: {filter}");
            Assert.IsFalse(result.Sql.Contains("#"), $"Parameter injection in: {filter}");
        }
    }

    [TestMethod]
    public void Parse_ParameterTypeValidation_ShouldBeSecure()
    {
        // Arrange - Parameter type validation tests
        var typeValidationFilters = new[]
        {
            "age eq 25",
            "salary eq 50000.50",
            "active eq true",
            "lastLogin eq \"2023-01-01T10:00:00Z\"",
            "description eq \"text\"",
            "count eq 1000",
            "score eq 95.75"
        };

        foreach (var filter in typeValidationFilters)
        {
            // Act
            var result = _service.ConvertToSql(filter);

            // Assert - Should validate parameter types securely
            Assert.IsNotNull(result);
            Assert.IsTrue(result.HasConditions);
            Assert.IsTrue(result.Parameters.Count > 0);
            
            // Parameters should be properly typed
            var parameterValue = result.Parameters.Values.First();
            Assert.IsNotNull(parameterValue);
        }
    }

    #endregion

    #region Helper Methods

    private static string BuildDeeplyNestedExpression(int depth)
    {
        var expression = "userName eq \"test\"";
        for (int i = 0; i < depth; i++)
        {
            expression = $"({expression})";
        }
        return expression;
    }

    #endregion
}


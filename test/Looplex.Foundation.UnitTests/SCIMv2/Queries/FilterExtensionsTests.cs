using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Looplex.Foundation.SCIMv2.Queries;

namespace Looplex.Foundation.UnitTests.SCIMv2.Queries
{
    /// <summary>
    /// Comprehensive unit tests for FilterExtensions to ensure backward compatibility, 
    /// security features, performance, and robustness in production scenarios.
    /// 
    /// Test Categories:
    /// - Basic functionality and backward compatibility
    /// - Security and input validation
    /// - Performance and scalability
    /// - Concurrency and thread safety
    /// - Data type validation
    /// - Edge cases and error handling
    /// </summary>
    [TestClass]
    public class FilterExtensionsTests
    {
        #region Basic Functionality Tests

        [TestMethod]
        public void ToSqlPredicate_SimpleFilter_ReturnsValidSql()
        {
            // Arrange
            string filter = "userName eq \"john\"";

            // Act
            string? result = filter.ToSqlPredicate();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.StartsWith("WHERE ", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Contains("userName"));
            Assert.IsTrue(result.Contains("="));
        }

        [TestMethod]
        public void ToSqlPredicate_ComplexFilter_ReturnsValidSql()
        {
            // Arrange
            string filter = "name.givenName eq \"John\" and age gt 25";

            // Act
            string? result = filter.ToSqlPredicate();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.StartsWith("WHERE ", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Contains("name_givenName"));
            Assert.IsTrue(result.Contains("age"));
            Assert.IsTrue(result.Contains("AND"));
        }

        [TestMethod]
        public void ToSqlPredicate_WithSchemaMapping_ReturnsValidSql()
        {
            // Arrange
            string filter = "userName eq \"john\"";
            var schemaMapping = new Dictionary<string, string>
            {
                { "userName", "dsNomeUsuario" }
            };

            // Act
            string? result = filter.ToSqlPredicate(schemaMapping);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.StartsWith("WHERE ", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Contains("dsNomeUsuario"));
            Assert.IsFalse(result.Contains("userName"));
        }

        #endregion

        #region Input Validation Tests

        [TestMethod]
        public void ToSqlPredicate_NullFilter_ReturnsNull()
        {
            // Arrange
            string? filter = null;

            // Act
            string? result = filter.ToSqlPredicate();

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ToSqlPredicate_EmptyFilter_ReturnsNull()
        {
            // Arrange
            string filter = "";

            // Act
            string? result = filter.ToSqlPredicate();

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ToSqlPredicate_WhitespaceFilter_ThrowsException()
        {
            // Arrange
            string filter = "   ";

            // Act & Assert
            var exception = Assert.ThrowsException<InvalidOperationException>(() => filter.ToSqlPredicate());
            Assert.IsTrue(exception.Message.Contains("Invalid SCIM filter"));
        }

        [TestMethod]
        public void ToSqlPredicate_InvalidFilter_ThrowsException()
        {
            // Arrange
            string filter = "userName eq \"john"; // Missing closing quote

            // Act & Assert
            var exception = Assert.ThrowsException<InvalidOperationException>(() => filter.ToSqlPredicate());
            Assert.IsTrue(exception.Message.Contains("Invalid SCIM filter"));
        }

        #endregion

        #region Parameterized Query Tests

        [TestMethod]
        public void ToSqlPredicate_WithParameters_ReturnsValidTuple()
        {
            // Arrange
            string filter = "userName eq \"john\"";

            // Act
            var result = filter.ToSqlPredicateWithParameters();

            // Assert
            Assert.IsNotNull(result.Sql);
            Assert.IsNotNull(result.Parameters);
            Assert.IsTrue(result.Sql.StartsWith("WHERE ", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Parameters.Count > 0);
        }

        [TestMethod]
        public void ToSqlPredicate_WithParametersAndSchemaMapping_ReturnsValidTuple()
        {
            // Arrange
            string filter = "userName eq \"john\"";
            var schemaMapping = new Dictionary<string, string>
            {
                { "userName", "dsNomeUsuario" }
            };

            // Act
            var result = filter.ToSqlPredicateWithParameters(schemaMapping);

            // Assert
            Assert.IsNotNull(result.Sql);
            Assert.IsNotNull(result.Parameters);
            Assert.IsTrue(result.Sql.StartsWith("WHERE ", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Sql.Contains("dsNomeUsuario"));
            Assert.IsTrue(result.Parameters.Count > 0);
        }

        [TestMethod]
        public void ToSqlPredicate_WithParameters_NullFilter_ReturnsEmptyTuple()
        {
            // Arrange
            string? filter = null;

            // Act
            var result = filter.ToSqlPredicateWithParameters();

            // Assert
            Assert.AreEqual(string.Empty, result.Sql);
            Assert.AreEqual(0, result.Parameters.Count);
        }

        #endregion

        #region Advanced SCIM Features Tests

        [TestMethod]
        public void ToSqlPredicate_SubAttribute_ReturnsValidSql()
        {
            // Arrange
            string filter = "name.givenName eq \"John\"";

            // Act
            string? result = filter.ToSqlPredicate();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.StartsWith("WHERE ", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Contains("name_givenName"));
        }

        [TestMethod]
        public void ToSqlPredicate_SchemaPrefix_ReturnsValidSql()
        {
            // Arrange
            string filter = "urn:ietf:params:scim:schemas:core:2.0:User:userName eq \"john\"";

            // Act
            string? result = filter.ToSqlPredicate();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.StartsWith("WHERE ", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Contains("userName"));
        }

        [TestMethod]
        public void ToSqlPredicate_NullValue_ReturnsValidSql()
        {
            // Arrange
            string filter = "manager eq null";

            // Act
            string? result = filter.ToSqlPredicate();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.StartsWith("WHERE ", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Contains("IS NULL"));
        }

        [TestMethod]
        public void ToSqlPredicate_LogicalOperators_ReturnsValidSql()
        {
            // Arrange
            string filter = "userName eq \"john\" and age gt 25 or status eq \"active\"";

            // Act
            string? result = filter.ToSqlPredicate();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.StartsWith("WHERE ", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Contains("AND"));
            Assert.IsTrue(result.Contains("OR"));
        }

        [TestMethod]
        public void ToSqlPredicate_ComplexNested_ReturnsValidSql()
        {
            // Arrange
            string filter = "not ((userName eq \"john\" and age gt 25) or (status eq \"active\" and department eq \"IT\"))";

            // Act
            string? result = filter.ToSqlPredicate();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.StartsWith("WHERE ", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Contains("NOT"));
            Assert.IsTrue(result.Contains("AND"));
            Assert.IsTrue(result.Contains("OR"));
        }

        [TestMethod]
        public void ToSqlPredicate_ComparisonOperators_ReturnsValidSql()
        {
            // Arrange
            string filter = "age gt 25 and salary lt 50000 and name co \"John\" and email sw \"john@\" and title ew \"Manager\"";

            // Act
            string? result = filter.ToSqlPredicate();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.StartsWith("WHERE ", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Contains(">"));
            Assert.IsTrue(result.Contains("<"));
            Assert.IsTrue(result.Contains("LIKE"));
        }

        [TestMethod]
        public void ToSqlPredicate_PresentOperator_ReturnsValidSql()
        {
            // Arrange
            string filter = "manager pr";

            // Act
            string? result = filter.ToSqlPredicate();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.StartsWith("WHERE ", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Contains("IS NOT NULL"));
        }

        #endregion

        #region Error Handling Tests

        [TestMethod]
        public void ToSqlPredicate_EscapeSequences_ReturnsValidSql()
        {
            // Arrange
            string filter = "description co \"Line 1\\nLine 2\"";

            // Act
            string? result = filter.ToSqlPredicate();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.StartsWith("WHERE ", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Contains("LIKE"));
        }

        [TestMethod]
        public void ToSqlPredicate_InvalidEscapeSequence_ThrowsException()
        {
            // Arrange
            string filter = "description co \"Line 1\\xLine 2\""; // Invalid escape sequence

            // Act & Assert
            var exception = Assert.ThrowsException<InvalidOperationException>(() => filter.ToSqlPredicate());
            Assert.IsTrue(exception.Message.Contains("Invalid SCIM filter"));
        }

        [TestMethod]
        public void ToSqlPredicate_UnbalancedParentheses_ThrowsException()
        {
            // Arrange
            string filter = "userName eq \"john\" and (age gt 25"; // Missing closing parenthesis

            // Act & Assert
            var exception = Assert.ThrowsException<InvalidOperationException>(() => filter.ToSqlPredicate());
            Assert.IsTrue(exception.Message.Contains("Invalid SCIM filter"));
        }

        [TestMethod]
        public void ToSqlPredicate_InvalidAttributeName_ThrowsException()
        {
            // Arrange
            string filter = "123invalid eq \"test\""; // Invalid attribute name starting with number

            // Act & Assert
            var exception = Assert.ThrowsException<InvalidOperationException>(() => filter.ToSqlPredicate());
            Assert.IsTrue(exception.Message.Contains("Invalid SCIM filter"));
        }

        [TestMethod]
        public void ToSqlPredicate_ConsecutiveDots_ThrowsException()
        {
            // Arrange
            string filter = "name..givenName eq \"John\""; // Consecutive dots

            // Act & Assert
            var exception = Assert.ThrowsException<InvalidOperationException>(() => filter.ToSqlPredicate());
            Assert.IsTrue(exception.Message.Contains("Invalid SCIM filter"));
        }

        [TestMethod]
        public void ToSqlPredicate_EmptyStringLiteral_ReturnsValidSql()
        {
            // Arrange
            string filter = "description eq \"\"";

            // Act
            string? result = filter.ToSqlPredicate();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.StartsWith("WHERE ", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void ToSqlPredicate_UnterminatedString_ThrowsException()
        {
            // Arrange
            string filter = "description eq \"unterminated"; // Unterminated string

            // Act & Assert
            var exception = Assert.ThrowsException<InvalidOperationException>(() => filter.ToSqlPredicate());
            Assert.IsTrue(exception.Message.Contains("Invalid SCIM filter"));
        }

        #endregion

        #region Performance Tests

        [TestMethod]
        [Timeout(5000)] // 5 seconds timeout
        public void ToSqlPredicate_VeryLongExpression_ShouldCompleteWithinTimeout()
        {
            // Arrange - Build a very long filter with 100+ conditions
            var conditions = Enumerable.Range(1, 100)
                .Select(i => $"userName{i} eq \"user{i}\"")
                .ToList();
            
            var longFilter = string.Join(" and ", conditions);

            // Act
            var result = longFilter.ToSqlPredicate();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.StartsWith("WHERE ", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Contains("AND"));
        }

        [TestMethod]
        public void ToSqlPredicate_MemoryUsage_ShouldNotExceedThreshold()
        {
            // Arrange
            var largeFilters = Enumerable.Range(1, 1000)
                .Select(i => $"userName{i} eq \"user{i}\"")
                .ToList();
            
            // Act & Assert
            var initialMemory = GC.GetTotalMemory(true);
            
            foreach (var filter in largeFilters)
            {
                var result = filter.ToSqlPredicate();
                Assert.IsNotNull(result);
            }
            
            var finalMemory = GC.GetTotalMemory(true);
            var memoryIncrease = finalMemory - initialMemory;
            
            // Should not increase more than 10MB
            Assert.IsTrue(memoryIncrease < 10 * 1024 * 1024, 
                $"Memory increase {memoryIncrease / (1024 * 1024)}MB exceeds 10MB threshold");
        }

        [TestMethod]
        public void ToSqlPredicate_RepeatedExecution_ShouldMaintainPerformance()
        {
            // Arrange
            var filter = "userName eq \"john\" and age gt 25 and department eq \"IT\"";
            var executionTimes = new List<long>();
            
            // Act - Execute 1000 times and measure performance
            for (int i = 0; i < 1000; i++)
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var result = filter.ToSqlPredicate();
                stopwatch.Stop();
                
                executionTimes.Add(stopwatch.ElapsedMilliseconds);
                Assert.IsNotNull(result);
            }
            
            // Assert - Average execution time should be under 10ms
            var averageTime = executionTimes.Average();
            Assert.IsTrue(averageTime < 10, $"Average execution time {averageTime}ms exceeds 10ms threshold");
        }

        #endregion

        #region Concurrency Tests

        [TestMethod]
        public void ToSqlPredicate_ConcurrentAccess_ShouldBeThreadSafe()
        {
            // Arrange
            var filters = Enumerable.Range(1, 100)
                .Select(i => $"userName{i} eq \"user{i}\"")
                .ToList();
            
            var results = new ConcurrentBag<string?>();
            var tasks = new List<Task>();
            
            // Act
            foreach (var filter in filters)
            {
                tasks.Add(Task.Run(() =>
                {
                    var result = filter.ToSqlPredicate();
                    results.Add(result);
                }));
            }
            
            Task.WaitAll(tasks.ToArray());
            
            // Assert
            Assert.AreEqual(filters.Count, results.Count);
            Assert.IsTrue(results.All(r => r != null));
            Assert.IsTrue(results.All(r => r!.StartsWith("WHERE ", StringComparison.OrdinalIgnoreCase)));
        }

        [TestMethod]
        public void ToSqlPredicate_ConcurrentParameterizedQueries_ShouldBeThreadSafe()
        {
            // Arrange
            var filters = Enumerable.Range(1, 50)
                .Select(i => $"userName{i} eq \"user{i}\" and age{i} gt {i}")
                .ToList();
            
            var results = new ConcurrentBag<(string Sql, Dictionary<string, object> Parameters)>();
            var tasks = new List<Task>();
            
            // Act
            foreach (var filter in filters)
            {
                tasks.Add(Task.Run(() =>
                {
                    var result = filter.ToSqlPredicateWithParameters();
                    results.Add(result);
                }));
            }
            
            Task.WaitAll(tasks.ToArray());
            
            // Assert
            Assert.AreEqual(filters.Count, results.Count);
            Assert.IsTrue(results.All(r => !string.IsNullOrEmpty(r.Sql)));
            Assert.IsTrue(results.All(r => r.Parameters.Count > 0));
        }

        #endregion

        #region Data Type Validation Tests

        [TestMethod]
        public void ToSqlPredicate_DataTypeValidation_ShouldHandleAllTypes()
        {
            // Arrange - Different data types
            var testCases = new[]
            {
                ("age eq 25", typeof(int)),
                ("salary eq 50000.50", typeof(decimal)),
                ("active eq true", typeof(bool)),
                ("lastLogin eq \"2023-01-01T10:00:00Z\"", typeof(string)), // Dates as strings in SCIM
                ("description eq \"text\"", typeof(string))
            };
            
            foreach (var (filter, expectedType) in testCases)
            {
                // Act
                var result = filter.ToSqlPredicateWithParameters();
                
                // Assert
                Assert.IsNotNull(result);
                Assert.IsTrue(result.Sql.StartsWith("WHERE ", StringComparison.OrdinalIgnoreCase));
                Assert.IsTrue(result.Parameters.Count > 0);
                
                var parameterValue = result.Parameters.Values.First();
                Assert.IsInstanceOfType(parameterValue, expectedType);
            }
        }

        [TestMethod]
        public void ToSqlPredicate_NumericValues_ShouldBeProperlyTyped()
        {
            // Arrange
            var numericFilters = new[]
            {
                "age eq 25",
                "salary eq 50000.50",
                "score eq 95.75",
                "count eq 1000"
            };
            
            foreach (var filter in numericFilters)
            {
                // Act
                var result = filter.ToSqlPredicateWithParameters();
                
                // Assert
                Assert.IsNotNull(result);
                Assert.IsTrue(result.Parameters.Count > 0);
                
                var parameterValue = result.Parameters.Values.First();
                Assert.IsTrue(parameterValue is int || parameterValue is decimal || parameterValue is double);
            }
        }

        #endregion

        #region Edge Cases Tests

        [TestMethod]
        public void ToSqlPredicate_EdgeCases_ShouldHandleGracefully()
        {
            // Arrange - Edge cases
            var edgeCases = new[]
            {
                "userName eq \"\"", // Empty string
                "userName eq \"   \"", // Whitespace only
                "userName eq \"\\\"quoted\\\"\"", // Escaped quotes
                "userName eq \"\\n\\t\\r\"", // Control characters
                "userName eq \"unicode: 🚀\"", // Unicode characters
                "userName eq \"very long string " + new string('x', 1000) + "\"", // Very long string
            };
            
            foreach (var filter in edgeCases)
            {
                // Act
                var result = filter.ToSqlPredicate();
                
                // Assert
                Assert.IsNotNull(result);
                Assert.IsTrue(result.StartsWith("WHERE ", StringComparison.OrdinalIgnoreCase));
            }
        }

        [TestMethod]
        public void ToSqlPredicate_SpecialCharacters_ShouldBeHandledCorrectly()
        {
            // Arrange - Special characters that might cause issues
            var specialCharFilters = new[]
            {
                "userName eq \"user@domain.com\"",
                "description eq \"file/path/with/slashes\"",
                "name eq \"O'Connor\"",
                "comment eq \"line1\\nline2\\tline3\"",
                "data eq \"{\\\"json\\\": \\\"value\\\"}\""
            };
            
            foreach (var filter in specialCharFilters)
            {
                // Act
                var result = filter.ToSqlPredicate();
                
                // Assert
                Assert.IsNotNull(result);
                Assert.IsTrue(result.StartsWith("WHERE ", StringComparison.OrdinalIgnoreCase));
            }
        }

        #endregion

        #region Regression Tests

        [TestMethod]
        public void ToSqlPredicate_Regression_ComplexNestedExpression_ShouldMaintainBehavior()
        {
            // Arrange - Known complex expression that was previously failing
            var complexFilter = "not ((userName eq \"john\" and age gt 25) or (status eq \"active\" and department eq \"IT\"))";
            
            // Act
            var result = complexFilter.ToSqlPredicate();
            
            // Assert - Specific expectations based on previous behavior
            Assert.IsNotNull(result);
            Assert.IsTrue(result.StartsWith("WHERE ", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Contains("NOT"));
            Assert.IsTrue(result.Contains("AND"));
            Assert.IsTrue(result.Contains("OR"));
        }

        [TestMethod]
        public void ToSqlPredicate_Regression_SchemaMapping_ShouldWorkCorrectly()
        {
            // Arrange - Complex schema mapping scenario
            var filter = "userName eq \"john\" and email eq \"john@example.com\"";
            var schemaMapping = new Dictionary<string, string>
            {
                { "userName", "dsNomeUsuario" },
                { "email", "dsEmail" }
            };
            
            // Act
            var result = filter.ToSqlPredicate(schemaMapping);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("dsNomeUsuario"));
            Assert.IsTrue(result.Contains("dsEmail"));
            Assert.IsFalse(result.Contains("userName"));
            Assert.IsFalse(result.Contains("email"));
        }

        #endregion

        [TestMethod]
        public void ToSqlPredicate_WithSchemaMapping_ShouldGenerateInlineSqlForStoredProcedures()
        {
            // Arrange
            var filter = "title co \"test\" and status eq \"active\"";
            var schemaMapping = new Dictionary<string, string>
            {
                ["title"] = "p.dstitulo",
                ["status"] = "p.nrSituacao"
            };

            // Act
            string? result = filter.ToSqlPredicate(schemaMapping);

            // Debug: Print the actual result
            Console.WriteLine($"Generated SQL: {result}");

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("p.dstitulo"), "Should contain mapped column name");
            Assert.IsTrue(result.Contains("p.nrSituacao"), "Should contain mapped column name");
            Assert.IsFalse(result.Contains("@p"), "Should not contain parameter placeholders for stored procedures");
            Assert.IsTrue(result.Contains("test"), "Should contain inline value");
            Assert.IsTrue(result.Contains("active"), "Should contain inline value");
        }

        [TestMethod]
        public void ToSqlPredicate_WithoutSchemaMapping_ShouldGenerateParameterizedSql()
        {
            // Arrange
            var filter = "title co \"test\" and status eq \"active\"";

            // Act
            string? result = filter.ToSqlPredicate();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("@p"), "Should contain parameter placeholders for direct execution");
            Assert.IsFalse(result.Contains("'test'"), "Should not contain inline values");
            Assert.IsFalse(result.Contains("'active'"), "Should not contain inline values");
        }

        [TestMethod]
        public void ToSqlPredicateWithParameters_NullLiteral_PreservesNull()
        {
            // Arrange - Test null literal preservation
            var filter = "manager eq null";

            // Act
            var result = filter.ToSqlPredicateWithParameters();



            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Sql.StartsWith("WHERE ", StringComparison.OrdinalIgnoreCase));
            
            // For null literals, SQL should use IS NULL (no parameters needed)
            Assert.IsTrue(result.Sql.Contains("IS NULL"), "Should use IS NULL for null literals");
            Assert.AreEqual(0, result.Parameters.Count, "No parameters needed for IS NULL");
        }

        [TestMethod]
        public void ToSqlPredicateWithParameters_StringLiteral_DoesNotConvertToNull()
        {
            // Arrange - Test that string literals are not converted to null
            var filter = "name eq \"John\"";

            // Act
            var result = filter.ToSqlPredicateWithParameters();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Sql.StartsWith("WHERE ", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(result.Parameters.Count > 0);
            
            // Should contain string value, not null
            var hasStringValue = result.Parameters.Values.Any(v => v is string);
            Assert.IsTrue(hasStringValue, "Parameters should contain string value");
            
            // Should not contain null values
            var hasNullValue = result.Parameters.Values.Any(v => v == null);
            Assert.IsFalse(hasNullValue, "Parameters should not contain null for string literal");
        }
    }
}

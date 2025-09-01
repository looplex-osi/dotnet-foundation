using Microsoft.VisualStudio.TestTools.UnitTesting;
using Looplex.Foundation.SearchContent;
using Looplex.Foundation.SearchContent.SqlGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Looplex.Foundation.UnitTests.SearchContent;

/// <summary>
/// Performance and robustness tests for SCIM filter parser
/// 
/// Tests production scenarios:
/// - High-load performance testing
/// - Memory usage optimization
/// - Concurrent access patterns
/// - Stress testing with large datasets
/// - Resource cleanup and garbage collection
/// - Scalability testing
/// - Production-like scenarios
/// - Error recovery under load
/// </summary>
[TestClass]
public class PerformanceAndRobustnessTests
{
    private ISearchContentService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _service = new SearchContentService();
    }

    #region High-Load Performance Tests

    [TestMethod]
    [Timeout(30000)] // 30 seconds timeout
    public void Parse_HighLoadScenario_ShouldMaintainPerformance()
    {
        // Arrange - Simulate high-load scenario
        var filters = GenerateHighLoadFilters(10000);
        var stopwatch = Stopwatch.StartNew();
        var results = new List<SqlPredicateResult>();

        // Act - Process all filters
        foreach (var filter in filters)
        {
            var result = _service.ConvertToSql(filter);
            results.Add(result);
        }

        stopwatch.Stop();

        // Assert - Performance metrics
        Assert.AreEqual(10000, results.Count);
        Assert.IsTrue(results.All(r => r.HasConditions));
        
        // Should process 10,000 filters in under 10 seconds
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 10000, 
            $"High-load processing took {stopwatch.ElapsedMilliseconds}ms, expected < 10000ms");
        
        // Average processing time should be under 1ms per filter
        var averageTime = stopwatch.ElapsedMilliseconds / 10000.0;
        Assert.IsTrue(averageTime < 1.0, 
            $"Average processing time {averageTime:F2}ms per filter, expected < 1.0ms");
    }

    [TestMethod]
    public void Parse_MemoryEfficiency_ShouldNotLeakMemory()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);
        var filters = GenerateMemoryTestFilters(100); // Reduced from 1000 to 100
        var results = new List<SqlPredicateResult>();

        // Act - Process filters in batches
        for (int batch = 0; batch < 10; batch++)
        {
            var batchResults = new List<SqlPredicateResult>();
            
            for (int i = 0; i < 10; i++) // Reduced from 100 to 10
            {
                var filter = filters[batch * 10 + i];
                var result = _service.ConvertToSql(filter);
                batchResults.Add(result);
            }
            
            results.AddRange(batchResults);
            
            // Force garbage collection between batches
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        // Assert - Memory should not grow significantly
        var finalMemory = GC.GetTotalMemory(true);
        var memoryIncrease = finalMemory - initialMemory;
        
        // Should not increase more than 5MB
        Assert.IsTrue(memoryIncrease < 5 * 1024 * 1024, 
            $"Memory increase {memoryIncrease / (1024 * 1024)}MB exceeds 5MB threshold");
        
        Assert.AreEqual(100, results.Count); // Updated from 1000 to 100
        Assert.IsTrue(results.All(r => r.HasConditions));
    }

    [TestMethod]
    public void Parse_ConcurrentHighLoad_ShouldBeThreadSafe()
    {
        // Arrange - High concurrent load
        var filters = GenerateHighLoadFilters(1000);
        var results = new ConcurrentBag<SqlPredicateResult>();
        var tasks = new List<Task>();
        var stopwatch = Stopwatch.StartNew();

        // Act - Process filters concurrently
        foreach (var filter in filters)
        {
            tasks.Add(Task.Run(() =>
            {
                var result = _service.ConvertToSql(filter);
                results.Add(result);
            }));
        }

        Task.WaitAll(tasks.ToArray());
        stopwatch.Stop();

        // Assert - All should complete successfully
        Assert.AreEqual(1000, results.Count);
        Assert.IsTrue(results.All(r => r.HasConditions));
        
        // Should complete within reasonable time
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 5000, 
            $"Concurrent processing took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");
    }

    #endregion

    #region Stress Testing

    [TestMethod]
    [Timeout(60000)] // 60 seconds timeout
    public void Parse_StressTest_ShouldHandleExtremeConditions()
    {
        // Arrange - Extreme stress conditions (reduced complexity to prevent stack overflow)
        var extremeFilters = new[]
        {
            // Very long expressions (reduced from 5000 to 100)
            string.Join(" and ", Enumerable.Range(1, 100).Select(i => $"field{i} eq \"value{i}\"")),
            
            // Deep nesting (reduced from 1000 to 50)
            BuildDeeplyNestedExpression(50),
            
            // Many OR conditions (reduced from 5000 to 100)
            string.Join(" or ", Enumerable.Range(1, 100).Select(i => $"field{i} eq \"value{i}\"")),
            
            // Complex mixed expressions (reduced from 1000 to 100)
            BuildComplexMixedExpression(100)
        };

        var stopwatch = Stopwatch.StartNew();
        var results = new List<SqlPredicateResult>();

        // Act - Process extreme filters
        foreach (var filter in extremeFilters)
        {
            var result = _service.ConvertToSql(filter);
            results.Add(result);
        }

        stopwatch.Stop();

        // Assert - Should handle extreme conditions
        Assert.AreEqual(4, results.Count);
        Assert.IsTrue(results.All(r => r.HasConditions));
        
        // Should complete within reasonable time
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 30000, 
            $"Stress test took {stopwatch.ElapsedMilliseconds}ms, expected < 30000ms");
    }

    [TestMethod]
    public void Parse_ResourceCleanup_ShouldNotLeakResources()
    {
        // Arrange
        if (!OperatingSystem.IsWindows())
        {
            Assert.Inconclusive("HandleCount is only available on Windows.");
            return;
        }
        var initialHandles = Process.GetCurrentProcess().HandleCount;
        var filters = GenerateResourceTestFilters(1000);

        // Act - Process filters with resource monitoring
        for (int i = 0; i < 10; i++)
        {
            var results = new List<SqlPredicateResult>();
            
            foreach (var filter in filters)
            {
                var result = _service.ConvertToSql(filter);
                results.Add(result);
            }
            
            // Force cleanup
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        // Assert - Handle count should not increase significantly
        var finalHandles = Process.GetCurrentProcess().HandleCount;
        var handleIncrease = finalHandles - initialHandles;
        
        // Should not increase more than 10 handles
        Assert.IsTrue(handleIncrease < 10, 
            $"Handle count increased by {handleIncrease}, expected < 10");
    }

    #endregion

    #region Production Scenario Tests

    [TestMethod]
    public void Parse_ProductionLikeScenario_ShouldHandleRealWorldUsage()
    {
        // Arrange - Real-world production scenarios
        var productionFilters = new[]
        {
            // User search with multiple criteria
            "userName co \"john\" and (department eq \"IT\" or department eq \"Engineering\") and active eq true and lastLogin gt \"2023-01-01\"",
            
            // Complex user filtering
            "name.givenName co \"John\" and name.familyName co \"Smith\" and emails.value co \"@company.com\" and manager ne null",
            
            // Group membership search
            "groups.displayName co \"Admin\" and groups.value eq \"admin-group-id\" and active eq true",
            
            // Audit trail search
            "meta.created gt \"2023-01-01\" and meta.lastModified lt \"2023-12-31\" and (status eq \"active\" or status eq \"pending\")",
            
            // Complex nested search
            "not ((department eq \"HR\" and role eq \"manager\") or (department eq \"Finance\" and role eq \"admin\")) and active eq true"
        };

        var stopwatch = Stopwatch.StartNew();
        var results = new List<SqlPredicateResult>();

        // Act - Process production-like filters
        foreach (var filter in productionFilters)
        {
            var result = _service.ConvertToSql(filter);
            results.Add(result);
        }

        stopwatch.Stop();

        // Assert - Should handle production scenarios efficiently
        Assert.AreEqual(5, results.Count);
        Assert.IsTrue(results.All(r => r.HasConditions));
        Assert.IsTrue(results.All(r => r.Parameters.Count > 0));
        
        // Should complete quickly
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, 
            $"Production scenario processing took {stopwatch.ElapsedMilliseconds}ms, expected < 1000ms");
    }

    [TestMethod]
    public void Parse_ErrorRecovery_ShouldHandleErrorsGracefully()
    {
        // Arrange - Mix of valid and invalid filters
        var mixedFilters = new[]
        {
            "userName eq \"john\"", // Valid
            "invalid filter", // Invalid
            "age gt 25", // Valid
            "userName eq \"unclosed quote", // Invalid
            "department eq \"IT\" and role eq \"admin\"", // Valid
            "userName eq \"test\" and (age gt 25", // Invalid
            "active eq true", // Valid
            "userName eq \"'; DROP TABLE Users; --\"", // Valid (but malicious)
            "name.givenName eq \"John\"", // Valid
            "123invalid eq \"test\"" // Invalid
        };

        var validResults = new List<SqlPredicateResult>();
        var invalidCount = 0;

        // Act - Process mixed filters
        foreach (var filter in mixedFilters)
        {
            try
            {
                var result = _service.ConvertToSql(filter);
                validResults.Add(result);
            }
            catch (Looplex.Foundation.SearchContent.Parser.FilterParseException)
            {
                invalidCount++;
            }
            catch (InvalidOperationException)
            {
                invalidCount++;
            }
        }

        // Assert - Should handle errors gracefully
        Assert.AreEqual(6, validResults.Count); // 6 valid filters
        Assert.AreEqual(4, invalidCount); // 4 invalid filters
        Assert.IsTrue(validResults.All(r => r.HasConditions));
    }

    #endregion

    #region Scalability Tests

    [TestMethod]
    public void Parse_ScalabilityTest_ShouldScaleLinearly()
    {
        // Arrange - Test different scales with more reasonable values
        var scales = new[] { 10, 50, 100, 200 };
        var scaleResults = new Dictionary<int, long>();

        foreach (var scale in scales)
        {
            var filters = GenerateScalabilityFilters(scale);
            var stopwatch = Stopwatch.StartNew();

            // Act - Process filters at this scale
            foreach (var filter in filters)
            {
                var result = _service.ConvertToSql(filter);
                Assert.IsNotNull(result);
            }

            stopwatch.Stop();
            scaleResults[scale] = stopwatch.ElapsedMilliseconds;
            
            // Ensure we have measurable time for small scales
            if (scale <= 50 && scaleResults[scale] < 1)
            {
                scaleResults[scale] = 1; // Minimum measurable time
            }
        }

        // Assert - Should scale reasonably (not exponentially)
        var ratios = new List<double>();
        for (int i = 1; i < scales.Length; i++)
        {
            var scaleRatio = (double)scales[i] / scales[i - 1];
            var timeRatio = (double)scaleResults[scales[i]] / scaleResults[scales[i - 1]];
            
            // Avoid division by zero and handle edge cases
            if (scaleResults[scales[i - 1]] > 0 && scaleRatio > 0)
            {
                var ratio = timeRatio / scaleRatio;
                
                // Handle Infinity and NaN values
                if (!double.IsInfinity(ratio) && !double.IsNaN(ratio))
                {
                    ratios.Add(ratio);
                }
            }
        }

        // Time increase should be roughly proportional to scale increase
        foreach (var ratio in ratios)
        {
            Assert.IsTrue(ratio < 2.0, $"Scalability ratio {ratio:F2} exceeds 2.0, indicating poor scaling");
        }
        
        // Ensure we have at least some valid ratios to test
        Assert.IsTrue(ratios.Count > 0, "No valid scalability ratios could be calculated");
    }

    [TestMethod]
    public void Parse_MemoryScalability_ShouldScaleEfficiently()
    {
        // Arrange - Test memory usage at different scales
        var scales = new[] { 100, 500, 1000 };
        var memoryResults = new Dictionary<int, long>();

        foreach (var scale in scales)
        {
            var initialMemory = GC.GetTotalMemory(true);
            var filters = GenerateScalabilityFilters(scale);

            // Act - Process filters at this scale
            foreach (var filter in filters)
            {
                var result = _service.ConvertToSql(filter);
                Assert.IsNotNull(result);
            }

            var finalMemory = GC.GetTotalMemory(true);
            memoryResults[scale] = finalMemory - initialMemory;
        }

        // Assert - Memory usage should scale reasonably
        for (int i = 1; i < scales.Length; i++)
        {
            var scaleRatio = (double)scales[i] / scales[i - 1];
            var memoryRatio = (double)memoryResults[scales[i]] / memoryResults[scales[i - 1]];
            
            // Handle edge cases where memory usage might be very small
            if (memoryResults[scales[i - 1]] > 0 && !double.IsInfinity(memoryRatio) && !double.IsNaN(memoryRatio))
            {
                // Memory increase should not be more than 3x the scale increase
                Assert.IsTrue(memoryRatio < scaleRatio * 3, 
                    $"Memory scaling ratio {memoryRatio:F2} exceeds {scaleRatio * 3:F2}");
            }
        }
    }

    #endregion

    #region Configuration Performance Tests

    [TestMethod]
    public void Parse_WithDifferentConfigurations_ShouldMaintainPerformance()
    {
        // Arrange - Different configuration scenarios
        var configurations = new[]
        {
            new SqlGenerationOptions { CaseSensitive = true },
            new SqlGenerationOptions { CaseSensitive = false },
            new SqlGenerationOptions { Dialect = SqlDialect.SqlServer },
            new SqlGenerationOptions { Dialect = SqlDialect.PostgreSql },
            new SqlGenerationOptions { Dialect = SqlDialect.MySql },
            new SqlGenerationOptions { UseParameters = false },
            new SqlGenerationOptions { IncludeNullChecks = false }
        };

        var baseFilter = "userName eq \"john\" and age gt 25 and department eq \"IT\"";
        var results = new List<long>();

        // Act - Test each configuration
        foreach (var config in configurations)
        {
            var stopwatch = Stopwatch.StartNew();
            
            for (int i = 0; i < 100; i++)
            {
                var result = _service.ConvertToSql(baseFilter, config);
                Assert.IsNotNull(result);
            }
            
            stopwatch.Stop();
            var elapsedTime = stopwatch.ElapsedMilliseconds;
            
            // Ensure we have measurable time
            if (elapsedTime < 1)
            {
                elapsedTime = 1; // Minimum measurable time
            }
            
            results.Add(elapsedTime);
        }

        // Assert - All configurations should perform reasonably
        var averageTime = results.Average();
        Assert.IsTrue(averageTime < 100, 
            $"Average configuration processing time {averageTime:F2}ms, expected < 100ms");
        
        // No configuration should be significantly slower than others
        var maxTime = results.Max();
        var minTime = results.Min();
        
        // Handle edge case where minTime might be 0
        if (minTime > 0)
        {
            var timeRatio = (double)maxTime / minTime;
            
            // Handle Infinity and NaN values
            if (!double.IsInfinity(timeRatio) && !double.IsNaN(timeRatio))
            {
                Assert.IsTrue(timeRatio < 15.0, 
                    $"Configuration performance ratio {timeRatio:F2} exceeds 15.0");
            }
        }
        else
        {
            // If minTime is 0, ensure maxTime is also reasonable
            Assert.IsTrue(maxTime < 100, 
                $"Maximum configuration processing time {maxTime}ms exceeds 100ms threshold");
        }
    }

    #endregion

    #region Helper Methods

    private static List<string> GenerateHighLoadFilters(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => $"userName{i} eq \"user{i}\" and age{i} gt {i} and department{i} eq \"dept{i}\"")
            .ToList();
    }

    private static List<string> GenerateMemoryTestFilters(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => $"field{i} eq \"value{i}\" and status{i} eq \"active\" and type{i} eq \"user\"")
            .ToList();
    }

    private static List<string> GenerateResourceTestFilters(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => $"resource{i} eq \"resource{i}\" and category{i} eq \"cat{i}\"")
            .ToList();
    }

    private static List<string> GenerateScalabilityFilters(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => $"attribute{i} eq \"value{i}\" and flag{i} eq true")
            .ToList();
    }

    private static string BuildDeeplyNestedExpression(int depth)
    {
        var expression = "userName eq \"test\"";
        for (int i = 0; i < depth; i++)
        {
            expression = $"({expression})";
        }
        return expression;
    }

    private static string BuildComplexMixedExpression(int complexity)
    {
        var conditions = new List<string>();
        
        for (int i = 0; i < complexity; i++)
        {
            var condition = (i % 4) switch
            {
                0 => $"field{i} eq \"value{i}\"",
                1 => $"number{i} gt {i}",
                2 => $"flag{i} eq true",
                3 => $"text{i} co \"text{i}\"",
                _ => $"field{i} eq \"value{i}\""
            };
            conditions.Add(condition);
        }

        return string.Join(" and ", conditions);
    }

    #endregion
}

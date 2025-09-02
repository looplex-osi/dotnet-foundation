using Xunit;
using System.Collections.Concurrent;
using Looplex.Foundation.SCIMv2.Queries;
using Looplex.Samples.Application;

namespace Notejam.Tests.Unit
{
    /// <summary>
    /// Testes de concorrência para processamento de filtros SCIM
    /// </summary>
    public class ScimFilterConcurrencyTests
    {
        private readonly Dictionary<string, string> _schemaMapping;

        public ScimFilterConcurrencyTests()
        {
            _schemaMapping = PadConfiguration.ScimMappings.AttributeToColumn;
        }

        #region Concurrency Tests

        [Fact]
        public void ScimFilter_ConcurrentProcessing_ShouldBeThreadSafe()
        {
            // Arrange
            var filters = Enumerable.Range(0, 10)
                .Select(i => $"name{i} eq \"test{i}\" and active eq true and status eq {i}")
                .ToArray();

            var results = new ConcurrentBag<(string Filter, object Result, Exception? Exception)>();

            // Act
            var tasks = filters.Select(filter => Task.Run(() =>
            {
                try
                {
                    var result = filter.ToSqlPredicateWithParameters(_schemaMapping);
                    results.Add((filter, result, null));
                }
                catch (Exception ex)
                {
                    results.Add((filter, null!, ex));
                }
            }));

            Task.WaitAll(tasks.ToArray());

            // Assert
            Assert.Equal(filters.Length, results.Count);
            
            var successfulResults = results.Where(r => r.Exception == null).ToList();
            var failedResults = results.Where(r => r.Exception != null).ToList();

            Assert.True(successfulResults.Count >= filters.Length * 0.9, 
                $"At least 90% should succeed, but only {successfulResults.Count}/{filters.Length} succeeded");

            foreach (var result in successfulResults)
            {
                var sqlResult = (ValueTuple<string, Dictionary<string, object>>)result.Result;
                Assert.True(sqlResult.Item1.Contains("p."), "Should contain table alias");
                Assert.True(sqlResult.Item2.Count > 0, "Should have parameters");
            }
        }

        [Fact]
        public void ScimFilter_ConcurrentProcessing_ShouldMaintainDataIntegrity()
        {
            // Arrange
            var testFilter = "name eq \"concurrent_test\" and active eq true";
            var concurrentResults = new ConcurrentBag<object>();
            var iterations = 100;

            // Act
            var tasks = Enumerable.Range(0, iterations)
                .Select(_ => Task.Run(() =>
                {
                    var result = testFilter.ToSqlPredicateWithParameters(_schemaMapping);
                    concurrentResults.Add(result);
                }));

            Task.WaitAll(tasks.ToArray());

            // Assert
            Assert.Equal(iterations, concurrentResults.Count);

            var firstResult = (ValueTuple<string, Dictionary<string, object>>)concurrentResults.First();
            var expectedSql = firstResult.Item1;
            var expectedParams = firstResult.Item2;

            foreach (var result in concurrentResults)
            {
                var sqlResult = (ValueTuple<string, Dictionary<string, object>>)result;
                Assert.Equal(expectedSql, sqlResult.Item1);
                Assert.Equal(expectedParams.Count, sqlResult.Item2.Count);
                
                foreach (var param in expectedParams)
                {
                    Assert.True(sqlResult.Item2.ContainsKey(param.Key));
                    Assert.Equal(param.Value, sqlResult.Item2[param.Key]);
                }
            }
        }

        [Fact]
        public void ScimFilter_ConcurrentProcessing_ShouldHandleMixedOperations()
        {
            // Arrange
            var operations = new[]
            {
                "name eq \"test1\"",
                "active eq true and status gt 0",
                "(name eq \"test2\" or active eq false) and status le 100",
                "name co \"test\" and active eq true",
                "name sw \"test\" and active eq true",
                "name ew \"test\" and active eq true",
                "name ne \"test\" and active eq true",
                "status gt 1 and status lt 10",
                "name pr and active eq true",
                "name eq null and active eq true"
            };

            var results = new ConcurrentDictionary<int, object>();
            var exceptions = new ConcurrentBag<Exception>();

            // Act
            var tasks = operations.Select((operation, index) => Task.Run(() =>
            {
                try
                {
                    var result = operation.ToSqlPredicateWithParameters(_schemaMapping);
                    results[index] = result;
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }));

            Task.WaitAll(tasks.ToArray());

            // Assert
            Assert.True(results.Count >= operations.Length * 0.8, 
                $"At least 80% should succeed, but only {results.Count}/{operations.Length} succeeded");

            Assert.True(exceptions.Count <= operations.Length * 0.2, 
                $"At most 20% should fail, but {exceptions.Count}/{operations.Length} failed");

            foreach (var result in results.Values)
            {
                var sqlResult = (ValueTuple<string, Dictionary<string, object>>)result;
                Assert.True(sqlResult.Item1.Contains("p."), "Should contain table alias");
            }
        }

        [Fact]
        public void ScimFilter_ConcurrentProcessing_ShouldHandleStressConditions()
        {
            // Arrange
            var stressFilters = Enumerable.Range(0, 50)
                .Select(i => GenerateStressFilter(i))
                .ToArray();

            var successfulResults = new ConcurrentBag<object>();
            var failedResults = new ConcurrentBag<Exception>();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var tasks = stressFilters.Select(filter => Task.Run(() =>
            {
                try
                {
                    var result = filter.ToSqlPredicateWithParameters(_schemaMapping);
                    successfulResults.Add(result);
                }
                catch (Exception ex)
                {
                    failedResults.Add(ex);
                }
            }));

            Task.WaitAll(tasks.ToArray());
            stopwatch.Stop();

            // Assert
            Assert.True(stopwatch.ElapsedMilliseconds < 5000, 
                $"Stress test took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");

            Assert.True(successfulResults.Count >= stressFilters.Length * 0.7, 
                $"At least 70% should succeed under stress, but only {successfulResults.Count}/{stressFilters.Length} succeeded");

            Assert.True(failedResults.Count <= stressFilters.Length * 0.3, 
                $"At most 30% should fail under stress, but {failedResults.Count}/{stressFilters.Length} failed");

            // Verify successful results are valid
            foreach (var result in successfulResults)
            {
                var sqlResult = (ValueTuple<string, Dictionary<string, object>>)result;
                Assert.True(sqlResult.Item1.Contains("WHERE"), "Should contain WHERE clause");
            }
        }

        [Fact]
        public void ScimFilter_ConcurrentProcessing_ShouldHandleResourceContention()
        {
            // Arrange
            var resourceIntensiveFilter = GenerateResourceIntensiveFilter();
            var concurrentTasks = 20;
            var results = new ConcurrentBag<object>();
            var exceptions = new ConcurrentBag<Exception>();

            // Act
            var tasks = Enumerable.Range(0, concurrentTasks)
                .Select(_ => Task.Run(() =>
                {
                    try
                    {
                        var result = resourceIntensiveFilter.ToSqlPredicateWithParameters(_schemaMapping);
                        results.Add(result);
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }));

            Task.WaitAll(tasks.ToArray());

            // Assert
            Assert.True(results.Count >= concurrentTasks * 0.8, 
                $"At least 80% should succeed under resource contention, but only {results.Count}/{concurrentTasks} succeeded");

            Assert.True(exceptions.Count <= concurrentTasks * 0.2, 
                $"At most 20% should fail under resource contention, but {exceptions.Count}/{concurrentTasks} failed");

            // All successful results should be identical (same input, same output)
            if (results.Count > 0)
            {
                var firstResult = (ValueTuple<string, Dictionary<string, object>>)results.First();
                foreach (var result in results)
                {
                    var sqlResult = (ValueTuple<string, Dictionary<string, object>>)result;
                    Assert.Equal(firstResult.Item1, sqlResult.Item1);
                    Assert.Equal(firstResult.Item2.Count, sqlResult.Item2.Count);
                }
            }
        }

        #endregion

        #region Helper Methods

        private static string GenerateStressFilter(int index)
        {
            var conditions = new List<string>();
            
            // Add various types of conditions
            conditions.Add($"name{index} eq \"stress_test_{index}\"");
            conditions.Add($"active eq {(index % 2 == 0 ? "true" : "false")}");
            conditions.Add($"status {(index % 3 == 0 ? "gt" : index % 3 == 1 ? "lt" : "eq")} {index}");
            
            if (index % 4 == 0)
            {
                conditions.Add($"name{index} co \"test\"");
            }
            
            if (index % 5 == 0)
            {
                conditions.Add($"(subname{index} eq \"sub_{index}\" or subactive{index} eq true)");
            }

            return string.Join(" and ", conditions);
        }

        private static string GenerateResourceIntensiveFilter()
        {
            // Create a complex filter that requires significant processing
            var conditions = Enumerable.Range(0, 20)
                .Select(i => $"(name{i} eq \"resource_test_{i}\" and active eq true and status eq {i})")
                .ToList();

            return string.Join(" or ", conditions);
        }

        #endregion
    }
}

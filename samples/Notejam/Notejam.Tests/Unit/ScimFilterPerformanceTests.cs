using Xunit;
using System.Diagnostics;
using System.Text;
using Looplex.Foundation.SCIMv2.Queries;
using Looplex.Samples.Application;

namespace Notejam.Tests.Unit
{
    /// <summary>
    /// Unit performance tests for SCIM filter processing
    /// </summary>
    public class ScimFilterPerformanceTests
    {
        private readonly Dictionary<string, string> _schemaMapping;

        public ScimFilterPerformanceTests()
        {
            _schemaMapping = PadConfiguration.ScimMappings.AttributeToColumn;
        }

        #region Performance Tests

        [Fact]
        public void ScimFilter_Performance_LargeFilter_ShouldCompleteWithinTimeout()
        {
            // Arrange
            var largeFilter = GenerateLargeFilter(100);
            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = largeFilter.ToSqlPredicateWithParameters(_schemaMapping);
            stopwatch.Stop();

            // Assert
            Assert.True(stopwatch.ElapsedMilliseconds < 100, 
                $"Parsing took {stopwatch.ElapsedMilliseconds}ms, expected < 100ms");
            Assert.True(!string.IsNullOrEmpty(result.Item1), "Should generate SQL");
        }

        [Fact]
        public void ScimFilter_Performance_ComplexNestedFilter_ShouldCompleteWithinTimeout()
        {
            // Arrange
            var complexFilter = GenerateComplexNestedFilter(10);
            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = complexFilter.ToSqlPredicateWithParameters(_schemaMapping);
            stopwatch.Stop();

            // Assert
            Assert.True(stopwatch.ElapsedMilliseconds < 50, 
                $"Complex parsing took {stopwatch.ElapsedMilliseconds}ms, expected < 50ms");
            Assert.True(!string.IsNullOrEmpty(result.Item1), "Should generate SQL");
        }

        [Fact]
        public void ScimFilter_Performance_MultipleFilters_ShouldMaintainConsistency()
        {
            // Arrange
            var filters = new[]
            {
                "name eq \"test1\"",
                "name eq \"test2\" and active eq true",
                "(name eq \"test3\" or active eq false) and status gt 0",
                "name co \"test\" and active eq true and status le 100"
            };

            var responseTimes = new List<long>();

            // Act
            foreach (var filter in filters)
            {
                var stopwatch = Stopwatch.StartNew();
                var result = filter.ToSqlPredicateWithParameters(_schemaMapping);
                stopwatch.Stop();

                responseTimes.Add(stopwatch.ElapsedMilliseconds);
                Assert.True(!string.IsNullOrEmpty(result.Item1), "Should generate SQL");
            }

            // Assert
            var avgResponseTime = responseTimes.Average();
            var maxResponseTime = responseTimes.Max();
            var minResponseTime = responseTimes.Min();

            Assert.True(avgResponseTime < 10, $"Average response time: {avgResponseTime}ms, expected < 10ms");
            Assert.True(maxResponseTime < 20, $"Max response time: {maxResponseTime}ms, expected < 20ms");
            Assert.True(minResponseTime >= 0, $"Min response time: {minResponseTime}ms, expected >= 0ms");

            // Consistency check - all should be similar (allow for very fast execution)
            var variance = responseTimes.Select(t => Math.Pow(t - avgResponseTime, 2)).Average();
            Assert.True(variance < 25, $"Response time variance: {variance}, expected < 25");
        }

        [Fact]
        public void ScimFilter_Performance_MemoryUsage_ShouldNotLeak()
        {
            // Arrange
            var initialMemory = GC.GetTotalMemory(true);
            var filters = Enumerable.Range(0, 50)
                .Select(i => $"name{i} eq \"test{i}\" and active eq true and status eq {i}");

            // Act
            foreach (var filter in filters)
            {
                filter.ToSqlPredicateWithParameters(_schemaMapping);
            }

            GC.Collect();
            var finalMemory = GC.GetTotalMemory(true);

            // Assert
            var memoryIncrease = finalMemory - initialMemory;
            Assert.True(memoryIncrease < 1024 * 1024, // 1MB
                $"Memory increase: {memoryIncrease} bytes, expected < 1MB");
        }

        [Fact]
        public void ScimFilter_Performance_StressTest_ShouldHandleExtremeConditions()
        {
            // Arrange
            var extremeFilters = new[]
            {
                GenerateLargeFilter(500), // Very large filter
                GenerateComplexNestedFilter(20), // Deep nesting
                GenerateRepetitiveFilter(100), // Many repetitions
            };

            var stopwatch = Stopwatch.StartNew();

            // Act
            foreach (var filter in extremeFilters)
            {
                try
                {
                    var result = filter.ToSqlPredicateWithParameters(_schemaMapping);
                    Assert.True(!string.IsNullOrEmpty(result.Item1), "Should generate SQL");
                }
                catch (Exception ex)
                {
                    // Extreme conditions might be rejected - that's acceptable
                    Assert.True(ex is InvalidOperationException || ex is ArgumentException,
                        $"Unexpected exception type: {ex.GetType().Name}");
                }
            }

            stopwatch.Stop();

            // Assert
            Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
                $"Stress test took {stopwatch.ElapsedMilliseconds}ms, expected < 1000ms");
        }

        [Fact]
        public void ScimFilter_Performance_ConcurrentProcessing_ShouldBeThreadSafe()
        {
            // Arrange
            var filters = Enumerable.Range(0, 10)
                .Select(i => $"name{i} eq \"test{i}\" and active eq true and status eq {i}")
                .ToArray();

            var tasks = filters.Select(filter => Task.Run(() =>
            {
                var stopwatch = Stopwatch.StartNew();
                var result = filter.ToSqlPredicateWithParameters(_schemaMapping);
                stopwatch.Stop();

                return new { Result = result, Time = stopwatch.ElapsedMilliseconds };
            }));

            // Act
            var results = Task.WhenAll(tasks).Result;

            // Assert
            Assert.All(results, r =>
            {
                Assert.True(!string.IsNullOrEmpty(r.Result.Item1), "Should generate SQL");
                Assert.True(r.Time < 100, $"Concurrent processing took {r.Time}ms, expected < 100ms");
            });

            var avgTime = results.Average(r => r.Time);
            Assert.True(avgTime < 60, $"Average concurrent processing time: {avgTime}ms, expected < 60ms");
        }

        #endregion

        #region Helper Methods

        private static string GenerateLargeFilter(int size)
        {
            var conditions = Enumerable.Range(0, size)
                .Select(i => $"name{i} eq \"value{i}\"");
            
            return string.Join(" and ", conditions);
        }

        private static string GenerateComplexNestedFilter(int depth)
        {
            if (depth <= 0) return "name eq \"base\"";
            
            var inner = GenerateComplexNestedFilter(depth - 1);
            return $"(name{depth} eq \"level{depth}\" and ({inner}))";
        }

        private static string GenerateRepetitiveFilter(int repetitions)
        {
            var conditions = Enumerable.Range(0, repetitions)
                .Select(i => $"name eq \"test{i}\"");
            
            return string.Join(" or ", conditions);
        }

        #endregion
    }
}

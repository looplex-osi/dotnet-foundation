using Xunit;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Net;
using System.Linq;
using Notejam.Tests.Common;

namespace Notejam.Tests.Performance
{
    /// <summary>
    /// Testes de performance avançados - Carga e Stress
    /// </summary>
    public class PerformanceTests : IDisposable
    {
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public PerformanceTests()
        {
            _client = new HttpClient();
            _client.BaseAddress = new Uri("http://localhost:7065/");
            _client.Timeout = TimeSpan.FromSeconds(60);
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public void Dispose()
        {
            _client?.Dispose();
        }

        #region Load Tests

        [Fact]
        [DemoWarning("Demo application not optimized for high concurrency - production should implement connection pooling and load balancing", "Performance")]
        public async Task LoadTest_ConcurrentRequests_ShouldHandleLoad()
        {
            // Arrange
            var requestCount = 50;
            var tasks = new List<Task<HttpResponseMessage>>();
            var stopwatch = Stopwatch.StartNew();

            // Act
            for (int i = 0; i < requestCount; i++)
            {
                tasks.Add(_client.GetAsync("/scim/v2/Notes"));
            }

            var responses = await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            Assert.Equal(requestCount, responses.Length);
            
            var successCount = responses.Count(r => r.IsSuccessStatusCode);
            var successRate = (double)successCount / requestCount;
            
            Assert.True(successRate >= 0.95, $"Success rate should be >= 95%, but was {successRate:P}");
            Assert.True(stopwatch.ElapsedMilliseconds < 30000, $"Total time should be < 30s, but was {stopwatch.ElapsedMilliseconds}ms");
            
            // Performance metrics
            var avgResponseTime = stopwatch.ElapsedMilliseconds / requestCount;
            Assert.True(avgResponseTime < 1000, $"Average response time should be < 1s, but was {avgResponseTime}ms");
        }

        [Fact]
        [DemoWarning("Demo application has basic performance characteristics - production should implement caching and optimization", "Performance")]
        public async Task LoadTest_SequentialRequests_ShouldMaintainPerformance()
        {
            // Arrange
            var requestCount = 20;
            var responseTimes = new List<long>();

            // Act
            for (int i = 0; i < requestCount; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                var response = await _client.GetAsync("/scim/v2/Notes");
                stopwatch.Stop();
                
                responseTimes.Add(stopwatch.ElapsedMilliseconds);
                Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound);
            }

            // Assert
            var avgResponseTime = responseTimes.Average();
            var maxResponseTime = responseTimes.Max();
            var minResponseTime = responseTimes.Min();
            
            Assert.True(avgResponseTime < 500, $"Average response time should be < 500ms, but was {avgResponseTime}ms");
            Assert.True(maxResponseTime < 2000, $"Max response time should be < 2s, but was {maxResponseTime}ms");
            Assert.True(minResponseTime > 0, "Min response time should be > 0");
        }

        #endregion

        #region Stress Tests

        [Fact]
        public async Task StressTest_HighConcurrency_ShouldNotFail()
        {
            // Arrange
            var requestCount = 100;
            var tasks = new List<Task<HttpResponseMessage>>();

            // Act
            for (int i = 0; i < requestCount; i++)
            {
                tasks.Add(_client.GetAsync("/scim/v2/Notes"));
                tasks.Add(_client.GetAsync("/scim/v2/Pads"));
            }

            var responses = await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(requestCount * 2, responses.Length);
            
            var successCount = responses.Count(r => r.IsSuccessStatusCode || r.StatusCode == HttpStatusCode.NotFound);
            var successRate = (double)successCount / responses.Length;
            
            Assert.True(successRate >= 0.90, $"Success rate should be >= 90%, but was {successRate:P}");
        }

        [Fact]
        public async Task StressTest_MixedOperations_ShouldHandleStress()
        {
            // Arrange
            var operations = new List<Task<HttpResponseMessage>>();
            var requestCount = 30;

            // Act - Mix of different operations
            for (int i = 0; i < requestCount; i++)
            {
                operations.Add(_client.GetAsync("/scim/v2/Notes"));
                operations.Add(_client.GetAsync("/scim/v2/Notes?filter=active eq true"));
                operations.Add(_client.GetAsync("/scim/v2/Notes?startIndex=1&count=10"));
                operations.Add(_client.GetAsync("/scim/v2/Pads"));
            }

            var responses = await Task.WhenAll(operations);

            // Assert
            Assert.Equal(requestCount * 4, responses.Length);
            
            var successCount = responses.Count(r => r.IsSuccessStatusCode || r.StatusCode == HttpStatusCode.NotFound);
            var successRate = (double)successCount / responses.Length;
            
            Assert.True(successRate >= 0.85, $"Success rate should be >= 85%, but was {successRate:P}");
        }

        #endregion

        #region Memory Tests

        [Fact]
        public async Task MemoryTest_LargePayloads_ShouldHandleCorrectly()
        {
            // Arrange
            var largeContent = new string('A', 10000); // 10KB payload
            var json = JsonSerializer.Serialize(new { name = "Large Note", text = largeContent }, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var stopwatch = Stopwatch.StartNew();
            var response = await _client.PostAsync("/scim/v2/Notes", content);
            stopwatch.Stop();

            // Assert
            Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound);
            Assert.True(stopwatch.ElapsedMilliseconds < 5000, $"Large payload should be processed in < 5s, but took {stopwatch.ElapsedMilliseconds}ms");
        }

        #endregion

        #region Endurance Tests

        [Fact]
        public async Task EnduranceTest_SustainedLoad_ShouldMaintainPerformance()
        {
            // Arrange
            var iterations = 10;
            var requestsPerIteration = 10;
            var responseTimes = new List<long>();

            // Act
            for (int iteration = 0; iteration < iterations; iteration++)
            {
                var tasks = new List<Task<HttpResponseMessage>>();
                
                for (int i = 0; i < requestsPerIteration; i++)
                {
                    tasks.Add(_client.GetAsync("/scim/v2/Notes"));
                }

                var stopwatch = Stopwatch.StartNew();
                var responses = await Task.WhenAll(tasks);
                stopwatch.Stop();

                responseTimes.Add(stopwatch.ElapsedMilliseconds);
                
                var successCount = responses.Count(r => r.IsSuccessStatusCode || r.StatusCode == HttpStatusCode.NotFound);
                Assert.Equal(requestsPerIteration, successCount);
            }

            // Assert
            var avgResponseTime = responseTimes.Average();
            var maxResponseTime = responseTimes.Max();
            var minResponseTime = responseTimes.Min();
            var variance = responseTimes.Select(t => Math.Pow(t - avgResponseTime, 2)).Average();
            var standardDeviation = Math.Sqrt(variance);

            Assert.True(avgResponseTime < 1000, $"Average response time should be < 1s, but was {avgResponseTime}ms");
            Assert.True(maxResponseTime < 3000, $"Max response time should be < 3s, but was {maxResponseTime}ms");
            Assert.True(standardDeviation < 500, $"Response time variance should be low, but SD was {standardDeviation}ms");
        }

        #endregion

        #region Scalability Tests

        [Theory]
        [InlineData(10)]
        [InlineData(25)]
        [InlineData(50)]
        public async Task ScalabilityTest_DifferentLoads_ShouldScaleLinearly(int requestCount)
        {
            // Arrange
            var tasks = new List<Task<HttpResponseMessage>>();
            var stopwatch = Stopwatch.StartNew();

            // Act
            for (int i = 0; i < requestCount; i++)
            {
                tasks.Add(_client.GetAsync("/scim/v2/Notes"));
            }

            var responses = await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            Assert.Equal(requestCount, responses.Length);
            
            var successCount = responses.Count(r => r.IsSuccessStatusCode || r.StatusCode == HttpStatusCode.NotFound);
            var successRate = (double)successCount / requestCount;
            
            Assert.True(successRate >= 0.95, $"Success rate should be >= 95%, but was {successRate:P}");
            
            // Performance should scale reasonably
            var avgResponseTime = stopwatch.ElapsedMilliseconds / requestCount;
            var expectedMaxTime = Math.Min(1000, requestCount * 10); // Linear scaling with reasonable limits
            Assert.True(avgResponseTime < expectedMaxTime, $"Average response time should be < {expectedMaxTime}ms, but was {avgResponseTime}ms");
        }

        #endregion
    }
}

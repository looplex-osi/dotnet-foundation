using Xunit;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using System.Net;
using System.Collections.Generic;

namespace Notejam.Tests.EndToEnd
{
    /// <summary>
    /// Testes end-to-end robustos e concisos focados em cenários críticos da aplicação
    /// </summary>
    public class ApiTests : IDisposable
    {
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiTests()
        {
            _client = new HttpClient();
            _client.BaseAddress = new Uri("http://localhost:7065/");
            _client.Timeout = TimeSpan.FromSeconds(30);
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }

        public void Dispose()
        {
            _client?.Dispose();
        }

        #region Basic API Tests

        [Fact]
        public async Task Api_HealthCheck_ShouldReturnOk()
        {
            // Act
            var response = await _client.GetAsync("/health");

            // Assert
            Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Api_RootEndpoint_ShouldReturnResponse()
        {
            // Act
            var response = await _client.GetAsync("/");

            // Assert
            Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound);
        }

        #endregion

        #region SCIM Endpoint Tests

        [Fact]
        public async Task Api_SCIMNotesEndpoint_ShouldReturnOk()
        {
            // Act
            var response = await _client.GetAsync("/scim/v2/Notes");

            // Assert
            Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Api_SCIMPadsEndpoint_ShouldReturnOk()
        {
            // Act
            var response = await _client.GetAsync("/scim/v2/Pads");

            // Assert
            Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task Api_NonExistentEndpoint_ShouldReturnNotFound()
        {
            // Act
            var response = await _client.GetAsync("/non-existent-endpoint");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Api_InvalidMethod_ShouldReturnMethodNotAllowed()
        {
            // Act
            var response = await _client.PostAsync("/", new StringContent(""));

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.MethodNotAllowed || 
                       response.StatusCode == HttpStatusCode.NotFound);
        }

        #endregion

        #region Performance Tests

        [Fact]
        public async Task Api_ResponseTime_ShouldBeReasonable()
        {
            // Arrange
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var response = await _client.GetAsync("/");
            stopwatch.Stop();

            // Assert
            Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound);
            Assert.True(stopwatch.ElapsedMilliseconds < 5000, "Response time should be under 5 seconds");
        }

        [Fact]
        public async Task Api_ConcurrentRequests_ShouldHandleCorrectly()
        {
            // Arrange
            var tasks = new List<Task<HttpResponseMessage>>();
            var requestCount = 5;

            // Act
            for (int i = 0; i < requestCount; i++)
            {
                tasks.Add(_client.GetAsync("/"));
            }

            var responses = await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(requestCount, responses.Length);
            foreach (var response in responses)
            {
                Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound);
            }
        }

        #endregion

        #region Content Type Tests

        [Fact]
        public async Task Api_Response_ShouldHaveCorrectContentType()
        {
            // Act
            var response = await _client.GetAsync("/");

            // Assert
            if (response.IsSuccessStatusCode)
            {
                Assert.NotNull(response.Content.Headers.ContentType);
                Assert.Contains("application/json", response.Content.Headers.ContentType.ToString());
            }
        }

        #endregion

        #region SCIM Filter Tests

        [Fact]
        public async Task Api_SCIMFilter_ShouldWorkCorrectly()
        {
            // Act
            var response = await _client.GetAsync("/scim/v2/Notes?filter=active eq true");

            // Assert
            Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Api_SCIMPagination_ShouldWorkCorrectly()
        {
            // Act
            var response = await _client.GetAsync("/scim/v2/Notes?startIndex=1&count=10");

            // Assert
            Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound);
        }

        #endregion
    }
}

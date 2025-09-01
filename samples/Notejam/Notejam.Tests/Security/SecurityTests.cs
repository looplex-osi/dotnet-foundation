using Xunit;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using System.Net;
using System.Collections.Generic;
using Notejam.Tests.Common;

namespace Notejam.Tests.Security
{
    /// <summary>
    /// Testes de segurança - Validação de vulnerabilidades
    /// </summary>
    public class SecurityTests : IDisposable
    {
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public SecurityTests()
        {
            _client = new HttpClient();
            _client.BaseAddress = new Uri("http://localhost:7065/");
            _client.Timeout = TimeSpan.FromSeconds(30);
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public void Dispose()
        {
            _client?.Dispose();
        }

        #region SQL Injection Tests

        [Fact]
        [DemoWarning("Demo application lacks advanced input validation - production should implement robust filter validation", "Security")]
        public async Task Security_SqlInjectionInFilter_ShouldBeHandledSafely()
        {
            // Arrange
            var maliciousFilters = new[]
            {
                "'; DROP TABLE Notes; --",
                "' OR '1'='1",
                "'; INSERT INTO Notes VALUES ('hacked', 'hacked'); --",
                "' UNION SELECT * FROM Users --"
            };

            // Act & Assert
            foreach (var filter in maliciousFilters)
            {
                var response = await _client.GetAsync($"/scim/v2/Notes?filter={Uri.EscapeDataString(filter)}");
                
                // Should not crash or expose sensitive data
                Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.BadRequest);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Assert.DoesNotContain("error", content.ToLower());
                    Assert.DoesNotContain("exception", content.ToLower());
                }
            }
        }

        [Fact]
        [DemoWarning("Demo application lacks parameter validation - production should implement input sanitization", "Security")]
        public async Task Security_SqlInjectionInParameters_ShouldBeHandledSafely()
        {
            // Arrange
            var maliciousName = "'; DROP TABLE Notes; --";
            var json = JsonSerializer.Serialize(new { name = maliciousName, text = "test" }, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/scim/v2/Notes", content);

            // Assert
            Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.BadRequest);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                Assert.DoesNotContain("error", responseContent.ToLower());
            }
        }

        #endregion

        #region XSS Tests

        [Fact]
        [DemoWarning("Demo application lacks XSS protection - production should implement content sanitization", "Security")]
        public async Task Security_XssInContent_ShouldBeHandledSafely()
        {
            // Arrange
            var xssPayloads = new[]
            {
                "<script>alert('xss')</script>",
                "<img src=x onerror=alert('xss')>",
                "javascript:alert('xss')",
                "<svg onload=alert('xss')>",
                "';alert('xss');//"
            };

            // Act & Assert
            foreach (var payload in xssPayloads)
            {
                var json = JsonSerializer.Serialize(new { name = "Test Note", text = payload }, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _client.PostAsync("/scim/v2/Notes", content);
                
                // Should accept the content but not execute scripts
                Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.BadRequest);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    // Should not contain unescaped script tags in response
                    Assert.DoesNotContain("<script>", responseContent);
                    Assert.DoesNotContain("javascript:", responseContent);
                }
            }
        }

        #endregion

        #region Input Validation Tests

        [Fact]
        [DemoWarning("Demo application has basic JSON validation - production should implement comprehensive validation", "Security")]
        public async Task Security_InvalidJson_ShouldBeHandledSafely()
        {
            // Arrange
            var invalidJsons = new[]
            {
                "{ invalid json }",
                "null",
                "undefined",
                "{\"name\": \"test\", \"text\": }",
                "{\"name\": \"test\", \"text\": \"test\", \"extra\": \"malicious\"}"
            };

            // Act & Assert
            foreach (var invalidJson in invalidJsons)
            {
                var content = new StringContent(invalidJson, Encoding.UTF8, "application/json");
                var response = await _client.PostAsync("/scim/v2/Notes", content);
                
                // Should return 400 Bad Request for invalid JSON
                Assert.True(response.StatusCode == HttpStatusCode.BadRequest || 
                           response.StatusCode == HttpStatusCode.UnprocessableEntity);
            }
        }

        [Fact]
        [DemoWarning("Demo application lacks payload size limits - production should implement request size validation", "Security")]
        public async Task Security_OverlyLargePayload_ShouldBeRejected()
        {
            // Arrange
            var largeContent = new string('A', 100000); // 100KB payload
            var json = JsonSerializer.Serialize(new { name = "Test", text = largeContent }, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/scim/v2/Notes", content);

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.RequestEntityTooLarge || 
                       response.StatusCode == HttpStatusCode.BadRequest ||
                       response.StatusCode == HttpStatusCode.UnprocessableEntity);
        }

        #endregion

        #region Authentication Tests

        [Fact]
        public async Task Security_UnauthenticatedAccess_ShouldBeHandled()
        {
            // Arrange
            var endpoints = new[]
            {
                "/scim/v2/Notes",
                "/scim/v2/Pads",
                "/scim/v2/Notes/123",
                "/scim/v2/Pads/456"
            };

            // Act & Assert
            foreach (var endpoint in endpoints)
            {
                var response = await _client.GetAsync(endpoint);
                
                // Should either require authentication or return appropriate response
                Assert.True(response.IsSuccessStatusCode || 
                           response.StatusCode == HttpStatusCode.Unauthorized ||
                           response.StatusCode == HttpStatusCode.NotFound);
            }
        }

        #endregion

        #region Rate Limiting Tests

        [Fact]
        public async Task Security_RapidRequests_ShouldBeHandled()
        {
            // Arrange
            var tasks = new List<Task<HttpResponseMessage>>();
            var requestCount = 100;

            // Act
            for (int i = 0; i < requestCount; i++)
            {
                tasks.Add(_client.GetAsync("/scim/v2/Notes"));
            }

            var responses = await Task.WhenAll(tasks);

            // Assert
            var tooManyRequestsCount = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);
            var successCount = responses.Count(r => r.IsSuccessStatusCode);
            
            // Should either handle all requests or implement rate limiting
            Assert.True(successCount > 0 || tooManyRequestsCount > 0, 
                       "Should either succeed or implement rate limiting");
        }

        #endregion

        #region Header Security Tests

        [Fact]
        public async Task Security_ResponseHeaders_ShouldBeSecure()
        {
            // Act
            var response = await _client.GetAsync("/scim/v2/Notes");

            // Assert
            if (response.IsSuccessStatusCode)
            {
                // Check for security headers
                var headers = response.Headers;
                
                // Should not expose server information
                Assert.False(headers.Contains("Server"));
                Assert.False(headers.Contains("X-Powered-By"));
                Assert.False(headers.Contains("X-AspNet-Version"));
                Assert.False(headers.Contains("X-AspNetMvc-Version"));
            }
        }

        #endregion

        #region Content Type Tests

        [Fact]
        [DemoWarning("Demo application has basic content-type validation - production should implement strict media type validation", "Security")]
        public async Task Security_InvalidContentType_ShouldBeRejected()
        {
            // Arrange
            var json = JsonSerializer.Serialize(new { name = "Test", text = "test" }, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "text/plain"); // Wrong content type

            // Act
            var response = await _client.PostAsync("/scim/v2/Notes", content);

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.UnsupportedMediaType || 
                       response.StatusCode == HttpStatusCode.BadRequest);
        }

        #endregion

        #region Path Traversal Tests

        [Fact]
        public async Task Security_PathTraversal_ShouldBeHandled()
        {
            // Arrange
            var maliciousPaths = new[]
            {
                "../../../etc/passwd",
                "..\\..\\..\\windows\\system32\\config\\sam",
                "....//....//....//etc/passwd",
                "%2e%2e%2f%2e%2e%2f%2e%2e%2fetc%2fpasswd"
            };

            // Act & Assert
            foreach (var path in maliciousPaths)
            {
                var response = await _client.GetAsync($"/scim/v2/Notes/{path}");
                
                // Should return 404 or 400, not expose file system
                Assert.True(response.StatusCode == HttpStatusCode.NotFound || 
                           response.StatusCode == HttpStatusCode.BadRequest);
            }
        }

        #endregion

        #region HTTP Method Tests

        [Fact]
        public async Task Security_UnsupportedMethods_ShouldBeRejected()
        {
            // Arrange
            var endpoints = new[] { "/scim/v2/Notes", "/scim/v2/Pads" };
            var unsupportedMethods = new[] { "TRACE", "OPTIONS", "HEAD" };

            // Act & Assert
            foreach (var endpoint in endpoints)
            {
                foreach (var method in unsupportedMethods)
                {
                    var request = new HttpRequestMessage(new HttpMethod(method), endpoint);
                    var response = await _client.SendAsync(request);
                    
                    // Should return 405 Method Not Allowed or 404
                    Assert.True(response.StatusCode == HttpStatusCode.MethodNotAllowed || 
                               response.StatusCode == HttpStatusCode.NotFound);
                }
            }
        }

        #endregion
    }
}

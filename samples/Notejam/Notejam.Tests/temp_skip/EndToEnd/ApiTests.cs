using Xunit;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq; // Added for Select

namespace Notejam.Tests.EndToEnd
{
    /// <summary>
    /// Testes end-to-end focados em fluxos completos da API
    /// </summary>
    public class ApiTests : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiTests()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("http://localhost:5000/"); // Ajustar conforme necessário
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        #region Complete Note Lifecycle Tests

        [Fact(Skip = "Requires running application")]
        public async Task CompleteNoteLifecycle_CreateReadUpdateDelete_ShouldWorkCorrectly()
        {
            // Arrange
            var createNoteRequest = new
            {
                name = "E2E Test Note",
                text = "This is a test note for end-to-end testing",
                active = true,
                status = 1
            };

            // Act 1: Create Note
            var createJson = JsonSerializer.Serialize(createNoteRequest, _jsonOptions);
            var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");
            var createResponse = await _httpClient.PostAsync("api/notes", createContent);

            // Assert 1: Create successful
            Assert.True(createResponse.IsSuccessStatusCode);
            var createdNote = JsonSerializer.Deserialize<dynamic>(await createResponse.Content.ReadAsStringAsync());
            Assert.NotNull(createdNote);
            Assert.Equal(createNoteRequest.name, createdNote.GetProperty("name").GetString());

            // Act 2: Read Note
            var noteId = createdNote.GetProperty("id").GetString();
            var getResponse = await _httpClient.GetAsync($"api/notes/{noteId}");

            // Assert 2: Read successful
            Assert.True(getResponse.IsSuccessStatusCode);
            var retrievedNote = JsonSerializer.Deserialize<dynamic>(await getResponse.Content.ReadAsStringAsync());
            Assert.Equal(createNoteRequest.name, retrievedNote.GetProperty("name").GetString());

            // Act 3: Update Note
            var updateNoteRequest = new
            {
                name = "Updated E2E Test Note",
                text = "This note has been updated",
                active = false,
                status = 2
            };
            var updateJson = JsonSerializer.Serialize(updateNoteRequest, _jsonOptions);
            var updateContent = new StringContent(updateJson, Encoding.UTF8, "application/json");
            var updateResponse = await _httpClient.PutAsync($"api/notes/{noteId}", updateContent);

            // Assert 3: Update successful
            Assert.True(updateResponse.IsSuccessStatusCode);
            var updatedNote = JsonSerializer.Deserialize<dynamic>(await updateResponse.Content.ReadAsStringAsync());
            Assert.Equal(updateNoteRequest.name, updatedNote.GetProperty("name").GetString());
            Assert.Equal(updateNoteRequest.text, updatedNote.GetProperty("text").GetString());

            // Act 4: Delete Note
            var deleteResponse = await _httpClient.DeleteAsync($"api/notes/{noteId}");

            // Assert 4: Delete successful
            Assert.True(deleteResponse.IsSuccessStatusCode);

            // Act 5: Verify Deletion
            var verifyDeleteResponse = await _httpClient.GetAsync($"api/notes/{noteId}");

            // Assert 5: Note no longer exists
            Assert.Equal(System.Net.HttpStatusCode.NotFound, verifyDeleteResponse.StatusCode);
        }

        #endregion

        #region SCIM Filter Workflow Tests

        [Fact(Skip = "Requires running application")]
        public async Task SCIMFilterWorkflow_ComplexFilteringAndPagination_ShouldWorkCorrectly()
        {
            // Arrange: Create multiple notes with different characteristics
            var notes = new List<string>();
            
            for (int i = 1; i <= 5; i++)
            {
                var createRequest = new
                {
                    name = $"Filter Test Note {i}",
                    text = $"Content for note {i}",
                    active = i % 2 == 0, // Even numbers are active
                    status = i
                };

                var createJson = JsonSerializer.Serialize(createRequest, _jsonOptions);
                var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");
                var createResponse = await _httpClient.PostAsync("api/notes", createContent);

                if (createResponse.IsSuccessStatusCode)
                {
                    var createdNote = JsonSerializer.Deserialize<dynamic>(await createResponse.Content.ReadAsStringAsync());
                    notes.Add(createdNote.GetProperty("id").GetString());
                }
            }

            // Act: Test complex SCIM filter with pagination
            var filter = "name co \"Filter Test\" and active eq true";
            var queryResponse = await _httpClient.GetAsync($"api/notes?filter={Uri.EscapeDataString(filter)}&page=1&pageSize=3&sortBy=name&sortOrder=asc");

            // Assert: Filter and pagination work correctly
            Assert.True(queryResponse.IsSuccessStatusCode);
            var queryResult = JsonSerializer.Deserialize<dynamic>(await queryResponse.Content.ReadAsStringAsync());
            
            Assert.NotNull(queryResult);
            Assert.True(queryResult.TryGetProperty("items", out var items));
            Assert.True(items.GetArrayLength() <= 3); // Page size limit

            // Verify all returned items match the filter criteria
            foreach (var item in items.EnumerateArray())
            {
                var name = item.GetProperty("name").GetString();
                var active = item.GetProperty("active").GetBoolean();
                
                Assert.Contains("Filter Test", name);
                Assert.True(active);
            }

            // Cleanup: Delete created notes
            foreach (var noteId in notes)
            {
                await _httpClient.DeleteAsync($"api/notes/{noteId}");
            }
        }

        #endregion

        #region Error Scenarios Tests

        [Fact(Skip = "Requires running application")]
        public async Task ErrorScenarios_InvalidRequests_ShouldReturnAppropriateErrors()
        {
            // Act 1: Try to create note with invalid data
            var invalidRequest = new
            {
                name = "", // Invalid: empty name
                text = "Valid content",
                active = true
            };

            var createJson = JsonSerializer.Serialize(invalidRequest, _jsonOptions);
            var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");
            var createResponse = await _httpClient.PostAsync("api/notes", createContent);

            // Assert 1: Should return error for invalid data
            Assert.False(createResponse.IsSuccessStatusCode);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, createResponse.StatusCode);

            // Act 2: Try to get non-existent note
            var nonExistentId = Guid.NewGuid().ToString();
            var getResponse = await _httpClient.GetAsync($"api/notes/{nonExistentId}");

            // Assert 2: Should return 404 for non-existent resource
            Assert.Equal(System.Net.HttpStatusCode.NotFound, getResponse.StatusCode);

            // Act 3: Try to update non-existent note
            var updateRequest = new
            {
                name = "Updated Name",
                text = "Updated content",
                active = true
            };

            var updateJson = JsonSerializer.Serialize(updateRequest, _jsonOptions);
            var updateContent = new StringContent(updateJson, Encoding.UTF8, "application/json");
            var updateResponse = await _httpClient.PutAsync($"api/notes/{nonExistentId}", updateContent);

            // Assert 3: Should return 404 for non-existent resource
            Assert.Equal(System.Net.HttpStatusCode.NotFound, updateResponse.StatusCode);
        }

        #endregion

        #region Business Logic Tests

        [Fact(Skip = "Requires running application")]
        public async Task BusinessLogic_NoteStatusValidation_ShouldEnforceBusinessRules()
        {
            // Arrange: Create a note
            var createRequest = new
            {
                name = "Status Test Note",
                text = "Content for status testing",
                active = true,
                status = 1
            };

            var createJson = JsonSerializer.Serialize(createRequest, _jsonOptions);
            var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");
            var createResponse = await _httpClient.PostAsync("api/notes", createContent);

            Assert.True(createResponse.IsSuccessStatusCode);
            var createdNote = JsonSerializer.Deserialize<dynamic>(await createResponse.Content.ReadAsStringAsync());
            var noteId = createdNote.GetProperty("id").GetString();

            // Act: Try to update with invalid status
            var invalidUpdateRequest = new
            {
                name = "Status Test Note",
                text = "Content for status testing",
                active = true,
                status = 256 // Invalid: exceeds maximum
            };

            var updateJson = JsonSerializer.Serialize(invalidUpdateRequest, _jsonOptions);
            var updateContent = new StringContent(updateJson, Encoding.UTF8, "application/json");
            var updateResponse = await _httpClient.PutAsync($"api/notes/{noteId}", updateContent);

            // Assert: Should return error for invalid status
            Assert.False(updateResponse.IsSuccessStatusCode);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, updateResponse.StatusCode);

            // Cleanup
            await _httpClient.DeleteAsync($"api/notes/{noteId}");
        }

        #endregion

        #region Performance Tests

        [Fact(Skip = "Requires running application")]
        public async Task Performance_BulkOperations_ShouldHandleMultipleRequests()
        {
            // Arrange: Create multiple notes in parallel
            var tasks = new List<Task<HttpResponseMessage>>();
            var noteIds = new List<string>();

            for (int i = 1; i <= 10; i++)
            {
                var createRequest = new
                {
                    name = $"Bulk Test Note {i}",
                    text = $"Content for bulk test {i}",
                    active = true,
                    status = 1
                };

                var createJson = JsonSerializer.Serialize(createRequest, _jsonOptions);
                var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");
                tasks.Add(_httpClient.PostAsync("api/notes", createContent));
            }

            // Act: Execute all create requests in parallel
            var responses = await Task.WhenAll(tasks);

            // Assert: All requests should succeed
            foreach (var response in responses)
            {
                Assert.True(response.IsSuccessStatusCode);
                var createdNote = JsonSerializer.Deserialize<dynamic>(await response.Content.ReadAsStringAsync());
                noteIds.Add(createdNote.GetProperty("id").GetString());
            }

            // Act: Query all notes
            var queryResponse = await _httpClient.GetAsync("api/notes?page=1&pageSize=20");

            // Assert: Query should return results
            Assert.True(queryResponse.IsSuccessStatusCode);
            var queryResult = JsonSerializer.Deserialize<dynamic>(await queryResponse.Content.ReadAsStringAsync());
            Assert.NotNull(queryResult);

            // Cleanup: Delete all created notes
            var deleteTasks = noteIds.Select(id => _httpClient.DeleteAsync($"api/notes/{id}"));
            await Task.WhenAll(deleteTasks);
        }

        #endregion
    }
}

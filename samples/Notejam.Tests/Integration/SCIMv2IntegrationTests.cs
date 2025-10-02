using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Text;
using Xunit;
using Xunit.Abstractions;
using Looplex.Samples.Domain.Entities;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;

namespace Looplex.Samples.Tests.Integration
{
    /// <summary>
    /// Testes de integração sistemáticos para diagnosticar problema SCIM
    /// </summary>
    public class SCIMv2IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly ITestOutputHelper _output;

        public SCIMv2IntegrationTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
        {
            _factory = factory;
            _output = output;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task Test01_GET_Notes_Should_Work()
        {
            // Arrange
            _output.WriteLine("🧪 TESTE 1: GET /notes deve funcionar");
            
            // Act
            var response = await _client.GetAsync("/notes?startIndex=1&count=5");
            var content = await response.Content.ReadAsStringAsync();
            
            // Assert
            _output.WriteLine($"Status: {response.StatusCode}");
            _output.WriteLine($"Content: {content}");
            Assert.True(response.IsSuccessStatusCode, $"GET /notes falhou: {content}");
        }

        [Fact]
        public async Task Test02_GET_Pads_Should_Work()
        {
            // Arrange
            _output.WriteLine("🧪 TESTE 2: GET /pads deve funcionar");
            
            // Act
            var response = await _client.GetAsync("/pads?startIndex=1&count=5");
            var content = await response.Content.ReadAsStringAsync();
            
            // Assert
            _output.WriteLine($"Status: {response.StatusCode}");
            _output.WriteLine($"Content: {content}");
            Assert.True(response.IsSuccessStatusCode, $"GET /pads falhou: {content}");
        }

        [Fact]
        public async Task Test03_POST_Notes_Minimal_JSON()
        {
            // Arrange
            _output.WriteLine("🧪 TESTE 3: POST /notes com JSON mínimo");
            var minimalJson = JsonSerializer.Serialize(new { name = "Test Note" });
            var content = new StringContent(minimalJson, Encoding.UTF8, "application/scim+json");
            
            // Act
            var response = await _client.PostAsync("/notes", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            // Assert
            _output.WriteLine($"Status: {response.StatusCode}");
            _output.WriteLine($"Request JSON: {minimalJson}");
            _output.WriteLine($"Response: {responseContent}");
            
            if (!response.IsSuccessStatusCode)
            {
                _output.WriteLine("❌ FALHA: POST /notes com JSON mínimo");
                _output.WriteLine($"Status Code: {response.StatusCode}");
            }
            else
            {
                _output.WriteLine("✅ SUCESSO: POST /notes com JSON mínimo funcionou!");
            }
        }

        [Fact]
        public async Task Test04_POST_Notes_Complete_JSON()
        {
            // Arrange
            _output.WriteLine("🧪 TESTE 4: POST /notes com JSON completo");
            var completeNote = new
            {
                schemas = new[] { "urn:looplex:params:scim:schemas:notejam:2.0:Note" },
                name = "Complete Test Note",
                text = "Test content",
                active = true,
                status = 1,
                customFields = "{}"
            };
            var completeJson = JsonSerializer.Serialize(completeNote);
            var content = new StringContent(completeJson, Encoding.UTF8, "application/scim+json");
            
            // Act
            var response = await _client.PostAsync("/notes", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            // Assert
            _output.WriteLine($"Status: {response.StatusCode}");
            _output.WriteLine($"Request JSON: {completeJson}");
            _output.WriteLine($"Response: {responseContent}");
            
            if (!response.IsSuccessStatusCode)
            {
                _output.WriteLine("❌ FALHA: POST /notes com JSON completo");
            }
            else
            {
                _output.WriteLine("✅ SUCESSO: POST /notes com JSON completo funcionou!");
            }
        }

        [Fact]
        public async Task Test05_POST_Pads_Minimal_JSON()
        {
            // Arrange
            _output.WriteLine("🧪 TESTE 5: POST /pads com JSON mínimo");
            var minimalJson = JsonSerializer.Serialize(new { name = "Test Pad" });
            var content = new StringContent(minimalJson, Encoding.UTF8, "application/scim+json");
            
            // Act
            var response = await _client.PostAsync("/pads", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            // Assert
            _output.WriteLine($"Status: {response.StatusCode}");
            _output.WriteLine($"Request JSON: {minimalJson}");
            _output.WriteLine($"Response: {responseContent}");
            
            if (!response.IsSuccessStatusCode)
            {
                _output.WriteLine("❌ FALHA: POST /pads com JSON mínimo");
            }
            else
            {
                _output.WriteLine("✅ SUCESSO: POST /pads com JSON mínimo funcionou!");
            }
        }

        [Fact]
        public async Task Test06_Direct_Note_Serialization()
        {
            // Arrange
            _output.WriteLine("🧪 TESTE 6: Serialização direta da entidade Note");
            
            try
            {
                // Act - Criar Note diretamente
                var note = new Note
                {
                    Name = "Direct Test",
                    Text = "Direct content",
                    Active = true,
                    Status = 1
                };
                
                // Serializar
                var serialized = JsonSerializer.Serialize(note);
                _output.WriteLine($"✅ SUCESSO: Note serializada: {serialized}");
                
                // Deserializar
                var deserialized = JsonSerializer.Deserialize<Note>(serialized);
                _output.WriteLine($"✅ SUCESSO: Note deserializada: {deserialized?.Name}");
                
            }
            catch (Exception ex)
            {
                _output.WriteLine($"❌ FALHA: Erro na serialização/deserialização: {ex.Message}");
                _output.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        [Fact]
        public async Task Test07_Service_Container_Analysis()
        {
            // Arrange
            _output.WriteLine("🧪 TESTE 7: Análise do container de serviços");
            
            using var scope = _factory.Services.CreateScope();
            var services = scope.ServiceProvider;
            
            try
            {
                // Verificar se os serviços estão registrados
                var noteService = services.GetService<Looplex.Foundation.Core.SCIMv2.Modules.IResourceService<Note>>();
                var padService = services.GetService<Looplex.Foundation.Core.SCIMv2.Modules.IResourceService<Pad>>();
                
                _output.WriteLine($"Note Service: {(noteService != null ? "✅ Registrado" : "❌ Não registrado")}");
                _output.WriteLine($"Pad Service: {(padService != null ? "✅ Registrado" : "❌ Não registrado")}");
                
                if (noteService != null)
                {
                    _output.WriteLine($"Note Service Type: {noteService.GetType().Name}");
                }
                
            }
            catch (Exception ex)
            {
                _output.WriteLine($"❌ ERRO no container: {ex.Message}");
            }
        }
    }
}










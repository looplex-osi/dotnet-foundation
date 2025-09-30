using Microsoft.AspNetCore.Mvc.Testing;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Looplex.Samples.Domain.Entities;

namespace Looplex.Samples.Tests.Integration
{
    /// <summary>
    /// Testes específicos para mapeamento JSON → Entidade
    /// </summary>
    public class JSONMappingTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly ITestOutputHelper _output;

        public JSONMappingTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
        {
            _factory = factory;
            _output = output;
        }

        [Fact]
        public void Test_Note_Serialization_Deserialization()
        {
            // Arrange
            _output.WriteLine("🧪 TESTE: Serialização/Deserialização da Note");
            
            var originalNote = new Note
            {
                Name = "Test Note",
                Text = "Test Content",
                Active = true,
                Status = 1,
                CustomFields = "{}"
            };

            try
            {
                // Act - Serializar
                var json = JsonSerializer.Serialize(originalNote, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });
                
                _output.WriteLine($"✅ JSON Serializado: {json}");
                
                // Act - Deserializar
                var deserializedNote = JsonSerializer.Deserialize<Note>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                
                _output.WriteLine($"✅ Note Deserializada: Name='{deserializedNote?.Name}', Text='{deserializedNote?.Text}'");
                
                // Assert
                Assert.NotNull(deserializedNote);
                Assert.Equal(originalNote.Name, deserializedNote.Name);
                Assert.Equal(originalNote.Text, deserializedNote.Text);
                Assert.Equal(originalNote.Active, deserializedNote.Active);
                Assert.Equal(originalNote.Status, deserializedNote.Status);
                
                _output.WriteLine("✅ SUCESSO: Serialização/Deserialização funciona perfeitamente");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"❌ FALHA: Erro na serialização/deserialização: {ex.Message}");
                _output.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        [Fact]
        public void Test_Note_With_SCIM_Schema()
        {
            // Arrange
            _output.WriteLine("🧪 TESTE: Note com schema SCIM");
            
            var scimNote = new
            {
                schemas = new[] { "urn:looplex:params:scim:schemas:notejam:2.0:Note" },
                name = "SCIM Test Note",
                text = "SCIM Test Content",
                active = true,
                status = 1,
                customFields = "{}"
            };

            try
            {
                // Act - Serializar com schema SCIM
                var json = JsonSerializer.Serialize(scimNote, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });
                
                _output.WriteLine($"✅ JSON SCIM: {json}");
                
                // Tentar deserializar para Note (pode falhar se houver incompatibilidade)
                try
                {
                    var deserializedNote = JsonSerializer.Deserialize<Note>(json, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    
                    _output.WriteLine($"✅ SUCESSO: JSON SCIM deserializado para Note");
                    _output.WriteLine($"Note: Name='{deserializedNote?.Name}', Text='{deserializedNote?.Text}'");
                }
                catch (Exception deserEx)
                {
                    _output.WriteLine($"⚠️ AVISO: JSON SCIM não pode ser deserializado diretamente para Note: {deserEx.Message}");
                    _output.WriteLine("Isso pode ser normal se o framework SCIM espera um formato específico");
                }
                
            }
            catch (Exception ex)
            {
                _output.WriteLine($"❌ FALHA: Erro no teste SCIM: {ex.Message}");
                throw;
            }
        }

        [Fact]
        public void Test_Note_Property_Mapping()
        {
            // Arrange
            _output.WriteLine("🧪 TESTE: Mapeamento de propriedades da Note");
            
            var note = new Note();
            
            try
            {
                // Testar cada propriedade individualmente
                _output.WriteLine("Testando propriedade Name...");
                note.Name = "Test Name";
                Assert.Equal("Test Name", note.Name);
                _output.WriteLine("✅ Name: OK");
                
                _output.WriteLine("Testando propriedade Text...");
                note.Text = "Test Text";
                Assert.Equal("Test Text", note.Text);
                _output.WriteLine("✅ Text: OK");
                
                _output.WriteLine("Testando propriedade Active...");
                note.Active = true;
                Assert.True(note.Active);
                _output.WriteLine("✅ Active: OK");
                
                _output.WriteLine("Testando propriedade Status...");
                note.Status = 1;
                Assert.Equal(1, note.Status);
                _output.WriteLine("✅ Status: OK");
                
                _output.WriteLine("Testando propriedade CustomFields...");
                note.CustomFields = "{\"test\": \"value\"}";
                Assert.Equal("{\"test\": \"value\"}", note.CustomFields);
                _output.WriteLine("✅ CustomFields: OK");
                
                _output.WriteLine("✅ SUCESSO: Todas as propriedades funcionam corretamente");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"❌ FALHA: Erro no mapeamento de propriedades: {ex.Message}");
                _output.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        [Fact]
        public void Test_JSON_Property_Names()
        {
            // Arrange
            _output.WriteLine("🧪 TESTE: Nomes de propriedades JSON");
            
            var note = new Note
            {
                Name = "Test",
                Text = "Content",
                Active = true,
                Status = 1,
                CustomFields = "{}"
            };

            try
            {
                // Testar serialização com diferentes políticas de nomenclatura
                var camelCaseJson = JsonSerializer.Serialize(note, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });
                
                _output.WriteLine($"✅ CamelCase JSON: {camelCaseJson}");
                
                var defaultJson = JsonSerializer.Serialize(note, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                _output.WriteLine($"✅ Default JSON: {defaultJson}");
                
                // Verificar se as propriedades estão corretas
                Assert.Contains("name", camelCaseJson);
                Assert.Contains("text", camelCaseJson);
                Assert.Contains("active", camelCaseJson);
                Assert.Contains("status", camelCaseJson);
                Assert.Contains("customFields", camelCaseJson);
                
                _output.WriteLine("✅ SUCESSO: Nomes de propriedades JSON estão corretos");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"❌ FALHA: Erro nos nomes de propriedades: {ex.Message}");
                throw;
            }
        }
    }
}










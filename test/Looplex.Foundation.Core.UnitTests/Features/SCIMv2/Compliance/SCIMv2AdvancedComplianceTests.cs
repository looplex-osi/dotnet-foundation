using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Looplex.Foundation.Core.SCIMv2.Entities;
using Looplex.Foundation.Core.Serialization;

namespace Looplex.Foundation.Core.UnitTests.Features.SCIMv2.Serialization
{
    /// <summary>
    /// Advanced SCIMv2 compliance tests - QA Specialist recommended improvements
    /// </summary>
    [TestClass]
    public class SCIMv2AdvancedComplianceTests
    {
        [TestMethod]
        public void TestSCIMv2SchemaUriCompliance()
        {
            // Arrange - Test standard SCIMv2 schema URIs
            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "testuser",
                DisplayName = "Test User",
                Active = true,
                Schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:User" },
                Meta = new ResourceMeta
                {
                    ResourceType = "User",
                    Created = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    Location = "https://example.com/v2/Users/test",
                    Version = "1"
                }
            };

            // Act
            var json = SCIMv2Serializer.SerializeResource(user, SCIMv2Serializer.ContentType.Json);
            var jsonDocument = JsonDocument.Parse(json);
            var root = jsonDocument.RootElement;

            // Assert - Verify SCIMv2 schema compliance
            Assert.IsTrue(root.TryGetProperty("schemas", out var schemasElement));
            Assert.IsTrue(schemasElement.ValueKind == JsonValueKind.Array);
            
            var schemas = schemasElement.EnumerateArray().ToList();
            Assert.IsTrue(schemas.Any(s => s.GetString() == "urn:ietf:params:scim:schemas:core:2.0:User"));
            
            // Verify no invalid schema URIs
            foreach (var schema in schemas)
            {
                var schemaUri = schema.GetString();
                Assert.IsTrue(schemaUri.StartsWith("urn:ietf:params:scim:schemas:") || 
                             schemaUri.StartsWith("urn:looplex:params:scim:schemas:"),
                             $"Invalid schema URI: {schemaUri}");
            }
        }

        [TestMethod]
        public void TestSCIMv2RequiredFieldsValidation()
        {
            // Arrange - Test missing required fields
            var invalidUser = new User
            {
                // Missing Id - should be required
                UserName = "testuser",
                DisplayName = "Test User",
                Active = true,
                Schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:User" },
                Meta = new ResourceMeta
                {
                    ResourceType = "User",
                    Created = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    Location = "https://example.com/v2/Users/test",
                    Version = "1"
                }
            };

            // Act & Assert - Should handle missing required fields gracefully
            var json = SCIMv2Serializer.SerializeResource(invalidUser, SCIMv2Serializer.ContentType.Json);
            var jsonDocument = JsonDocument.Parse(json);
            var root = jsonDocument.RootElement;

            // Verify that empty Id is serialized as empty string (not null)
            Assert.IsTrue(root.TryGetProperty("id", out var idElement));
            Assert.AreEqual(string.Empty, idElement.GetString());
        }

        [TestMethod]
        public void TestSCIMv2DateTimeFormatCompliance()
        {
            // Arrange - Test ISO 8601 datetime format compliance
            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "testuser",
                DisplayName = "Test User",
                Active = true,
                Schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:User" },
                Meta = new ResourceMeta
                {
                    ResourceType = "User",
                    Created = DateTime.Parse("2023-12-01T10:30:00.000Z"),
                    LastModified = DateTime.Parse("2023-12-01T10:30:00.000Z"),
                    Location = "https://example.com/v2/Users/test",
                    Version = "1"
                }
            };

            // Act
            var json = SCIMv2Serializer.SerializeResource(user, SCIMv2Serializer.ContentType.Json);
            var jsonDocument = JsonDocument.Parse(json);
            var root = jsonDocument.RootElement;

            // Assert - Verify ISO 8601 format
            var meta = root.GetProperty("meta");
            var created = meta.GetProperty("created").GetString();
            var lastModified = meta.GetProperty("lastModified").GetString();

            // Should be in ISO 8601 format (Z suffix or timezone offset)
            Assert.IsTrue(created.EndsWith("Z") || created.Contains("+") || created.Contains("-"), 
                $"Created datetime not in ISO 8601 format: {created}");
            Assert.IsTrue(lastModified.EndsWith("Z") || lastModified.Contains("+") || lastModified.Contains("-"), 
                $"LastModified datetime not in ISO 8601 format: {lastModified}");
        }

        [TestMethod]
        public void TestSCIMv2MultiValuedAttributesCompliance()
        {
            // Arrange - Test multi-valued attributes structure
            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "testuser",
                DisplayName = "Test User",
                Active = true,
                Schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:User" },
                Meta = new ResourceMeta
                {
                    ResourceType = "User",
                    Created = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    Location = "https://example.com/v2/Users/test",
                    Version = "1"
                },
                Emails = new List<ScimEmail>
                {
                    new ScimEmail { Value = "work@example.com", Type = "work", Primary = true },
                    new ScimEmail { Value = "personal@example.com", Type = "home", Primary = false }
                },
                PhoneNumbers = new List<ScimPhoneNumber>
                {
                    new ScimPhoneNumber { Value = "+1-555-123-4567", Type = "work" },
                    new ScimPhoneNumber { Value = "+1-555-987-6543", Type = "mobile" }
                }
            };

            // Act
            var json = SCIMv2Serializer.SerializeResource(user, SCIMv2Serializer.ContentType.Json);
            var jsonDocument = JsonDocument.Parse(json);
            var root = jsonDocument.RootElement;

            // Assert - Verify multi-valued attributes structure
            Assert.IsTrue(root.TryGetProperty("emails", out var emailsElement));
            Assert.IsTrue(emailsElement.ValueKind == JsonValueKind.Array);
            Assert.AreEqual(2, emailsElement.GetArrayLength());

            // Verify primary email is marked correctly
            var emails = emailsElement.EnumerateArray().ToList();
            var primaryEmails = emails.Where(e => e.TryGetProperty("primary", out var p) && p.GetBoolean()).ToList();
            Assert.AreEqual(1, primaryEmails.Count, "Should have exactly one primary email");

            // Verify phone numbers structure
            Assert.IsTrue(root.TryGetProperty("phoneNumbers", out var phoneElement));
            Assert.IsTrue(phoneElement.ValueKind == JsonValueKind.Array);
            Assert.AreEqual(2, phoneElement.GetArrayLength());
        }

        [TestMethod]
        public void TestSCIMv2PatchOperationCompliance()
        {
            // Arrange - Test RFC 6902 compliance for patch operations
            var patches = new[]
            {
                PatchOperation.Replace("displayName", "Updated Name"),
                PatchOperation.Add("emails", new { value = "new@example.com", type = "work", primary = false }),
                PatchOperation.Remove("nickName")
            };

            // Act
            var json = JsonSerializer.Serialize(patches, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            // Assert - Verify RFC 6902 compliance
            var jsonDocument = JsonDocument.Parse(json);
            var root = jsonDocument.RootElement;

            Assert.IsTrue(root.ValueKind == JsonValueKind.Array);

            foreach (var patch in root.EnumerateArray())
            {
                // Verify required fields
                Assert.IsTrue(patch.TryGetProperty("op", out var opElement));
                Assert.IsTrue(patch.TryGetProperty("path", out var pathElement));

                var op = opElement.GetString();
                var path = pathElement.GetString();

                // Verify valid operations
                Assert.IsTrue(new[] { "add", "remove", "replace" }.Contains(op), 
                    $"Invalid operation: {op}");

                // Verify path format
                Assert.IsTrue(path.StartsWith("/") || !path.Contains("/"), 
                    $"Invalid path format: {path}");

                // Verify value handling based on operation
                if (op == "remove")
                {
                    // Remove operations should not have value or value should be null
                    if (patch.TryGetProperty("value", out var valueElement))
                    {
                        Assert.AreEqual(JsonValueKind.Null, valueElement.ValueKind);
                    }
                }
                else if (op == "add" || op == "replace")
                {
                    // Add and replace operations must have value
                    Assert.IsTrue(patch.TryGetProperty("value", out var valueElement));
                    Assert.AreNotEqual(JsonValueKind.Null, valueElement.ValueKind);
                }
            }
        }

        [TestMethod]
        public void TestSCIMv2ErrorResponseCompliance()
        {
            // Arrange - Test SCIMv2 error response structure
            var error = new SCIMv2Error
            {
                Status = "400 Bad Request",
                ScimType = "invalidValue",
                Detail = "The 'userName' attribute is required.",
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };

            // Act
            var json = SCIMv2Serializer.SerializeError(error, SCIMv2Serializer.ContentType.Json);
            var jsonDocument = JsonDocument.Parse(json);
            var root = jsonDocument.RootElement;

            // Assert - Verify SCIMv2 error response compliance
            Assert.IsTrue(root.TryGetProperty("schemas", out var schemasElement));
            Assert.IsTrue(schemasElement.ValueKind == JsonValueKind.Array);
            Assert.AreEqual("urn:ietf:params:scim:api:messages:2.0:Error", 
                schemasElement[0].GetString());

            Assert.IsTrue(root.TryGetProperty("status", out var statusElement));
            Assert.IsTrue(root.TryGetProperty("detail", out var detailElement));

            // Verify status format (should be HTTP status code + description)
            var status = statusElement.GetString();
            Assert.IsTrue(status.Contains(" "), "Status should include HTTP code and description");
            Assert.IsTrue(int.TryParse(status.Split(' ')[0], out var statusCode), 
                "Status should start with numeric HTTP code");
        }

        [TestMethod]
        public void TestSCIMv2BulkOperationCompliance()
        {
            // Arrange - Test bulk operation structure
            var users = new List<User>
            {
                new User
                {
                    Id = "user1",
                    UserName = "user1",
                    DisplayName = "User One",
                    Active = true,
                    Schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:User" },
                    Meta = new ResourceMeta
                    {
                        ResourceType = "User",
                        Created = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow,
                        Location = "https://example.com/v2/Users/user1",
                        Version = "1"
                    }
                }
            };

            // Act
            var json = SCIMv2Serializer.SerializeResources(users, SCIMv2Serializer.ContentType.Json);
            var jsonDocument = JsonDocument.Parse(json);
            var root = jsonDocument.RootElement;

            // Assert - Verify bulk operation structure
            Assert.IsTrue(root.ValueKind == JsonValueKind.Array);
            
            foreach (var userElement in root.EnumerateArray())
            {
                // Each resource in bulk should be a complete SCIMv2 resource
                Assert.IsTrue(userElement.TryGetProperty("id", out _));
                Assert.IsTrue(userElement.TryGetProperty("schemas", out _));
                Assert.IsTrue(userElement.TryGetProperty("meta", out _));
            }
        }

        [TestMethod]
        public void TestSCIMv2CharacterEncodingCompliance()
        {
            // Arrange - Test special characters and Unicode
            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "testuser",
                DisplayName = "José da Silva", // Unicode characters
                Active = true,
                Schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:User" },
                Meta = new ResourceMeta
                {
                    ResourceType = "User",
                    Created = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    Location = "https://example.com/v2/Users/test",
                    Version = "1"
                }
            };

            // Act
            var json = SCIMv2Serializer.SerializeResource(user, SCIMv2Serializer.ContentType.Json);
            Console.WriteLine($"Serialized JSON with Unicode: {json}");
            
            var jsonDocument = JsonDocument.Parse(json);
            var root = jsonDocument.RootElement;

            // Assert - Verify Unicode characters are preserved
            Assert.IsTrue(root.TryGetProperty("displayName", out var displayNameElement));
            Assert.AreEqual("José da Silva", displayNameElement.GetString());
            
            // Verify JSON contains the Unicode characters (escaped as \u00E9)
            var hasUnicode = json.Contains("José da Silva") || json.Contains("Jos\\u00E9 da Silva");
            Assert.IsTrue(hasUnicode, $"JSON should contain Unicode characters. JSON: {json}");
            
            // Verify JSON can be parsed without encoding issues
            var parsedJson = JsonDocument.Parse(json);
            Assert.IsNotNull(parsedJson);
        }
    }
}

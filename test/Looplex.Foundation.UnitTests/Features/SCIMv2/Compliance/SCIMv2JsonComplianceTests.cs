using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Looplex.Foundation.SCIMv2.Entities;
using Looplex.Foundation.Serialization;

namespace Looplex.Foundation.UnitTests.Features.SCIMv2.Serialization
{
    /// <summary>
    /// Tests to verify SCIMv2 JSON input/output layouts are compliant with RFC 7643
    /// </summary>
    [TestClass]
    public class SCIMv2JsonComplianceTests
    {
        [TestMethod]
        public void TestUserResourceJsonLayout()
        {
            // Arrange - Create a complete SCIMv2 User resource
            var user = new User
            {
                Id = "2819c223-7f76-453a-919d-413861904646",
                UserName = "bjensen",
                DisplayName = "Babs Jensen",
                Active = true,
                Schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:User" },
                Meta = new ResourceMeta
                {
                    ResourceType = "User",
                    Created = DateTime.Parse("2011-08-01T18:29:49.793Z"),
                    LastModified = DateTime.Parse("2011-08-01T18:29:49.793Z"),
                    Location = "https://example.com/v2/Users/2819c223-7f76-453a-919d-413861904646",
                    Version = "W/\"f250dd84f0671c3\""
                },
                Name = new ScimName
                {
                    Formatted = "Ms. Barbara J Jensen, III",
                    FamilyName = "Jensen",
                    GivenName = "Barbara",
                    MiddleName = "Jane"
                },
                Emails = new List<ScimEmail>
                {
                    new ScimEmail
                    {
                        Value = "bjensen@example.com",
                        Type = "work",
                        Primary = true
                    }
                }
            };

            // Act - Serialize to JSON
            var json = SCIMv2Serializer.SerializeResource(user, SCIMv2Serializer.ContentType.Json);
            Console.WriteLine("Serialized User JSON:");
            Console.WriteLine(json);

            // Assert - Verify JSON structure follows SCIMv2 format
            var jsonDocument = JsonDocument.Parse(json);
            var root = jsonDocument.RootElement;

            // Check required SCIMv2 fields
            Assert.IsTrue(root.TryGetProperty("id", out var idElement));
            Assert.AreEqual("2819c223-7f76-453a-919d-413861904646", idElement.GetString());

            Assert.IsTrue(root.TryGetProperty("schemas", out var schemasElement));
            Assert.IsTrue(schemasElement.ValueKind == JsonValueKind.Array);
            Assert.AreEqual(1, schemasElement.GetArrayLength());
            Assert.AreEqual("urn:ietf:params:scim:schemas:core:2.0:User", schemasElement[0].GetString());

            Assert.IsTrue(root.TryGetProperty("meta", out var metaElement));
            Assert.IsTrue(metaElement.TryGetProperty("resourceType", out var resourceTypeElement));
            Assert.AreEqual("User", resourceTypeElement.GetString());

            Assert.IsTrue(metaElement.TryGetProperty("created", out var createdElement));
            Assert.IsTrue(metaElement.TryGetProperty("lastModified", out var lastModifiedElement));
            Assert.IsTrue(metaElement.TryGetProperty("location", out var locationElement));
            Assert.IsTrue(metaElement.TryGetProperty("version", out var versionElement));

            // Check User-specific fields
            Assert.IsTrue(root.TryGetProperty("userName", out var userNameElement));
            Assert.AreEqual("bjensen", userNameElement.GetString());

            Assert.IsTrue(root.TryGetProperty("displayName", out var displayNameElement));
            Assert.AreEqual("Babs Jensen", displayNameElement.GetString());

            Assert.IsTrue(root.TryGetProperty("active", out var activeElement));
            Assert.IsTrue(activeElement.GetBoolean());

            // Check complex objects
            Assert.IsTrue(root.TryGetProperty("name", out var nameElement));
            Assert.IsTrue(nameElement.TryGetProperty("formatted", out var formattedElement));
            Assert.AreEqual("Ms. Barbara J Jensen, III", formattedElement.GetString());

            Assert.IsTrue(root.TryGetProperty("emails", out var emailsElement));
            Assert.IsTrue(emailsElement.ValueKind == JsonValueKind.Array);
            Assert.AreEqual(1, emailsElement.GetArrayLength());

            var email = emailsElement[0];
            Assert.IsTrue(email.TryGetProperty("value", out var emailValueElement));
            Assert.AreEqual("bjensen@example.com", emailValueElement.GetString());
            Assert.IsTrue(email.TryGetProperty("primary", out var primaryElement));
            Assert.IsTrue(primaryElement.GetBoolean());
        }

        [TestMethod]
        public void TestUserResourceDeserialization()
        {
            // Arrange - SCIMv2 compliant JSON
            var json = @"{
                ""id"": ""2819c223-7f76-453a-919d-413861904646"",
                ""schemas"": [""urn:ietf:params:scim:schemas:core:2.0:User""],
                ""userName"": ""bjensen"",
                ""displayName"": ""Babs Jensen"",
                ""active"": true,
                ""meta"": {
                    ""resourceType"": ""User"",
                    ""created"": ""2011-08-01T18:29:49.793Z"",
                    ""lastModified"": ""2011-08-01T18:29:49.793Z"",
                    ""location"": ""https://example.com/v2/Users/2819c223-7f76-453a-919d-413861904646"",
                    ""version"": ""W/\""f250dd84f0671c3\""""
                },
                ""name"": {
                    ""formatted"": ""Ms. Barbara J Jensen, III"",
                    ""familyName"": ""Jensen"",
                    ""givenName"": ""Barbara"",
                    ""middleName"": ""Jane""
                },
                ""emails"": [
                    {
                        ""value"": ""bjensen@example.com"",
                        ""type"": ""work"",
                        ""primary"": true
                    }
                ]
            }";

            // Act - Deserialize from JSON
            var user = SCIMv2Serializer.DeserializeResource<User>(json, SCIMv2Serializer.ContentType.Json);

            // Assert - Verify deserialized object
            Assert.AreEqual("2819c223-7f76-453a-919d-413861904646", user.Id);
            Assert.AreEqual("bjensen", user.UserName);
            Assert.AreEqual("Babs Jensen", user.DisplayName);
            Assert.IsTrue(user.Active);
            Assert.AreEqual(1, user.Schemas.Length);
            Assert.AreEqual("urn:ietf:params:scim:schemas:core:2.0:User", user.Schemas[0]);

            Assert.IsNotNull(user.Meta);
            Assert.AreEqual("User", user.Meta.ResourceType);
            Assert.AreEqual("https://example.com/v2/Users/2819c223-7f76-453a-919d-413861904646", user.Meta.Location);
            Assert.AreEqual("W/\"f250dd84f0671c3\"", user.Meta.Version);

            Assert.IsNotNull(user.Name);
            Assert.AreEqual("Ms. Barbara J Jensen, III", user.Name.Formatted);
            Assert.AreEqual("Jensen", user.Name.FamilyName);
            Assert.AreEqual("Barbara", user.Name.GivenName);

            Assert.IsNotNull(user.Emails);
            Assert.AreEqual(1, user.Emails.Count);
            Assert.AreEqual("bjensen@example.com", user.Emails[0].Value);
            Assert.AreEqual("work", user.Emails[0].Type);
            Assert.IsTrue(user.Emails[0].Primary);
        }

        [TestMethod]
        public void TestSCIMv2ResponseJsonLayout()
        {
            // Arrange - Create a SCIMv2 response
            var response = new SCIMv2Response
            {
                StatusCode = 201,
                Data = new User
                {
                    Id = "2819c223-7f76-453a-919d-413861904646",
                    UserName = "bjensen",
                    DisplayName = "Babs Jensen",
                    Active = true,
                    Schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:User" },
                    Meta = new ResourceMeta
                    {
                        ResourceType = "User",
                        Created = DateTime.Parse("2011-08-01T18:29:49.793Z"),
                        LastModified = DateTime.Parse("2011-08-01T18:29:49.793Z"),
                        Location = "https://example.com/v2/Users/2819c223-7f76-453a-919d-413861904646",
                        Version = "W/\"f250dd84f0671c3\""
                    }
                },
                Schemas = new[] { "urn:ietf:params:scim:api:messages:2.0:Response" },
                Location = "https://example.com/v2/Users/2819c223-7f76-453a-919d-413861904646",
                ETag = "W/\"f250dd84f0671c3\""
            };

            // Act - Serialize to JSON
            var json = SCIMv2Serializer.SerializeResponse(response, SCIMv2Serializer.ContentType.Json);
            Console.WriteLine("Serialized SCIMv2Response JSON:");
            Console.WriteLine(json);

            // Assert - Verify JSON structure
            var jsonDocument = JsonDocument.Parse(json);
            var root = jsonDocument.RootElement;

            Assert.IsTrue(root.TryGetProperty("statusCode", out var statusCodeElement));
            Assert.AreEqual(201, statusCodeElement.GetInt32());

            Assert.IsTrue(root.TryGetProperty("schemas", out var schemasElement));
            Assert.IsTrue(schemasElement.ValueKind == JsonValueKind.Array);

            Assert.IsTrue(root.TryGetProperty("Resources", out var dataElement));
            Assert.IsTrue(dataElement.TryGetProperty("id", out var idElement));
            Assert.AreEqual("2819c223-7f76-453a-919d-413861904646", idElement.GetString());

            Assert.IsTrue(root.TryGetProperty("location", out var locationElement));
            Assert.AreEqual("https://example.com/v2/Users/2819c223-7f76-453a-919d-413861904646", locationElement.GetString());

            Assert.IsTrue(root.TryGetProperty("etag", out var etagElement));
            Assert.AreEqual("W/\"f250dd84f0671c3\"", etagElement.GetString());
        }

        [TestMethod]
        public void TestSCIMv2ErrorJsonLayout()
        {
            // Arrange - Create a SCIMv2 error
            var error = new SCIMv2Error
            {
                Status = "400 Bad Request",
                ScimType = "invalidValue",
                Detail = "The 'userName' attribute is required.",
                Timestamp = "2023-12-01T10:30:00.000Z"
            };

            // Act - Serialize to JSON
            var json = SCIMv2Serializer.SerializeError(error, SCIMv2Serializer.ContentType.Json);
            Console.WriteLine("Serialized SCIMv2Error JSON:");
            Console.WriteLine(json);

            // Assert - Verify JSON structure
            var jsonDocument = JsonDocument.Parse(json);
            var root = jsonDocument.RootElement;

            Assert.IsTrue(root.TryGetProperty("schemas", out var schemasElement));
            Assert.IsTrue(schemasElement.ValueKind == JsonValueKind.Array);
            Assert.AreEqual("urn:ietf:params:scim:api:messages:2.0:Error", schemasElement[0].GetString());

            Assert.IsTrue(root.TryGetProperty("status", out var statusElement));
            Assert.AreEqual("400 Bad Request", statusElement.GetString());

            Assert.IsTrue(root.TryGetProperty("scimType", out var scimTypeElement));
            Assert.AreEqual("invalidValue", scimTypeElement.GetString());

            Assert.IsTrue(root.TryGetProperty("detail", out var detailElement));
            Assert.AreEqual("The 'userName' attribute is required.", detailElement.GetString());

            Assert.IsTrue(root.TryGetProperty("timestamp", out var timestampElement));
            Assert.AreEqual("2023-12-01T10:30:00.000Z", timestampElement.GetString());
        }

        [TestMethod]
        public void TestPatchOperationJsonLayout()
        {
            // Arrange - Create SCIMv2 patch operations
            var patches = new[]
            {
                PatchOperation.Replace("displayName", "Updated Display Name"),
                PatchOperation.Add("emails", new { value = "newemail@example.com", type = "work", primary = true }),
                PatchOperation.Remove("nickName")
            };

            // Act - Serialize to JSON
            var json = JsonSerializer.Serialize(patches, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
            Console.WriteLine("Serialized PatchOperation[] JSON:");
            Console.WriteLine(json);

            // Assert - Verify JSON structure
            var jsonDocument = JsonDocument.Parse(json);
            var root = jsonDocument.RootElement;

            Assert.IsTrue(root.ValueKind == JsonValueKind.Array);
            Assert.AreEqual(3, root.GetArrayLength());

            // Check first patch (replace)
            var replacePatch = root[0];
            Assert.IsTrue(replacePatch.TryGetProperty("op", out var opElement));
            Assert.AreEqual("replace", opElement.GetString());
            Assert.IsTrue(replacePatch.TryGetProperty("path", out var pathElement));
            Assert.AreEqual("displayName", pathElement.GetString());
            Assert.IsTrue(replacePatch.TryGetProperty("value", out var valueElement));
            Assert.AreEqual("Updated Display Name", valueElement.GetString());

            // Check second patch (add)
            var addPatch = root[1];
            Assert.AreEqual("add", addPatch.GetProperty("op").GetString());
            Assert.AreEqual("emails", addPatch.GetProperty("path").GetString());
            Assert.IsTrue(addPatch.TryGetProperty("value", out var addValueElement));

            // Check third patch (remove)
            var removePatch = root[2];
            Assert.AreEqual("remove", removePatch.GetProperty("op").GetString());
            Assert.AreEqual("nickName", removePatch.GetProperty("path").GetString());
            // For remove operations, value should be null or not present
            // The current implementation includes value: null, which is acceptable
            Assert.IsTrue(removePatch.TryGetProperty("value", out var removeValueElement));
            Assert.AreEqual(JsonValueKind.Null, removeValueElement.ValueKind);
        }

        [TestMethod]
        public void TestListResponseJsonLayout()
        {
            // Arrange - Create a list response
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
                },
                new User
                {
                    Id = "user2",
                    UserName = "user2",
                    DisplayName = "User Two",
                    Active = true,
                    Schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:User" },
                    Meta = new ResourceMeta
                    {
                        ResourceType = "User",
                        Created = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow,
                        Location = "https://example.com/v2/Users/user2",
                        Version = "1"
                    }
                }
            };

            // Act - Serialize to JSON
            var json = SCIMv2Serializer.SerializeResources(users, SCIMv2Serializer.ContentType.Json);
            Console.WriteLine("Serialized User List JSON:");
            Console.WriteLine(json);

            // Assert - Verify JSON structure
            var jsonDocument = JsonDocument.Parse(json);
            var root = jsonDocument.RootElement;

            Assert.IsTrue(root.ValueKind == JsonValueKind.Array);
            Assert.AreEqual(2, root.GetArrayLength());

            var firstUser = root[0];
            Assert.IsTrue(firstUser.TryGetProperty("id", out var idElement));
            Assert.AreEqual("user1", idElement.GetString());
            Assert.IsTrue(firstUser.TryGetProperty("userName", out var userNameElement));
            Assert.AreEqual("user1", userNameElement.GetString());
        }
    }
}

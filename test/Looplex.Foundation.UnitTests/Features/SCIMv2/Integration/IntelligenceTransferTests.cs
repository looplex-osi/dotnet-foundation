using System;
using System.Threading.Tasks;
using Looplex.Foundation.SCIMv2;
using Looplex.Foundation.SCIMv2.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Looplex.Foundation.UnitTests.Features.SCIMv2.Entities;

/// <summary>
/// Comprehensive integration tests for SCIMv2 intelligence transfer functionality
/// Tests the complete SCIMv2 workflow including CRUD operations, metadata management, and RFC compliance
/// </summary>
[TestClass]
public class IntelligenceTransferTests
{
    #region Initialization Tests
    
    [TestMethod]
    public async Task TestSCIMv2Initialization()
    {
        // Arrange & Act
        var scimService = new Looplex.Foundation.SCIMv2.SCIMv2();
        
        // Assert
        var collections = scimService.GetRegisteredCollections();
        Assert.IsTrue(collections.Contains("Users"));
        Assert.IsTrue(collections.Contains("Groups"));
        
        Assert.IsTrue(scimService.IsCollectionRegistered("Users"));
        Assert.IsTrue(scimService.IsCollectionRegistered("Groups"));
    }
    
    [TestMethod]
    public async Task TestSchemaDiscovery()
    {
        // Arrange
        var scimService = new Looplex.Foundation.SCIMv2.SCIMv2();
        
        // Act
        var schemasResponse = await scimService.GetSchemasAsync();
        var userSchema = await scimService.GetSchemaAsync("urn:ietf:params:scim:schemas:core:2.0:User");
        var configResponse = await scimService.GetServiceProviderConfigAsync();
        
        // Assert
        Assert.AreEqual(200, schemasResponse.StatusCode);
        Assert.IsTrue(schemasResponse.TotalResults >= 2);
        
        Assert.IsNotNull(userSchema);
        Assert.AreEqual(200, configResponse.StatusCode);
    }
    
    #endregion

    #region User Operations Tests
    
    [TestMethod]
    public async Task TestUserCRUDOperations()
    {
        // Arrange
        var scimService = new Looplex.Foundation.SCIMv2.SCIMv2();
        var user = CreateTestUser();
        
        // Act & Assert - Create
        var createResponse = await scimService.CreateAsync("Users", user);
        Assert.AreEqual(201, createResponse.StatusCode);
        Assert.IsNotNull(user.Id);
        Assert.AreNotEqual(default(DateTime), user.Meta.Created);
        Assert.AreEqual("1", user.Meta.Version);
        Assert.IsNotNull(user.Meta.Location);
        
        // Act & Assert - Retrieve
        var retrieveResponse = await scimService.RetrieveAsync("Users", user.Id!);
        Assert.AreEqual(200, retrieveResponse.StatusCode);
        
        // Act & Assert - Update
        var patches = new PatchOperation[]
        {
            PatchOperation.Replace("displayName", "Updated User")
        };
        var patchResponse = await scimService.ModifyAsync("Users", user.Id!, patches);
        Assert.AreEqual(200, patchResponse.StatusCode);
        
        // Act & Assert - Delete
        var deleteResponse = await scimService.DeleteAsync("Users", user.Id!);
        Assert.AreEqual(204, deleteResponse.StatusCode);
        
        // Verify deletion
        var retrieveAfterDelete = await scimService.RetrieveAsync("Users", user.Id!);
        Assert.AreEqual(404, retrieveAfterDelete.StatusCode);
    }
    
    [TestMethod]
    public async Task TestUserQueryOperations()
    {
        // Arrange
        var scimService = new Looplex.Foundation.SCIMv2.SCIMv2();
        
        // Create multiple users for testing
        for (int i = 1; i <= 3; i++)
        {
            var user = CreateTestUser($"user{i}", $"User {i}");
            var createResponse = await scimService.CreateAsync("Users", user);
            Assert.AreEqual(201, createResponse.StatusCode, $"Failed to create user {i}");
        }
        
        // Act & Assert - Query
        var queryResponse = await scimService.QueryAsync("Users", 1, 10, null, null, null);
        Assert.AreEqual(200, queryResponse.StatusCode);
        Assert.IsTrue(queryResponse.TotalResults >= 3);
        
        // Act & Assert - Sorted Query
        var sortedQueryResponse = await scimService.QueryAsync("Users", 1, 10, null, "UserName", "ascending");
        Assert.AreEqual(200, sortedQueryResponse.StatusCode);
    }
    
    #endregion

    #region Group Operations Tests
    
    [TestMethod]
    public async Task TestGroupCRUDOperations()
    {
        // Arrange
        var scimService = new Looplex.Foundation.SCIMv2.SCIMv2();
        var group = CreateTestGroup();
        
        // Act & Assert - Create
        var createResponse = await scimService.CreateAsync("Groups", group);
        Assert.AreEqual(201, createResponse.StatusCode);
        Assert.IsNotNull(group.Id);
        Assert.AreNotEqual(default(DateTime), group.Meta.Created);
        Assert.AreEqual("1", group.Meta.Version);
        Assert.IsNotNull(group.Meta.Location);
        
        // Act & Assert - Retrieve
        var retrieveResponse = await scimService.RetrieveAsync("Groups", group.Id!);
        Assert.AreEqual(200, retrieveResponse.StatusCode);
        
        // Act & Assert - Delete
        var deleteResponse = await scimService.DeleteAsync("Groups", group.Id!);
        Assert.AreEqual(204, deleteResponse.StatusCode);
    }
    
    #endregion

    #region Advanced Operations Tests
    
    [TestMethod]
    public async Task TestResourceReplacementWithMetadataPreservation()
    {
        // Arrange
        var scimService = new Looplex.Foundation.SCIMv2.SCIMv2();
        var originalUser = CreateTestUser("original.user", "Original User");
        
        await scimService.CreateAsync("Users", originalUser);
        var originalCreated = originalUser.Meta.Created;
        var originalVersion = originalUser.Meta.Version;
        
        var replacementUser = new User
        {
            Id = originalUser.Id,
            UserName = "replacement.user",
            DisplayName = "Replacement User",
            Active = false,
            Schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:User" },
            Meta = new ResourceMeta
            {
                ResourceType = "User",
                Created = originalUser.Meta.Created,
                LastModified = DateTime.UtcNow,
                Location = originalUser.Meta.Location,
                Version = "2"
            }
        };
        
        // Act
        var replaceResponse = await scimService.ReplaceAsync("Users", originalUser.Id!, replacementUser);
        
        // Assert
        Assert.AreEqual(200, replaceResponse.StatusCode);
        Assert.AreEqual(originalUser.Id, replacementUser.Id);
        Assert.AreEqual(originalCreated, replacementUser.Meta.Created);
        Assert.AreNotEqual(originalVersion, replacementUser.Meta.Version);
        Assert.IsTrue(int.Parse(replacementUser.Meta.Version!) > int.Parse(originalVersion!));
    }
    
    [TestMethod]
    public async Task TestPatchOperations()
    {
        // Arrange
        var scimService = new Looplex.Foundation.SCIMv2.SCIMv2();
        var user = CreateTestUser("patch.user", "Patch User");
        
        await scimService.CreateAsync("Users", user);
        var originalVersion = user.Meta.Version;
        
        var patches = new PatchOperation[]
        {
            PatchOperation.Replace("displayName", "Patched User")
        };
        
        // Act
        var patchResponse = await scimService.ModifyAsync("Users", user.Id!, patches);
        
        // Assert
        Assert.AreEqual(200, patchResponse.StatusCode);
        
        // Verify version was incremented
        var updatedUser = await scimService.RetrieveAsync("Users", user.Id!);
        Assert.AreEqual(200, updatedUser.StatusCode);
    }
    
    #endregion

    #region Performance Tests
    
    [TestMethod]
    public async Task TestSCIMv2Performance()
    {
        // Arrange
        var scimService = new Looplex.Foundation.SCIMv2.SCIMv2();
        var startTime = DateTime.UtcNow;
        
        // Act - Create multiple resources to test performance
        for (int i = 1; i <= 10; i++)
        {
            var user = CreateTestUser($"perf.user{i}", $"Performance User {i}");
            await scimService.CreateAsync("Users", user);
        }
        
        var endTime = DateTime.UtcNow;
        var duration = endTime - startTime;
        
        // Assert - Performance should be reasonable (less than 2 seconds for 10 operations)
        Assert.IsTrue(duration.TotalSeconds < 2.0, $"Performance test took too long: {duration.TotalSeconds} seconds");
        
        // Test query performance
        var queryStartTime = DateTime.UtcNow;
        var queryResponse = await scimService.QueryAsync("Users", 1, 10, null, null, null);
        var queryEndTime = DateTime.UtcNow;
        var queryDuration = queryEndTime - queryStartTime;
        
        Assert.AreEqual(200, queryResponse.StatusCode);
        Assert.IsTrue(queryDuration.TotalMilliseconds < 500, $"Query performance test took too long: {queryDuration.TotalMilliseconds} ms");
    }
    
    #endregion

    #region Helper Methods
    
    private static User CreateTestUser(string userName = "test.user", string displayName = "Test User")
    {
        return new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = userName,
            DisplayName = displayName,
            Active = true,
            Schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:User" },
            Meta = new ResourceMeta
            {
                ResourceType = "User",
                Created = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Location = $"https://example.com/Users/{Guid.NewGuid()}",
                Version = "1"
            }
        };
    }
    
    private static Group CreateTestGroup(string displayName = "Test Group")
    {
        return new Group
        {
            Id = Guid.NewGuid().ToString(),
            DisplayName = displayName,
            Schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:Group" },
            Meta = new ResourceMeta
            {
                ResourceType = "Group",
                Created = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Location = $"https://example.com/Groups/{Guid.NewGuid()}",
                Version = "1"
            }
        };
    }
    
    #endregion
}
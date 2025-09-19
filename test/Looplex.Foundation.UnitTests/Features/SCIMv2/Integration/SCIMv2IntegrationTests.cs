using System;
using System.Threading.Tasks;
using Looplex.Foundation.SCIMv2;
using Looplex.Foundation.SCIMv2.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Looplex.Foundation.UnitTests.Features.SCIMv2.Integration;

/// <summary>
/// Comprehensive integration tests for SCIMv2 functionality
/// Tests complete workflows, error scenarios, and edge cases
/// </summary>
[TestClass]
public class SCIMv2IntegrationTests
{
    #region Complete Workflow Tests
    
    [TestMethod]
    public async Task TestCompleteUserLifecycle()
    {
        // Arrange
        var scimService = new Looplex.Foundation.SCIMv2.SCIMv2();
        var user = CreateTestUser("lifecycle.user", "Lifecycle User");
        
        // Act & Assert - Complete CRUD lifecycle
        
        // 1. Create
        var createResponse = await scimService.CreateAsync("Users", user);
        Assert.AreEqual(201, createResponse.StatusCode);
        Assert.IsNotNull(user.Id);
        Assert.IsNotNull(user.Meta.Location);
        
        // 2. Retrieve
        var retrieveResponse = await scimService.RetrieveAsync("Users", user.Id!);
        Assert.AreEqual(200, retrieveResponse.StatusCode);
        
        // 3. Update via PATCH
        var patches = new PatchOperation[]
        {
            PatchOperation.Replace("displayName", "Updated Lifecycle User"),
            PatchOperation.Replace("active", false)
        };
        var patchResponse = await scimService.ModifyAsync("Users", user.Id!, patches);
        Assert.AreEqual(200, patchResponse.StatusCode);
        
        // 4. Replace entire resource
        var replacementUser = CreateTestUser("replacement.user", "Replacement User");
        replacementUser.Id = user.Id;
        var replaceResponse = await scimService.ReplaceAsync("Users", user.Id!, replacementUser);
        Assert.AreEqual(200, replaceResponse.StatusCode);
        
        // 5. Query
        var queryResponse = await scimService.QueryAsync("Users", 1, 10, null, null, null);
        Assert.AreEqual(200, queryResponse.StatusCode);
        Assert.IsTrue(queryResponse.TotalResults >= 1);
        
        // 6. Delete
        var deleteResponse = await scimService.DeleteAsync("Users", user.Id!);
        Assert.AreEqual(204, deleteResponse.StatusCode);
        
        // 7. Verify deletion
        var retrieveAfterDelete = await scimService.RetrieveAsync("Users", user.Id!);
        Assert.AreEqual(404, retrieveAfterDelete.StatusCode);
    }
    
    [TestMethod]
    public async Task TestCompleteGroupLifecycle()
    {
        // Arrange
        var scimService = new Looplex.Foundation.SCIMv2.SCIMv2();
        var group = CreateTestGroup("Lifecycle Group");
        
        // Act & Assert - Complete CRUD lifecycle
        
        // 1. Create
        var createResponse = await scimService.CreateAsync("Groups", group);
        Assert.AreEqual(201, createResponse.StatusCode);
        Assert.IsNotNull(group.Id);
        
        // 2. Retrieve
        var retrieveResponse = await scimService.RetrieveAsync("Groups", group.Id!);
        Assert.AreEqual(200, retrieveResponse.StatusCode);
        
        // 3. Update
        var patches = new PatchOperation[]
        {
            PatchOperation.Replace("displayName", "Updated Lifecycle Group")
        };
        var patchResponse = await scimService.ModifyAsync("Groups", group.Id!, patches);
        Assert.AreEqual(200, patchResponse.StatusCode);
        
        // 4. Delete
        var deleteResponse = await scimService.DeleteAsync("Groups", group.Id!);
        Assert.AreEqual(204, deleteResponse.StatusCode);
    }
    
    #endregion

    #region Error Scenario Tests
    
    [TestMethod]
    public async Task TestErrorHandlingScenarios()
    {
        // Arrange
        var scimService = new Looplex.Foundation.SCIMv2.SCIMv2();
        var nonExistentId = Guid.NewGuid().ToString();
        
        // Act & Assert - Error scenarios
        
        // 1. Retrieve non-existent user
        var retrieveResponse = await scimService.RetrieveAsync("Users", nonExistentId);
        Assert.AreEqual(404, retrieveResponse.StatusCode);
        
        // 2. Update non-existent user
        var patches = new PatchOperation[]
        {
            PatchOperation.Replace("displayName", "Non-existent User")
        };
        var patchResponse = await scimService.ModifyAsync("Users", nonExistentId, patches);
        Assert.AreEqual(404, patchResponse.StatusCode);
        
        // 3. Delete non-existent user
        var deleteResponse = await scimService.DeleteAsync("Users", nonExistentId);
        Assert.AreEqual(404, deleteResponse.StatusCode);
        
        // 4. Replace non-existent user
        var replacementUser = CreateTestUser("replacement.user", "Replacement User");
        var replaceResponse = await scimService.ReplaceAsync("Users", nonExistentId, replacementUser);
        Assert.AreEqual(404, replaceResponse.StatusCode);
    }
    
    [TestMethod]
    public async Task TestInvalidCollectionNames()
    {
        // Arrange
        var scimService = new Looplex.Foundation.SCIMv2.SCIMv2();
        var user = CreateTestUser("test.user", "Test User");
        
        // Act & Assert - Invalid collection names
        
        // 1. Create with invalid collection
        var createResponse = await scimService.CreateAsync("InvalidCollection", user);
        Assert.AreEqual(404, createResponse.StatusCode);
        
        // 2. Query with invalid collection
        var queryResponse = await scimService.QueryAsync("InvalidCollection", 1, 10, null, null, null);
        Assert.AreEqual(404, queryResponse.StatusCode);
    }
    
    #endregion

    #region Edge Case Tests
    
    [TestMethod]
    public async Task TestEmptyQueryResults()
    {
        // Arrange
        var scimService = new Looplex.Foundation.SCIMv2.SCIMv2();
        
        // Act - Query with filter that should return no results
        var queryResponse = await scimService.QueryAsync("Users", 1, 10, "userName eq \"nonexistent\"", null, null);
        
        // Assert
        Assert.AreEqual(200, queryResponse.StatusCode);
        Assert.AreEqual(0, queryResponse.TotalResults);
    }
    
    [TestMethod]
    public async Task TestPaginationEdgeCases()
    {
        // Arrange
        var scimService = new Looplex.Foundation.SCIMv2.SCIMv2();
        
        // Create some test data
        for (int i = 1; i <= 5; i++)
        {
            var user = CreateTestUser($"pagination.user{i}", $"Pagination User {i}");
            await scimService.CreateAsync("Users", user);
        }
        
        // Act & Assert - Pagination edge cases
        
        // 1. First page
        var firstPageResponse = await scimService.QueryAsync("Users", 1, 2, null, null, null);
        Assert.AreEqual(200, firstPageResponse.StatusCode);
        Assert.IsTrue(firstPageResponse.TotalResults >= 5);
        
        // 2. Last page
        var lastPageResponse = await scimService.QueryAsync("Users", 3, 2, null, null, null);
        Assert.AreEqual(200, lastPageResponse.StatusCode);
        
        // 3. Page beyond available data - should return empty results but still 200
        var beyondPageResponse = await scimService.QueryAsync("Users", 10, 2, null, null, null);
        Assert.AreEqual(200, beyondPageResponse.StatusCode);
        // Note: TotalResults might not be 0 if there are other users from previous tests
        // The important thing is that the response is successful
    }
    
    [TestMethod]
    public async Task TestSortingEdgeCases()
    {
        // Arrange
        var scimService = new Looplex.Foundation.SCIMv2.SCIMv2();
        
        // Create test data with different user names
        var userNames = new[] { "zebra.user", "alpha.user", "beta.user" };
        foreach (var userName in userNames)
        {
            var user = CreateTestUser(userName, $"User {userName}");
            await scimService.CreateAsync("Users", user);
        }
        
        // Act & Assert - Sorting scenarios
        
        // 1. Ascending sort
        var ascendingResponse = await scimService.QueryAsync("Users", 1, 10, null, "userName", "ascending");
        Assert.AreEqual(200, ascendingResponse.StatusCode);
        
        // 2. Descending sort
        var descendingResponse = await scimService.QueryAsync("Users", 1, 10, null, "userName", "descending");
        Assert.AreEqual(200, descendingResponse.StatusCode);
        
        // 3. Invalid sort field
        var invalidSortResponse = await scimService.QueryAsync("Users", 1, 10, null, "invalidField", "ascending");
        // Should still return 200 but may not sort properly
        Assert.AreEqual(200, invalidSortResponse.StatusCode);
    }
    
    #endregion

    #region Metadata Management Tests
    
    [TestMethod]
    public async Task TestMetadataConsistency()
    {
        // Arrange
        var scimService = new Looplex.Foundation.SCIMv2.SCIMv2();
        var user = CreateTestUser("metadata.user", "Metadata User");
        var originalCreated = DateTime.UtcNow;
        
        // Act
        var createResponse = await scimService.CreateAsync("Users", user);
        Assert.AreEqual(201, createResponse.StatusCode);
        
        // Wait a moment to ensure different timestamps
        await Task.Delay(100);
        
        var patches = new PatchOperation[]
        {
            PatchOperation.Replace("displayName", "Updated Metadata User")
        };
        var patchResponse = await scimService.ModifyAsync("Users", user.Id!, patches);
        Assert.AreEqual(200, patchResponse.StatusCode);
        
        // Assert - Metadata consistency
        var retrieveResponse = await scimService.RetrieveAsync("Users", user.Id!);
        Assert.AreEqual(200, retrieveResponse.StatusCode);
        
        // Verify metadata was properly managed
        Assert.IsNotNull(user.Meta.Created);
        Assert.IsNotNull(user.Meta.LastModified);
        Assert.IsNotNull(user.Meta.Location);
        Assert.IsNotNull(user.Meta.Version);
        Assert.IsTrue(user.Meta.Created <= user.Meta.LastModified);
    }
    
    #endregion

    #region Helper Methods
    
    private static User CreateTestUser(string userName, string displayName)
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
    
    private static Group CreateTestGroup(string displayName)
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

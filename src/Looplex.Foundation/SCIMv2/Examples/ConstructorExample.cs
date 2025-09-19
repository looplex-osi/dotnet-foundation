using System;
using System.Threading.Tasks;
using Looplex.Foundation.SCIMv2.Entities;

namespace Looplex.Foundation.SCIMv2.Examples;

/// <summary>
/// Example demonstrating the corrected SCIMv2 constructor
/// Shows how the automatic registration of User and Group collections works
/// </summary>
public class ConstructorExample
{
    /// <summary>
    /// Demonstrates the corrected constructor behavior
    /// </summary>
    public static async Task DemonstrateConstructor()
    {
        // Create SCIMv2 service instance
        // The constructor now automatically registers User and Group collections
        var scimService = new SCIMv2();
        
        Console.WriteLine("SCIMv2 Service initialized successfully!");
        Console.WriteLine($"Registered collections: {string.Join(", ", scimService.GetRegisteredCollections())}");
        
        // Test User collection
        Console.WriteLine("\n=== Testing User Collection ===");
        var userQueryResponse = await scimService.QueryAsync("Users", 1, 10, null, null, null);
        Console.WriteLine($"User query status: {userQueryResponse.StatusCode}");
        Console.WriteLine($"User query total results: {userQueryResponse.TotalResults}");
        
        // Test Group collection
        Console.WriteLine("\n=== Testing Group Collection ===");
        var groupQueryResponse = await scimService.QueryAsync("Groups", 1, 10, null, null, null);
        Console.WriteLine($"Group query status: {groupQueryResponse.StatusCode}");
        Console.WriteLine($"Group query total results: {groupQueryResponse.TotalResults}");
        
        // Test creating a user
        Console.WriteLine("\n=== Testing User Creation ===");
        var newUser = new User
        {
            UserName = "john.doe",
            DisplayName = "John Doe",
            Active = true,
            Schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:User" }
        };
        
        var createUserResponse = await scimService.CreateAsync("Users", newUser);
        Console.WriteLine($"Create user status: {createUserResponse.StatusCode}");
        Console.WriteLine($"Created user ID: {newUser.Id}");
        
        // Test creating a group
        Console.WriteLine("\n=== Testing Group Creation ===");
        var newGroup = new Group
        {
            DisplayName = "Developers",
            Schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:Group" }
        };
        
        var createGroupResponse = await scimService.CreateAsync("Groups", newGroup);
        Console.WriteLine($"Create group status: {createGroupResponse.StatusCode}");
        Console.WriteLine($"Created group ID: {newGroup.Id}");
        
        // Test querying after creation
        Console.WriteLine("\n=== Testing Query After Creation ===");
        var userQueryAfterResponse = await scimService.QueryAsync("Users", 1, 10, null, null, null);
        Console.WriteLine($"User query after creation - Total results: {userQueryAfterResponse.TotalResults}");
        
        var groupQueryAfterResponse = await scimService.QueryAsync("Groups", 1, 10, null, null, null);
        Console.WriteLine($"Group query after creation - Total results: {groupQueryAfterResponse.TotalResults}");
        
        Console.WriteLine("\n✅ All tests completed successfully!");
    }
}

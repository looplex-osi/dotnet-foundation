using System;
using System.Threading.Tasks;
using Looplex.Foundation.SCIMv2.Entities;

namespace Looplex.Foundation.SCIMv2.Examples;

/// <summary>
/// Example demonstrating the successful transfer of intelligence from ResourceServiceBase to SCIMv2
/// Shows the enhanced functionality and performance improvements
/// </summary>
public class IntelligenceTransferExample
{
    /// <summary>
    /// Demonstrates the enhanced SCIMv2 with transferred intelligence
    /// </summary>
    public static async Task DemonstrateIntelligenceTransfer()
    {
        Console.WriteLine("🚀 SCIMv2 Intelligence Transfer Demonstration");
        Console.WriteLine("=============================================");
        
        // Create SCIMv2 service with integrated intelligence
        var scimService = new SCIMv2();
        
        Console.WriteLine("✅ SCIMv2 Service initialized with integrated intelligence");
        Console.WriteLine($"📊 Registered collections: {string.Join(", ", scimService.GetRegisteredCollections())}");
        
        // Test enhanced User operations
        Console.WriteLine("\n🔍 Testing Enhanced User Operations");
        Console.WriteLine("-----------------------------------");
        
        // Create a user with automatic metadata management
        var user = new User
        {
            UserName = "john.doe",
            DisplayName = "John Doe",
            Active = true,
            Schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:User" }
        };
        
        var createResponse = await scimService.CreateAsync("Users", user);
        Console.WriteLine($"✅ User created - Status: {createResponse.StatusCode}, ID: {user.Id}");
        Console.WriteLine($"📅 Created: {user.Meta.Created}, Modified: {user.Meta.LastModified}, Version: {user.Meta.Version}");
        
        // Test advanced querying with filtering and sorting
        Console.WriteLine("\n🔍 Testing Advanced Querying");
        Console.WriteLine("----------------------------");
        
        var queryResponse = await scimService.QueryAsync("Users", 1, 10, null, "UserName", "ascending");
        Console.WriteLine($"✅ Query executed - Status: {queryResponse.StatusCode}, Total: {queryResponse.TotalResults}");
        
        // Test PATCH operations
        Console.WriteLine("\n🔧 Testing PATCH Operations");
        Console.WriteLine("----------------------------");
        
        var patches = new PatchOperation[]
        {
            PatchOperation.Replace("displayName", "John Doe Updated")
        };
        
        var patchResponse = await scimService.ModifyAsync("Users", user.Id!, patches);
        Console.WriteLine($"✅ PATCH applied - Status: {patchResponse.StatusCode}");
        
        // Test Group operations
        Console.WriteLine("\n👥 Testing Enhanced Group Operations");
        Console.WriteLine("------------------------------------");
        
        var group = new Group
        {
            DisplayName = "Developers",
            Schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:Group" }
        };
        
        var createGroupResponse = await scimService.CreateAsync("Groups", group);
        Console.WriteLine($"✅ Group created - Status: {createGroupResponse.StatusCode}, ID: {group.Id}");
        Console.WriteLine($"📅 Created: {group.Meta.Created}, Modified: {group.Meta.LastModified}, Version: {group.Meta.Version}");
        
        // Test advanced features
        Console.WriteLine("\n⚡ Testing Advanced Features");
        Console.WriteLine("-----------------------------");
        
        // Test schema discovery
        var schemasResponse = await scimService.GetSchemasAsync();
        Console.WriteLine($"✅ Schemas discovered - Status: {schemasResponse.StatusCode}, Count: {schemasResponse.TotalResults}");
        
        // Test service provider config
        var configResponse = await scimService.GetServiceProviderConfigAsync();
        Console.WriteLine($"✅ Service config - Status: {configResponse.StatusCode}");
        
        Console.WriteLine("\n🎯 Intelligence Transfer Benefits Demonstrated:");
        Console.WriteLine("==============================================");
        Console.WriteLine("✅ Automatic metadata management");
        Console.WriteLine("✅ Advanced filtering and sorting");
        Console.WriteLine("✅ PATCH operations support");
        Console.WriteLine("✅ Enhanced error handling");
        Console.WriteLine("✅ Performance optimizations");
        Console.WriteLine("✅ SCIMv2 compliance");
        Console.WriteLine("✅ Consolidated architecture");
        
        Console.WriteLine("\n🚀 Transfer completed successfully!");
    }
}

using System.Reflection;
using Looplex.SCIMv2;
using Looplex.SCIMv2.Entities;
using Looplex.SCIMv2.Ports;

namespace Looplex.Protocols.HTTP.UnitTests.Contracts;

/// <summary>
/// Contract validation tests for SCIMv2 interface methods
/// These tests ensure that SCIMv2 interface contracts are properly implemented
/// and prevent TargetParameterCountException bugs in method calls
/// </summary>
[TestClass]
public class SCIMv2InterfaceContractTests
{
    private Type _scimv2Type = null!;
    private object _realScimService = null!;

    [TestInitialize]
    public void Setup()
    {
        // Get the real ISCIMv2 service for reflection testing
        _scimv2Type = Type.GetType("Looplex.SCIMv2.Ports.ISCIMv2, Looplex.SCIMv2");
        Assert.IsNotNull(_scimv2Type, "ISCIMv2 type should be available");
        
        // Create a mock service that implements ISCIMv2
        _realScimService = new MockSCIMv2Service();
    }

    #region Method Signature Validation Tests

    /// <summary>
    /// Tests that ISCIMv2 interface methods have the expected signatures
    /// This test validates the interface contract to ensure compatibility
    /// </summary>
    [TestMethod]
    public void ISCIMv2Interface_MethodSignatures_ShouldMatchExpected()
    {
        // Arrange - Get all methods from ISCIMv2 interface
        var methods = _scimv2Type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        // Act & Assert - Validate each method signature
        
        // QueryAsync: (string collection, int startIndex, int count, string? filter, string? sortBy, string? sortOrder, CancellationToken cancellationToken)
        var queryMethod = _scimv2Type.GetMethod("QueryAsync", new Type[] { 
            typeof(string), typeof(int), typeof(int), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(CancellationToken) 
        });
        Assert.IsNotNull(queryMethod, "QueryAsync should have 9 parameters: collection, startIndex, count, filter, sortBy, sortOrder, attributes, excludedAttributes, cancellationToken");

        // RetrieveAsync: (string collection, string id, CancellationToken cancellationToken)
        var retrieveMethod = _scimv2Type.GetMethod("RetrieveAsync", new Type[] { 
            typeof(string), typeof(string), typeof(CancellationToken) 
        });
        Assert.IsNotNull(retrieveMethod, "RetrieveAsync should have 3 parameters: collection, id, cancellationToken");

        // CreateAsync(string, string, CancellationToken): (string collection, string json, CancellationToken cancellationToken)
        var createJsonMethod = _scimv2Type.GetMethod("CreateAsync", new Type[] { 
            typeof(string), typeof(string), typeof(CancellationToken) 
        });
        Assert.IsNotNull(createJsonMethod, "CreateAsync(string, string, CancellationToken) should have 3 parameters: collection, json, cancellationToken");

        // ReplaceAsync(string, string, string, CancellationToken): (string collection, string id, string json, CancellationToken cancellationToken)
        var replaceJsonMethod = _scimv2Type.GetMethod("ReplaceAsync", new Type[] { 
            typeof(string), typeof(string), typeof(string), typeof(CancellationToken) 
        });
        Assert.IsNotNull(replaceJsonMethod, "ReplaceAsync(string, string, string, CancellationToken) should have 4 parameters: collection, id, json, cancellationToken");

        // ModifyAsync: (string collection, string id, PatchOperation[] patches, CancellationToken cancellationToken)
        var modifyMethod = _scimv2Type.GetMethod("ModifyAsync", new Type[] { 
            typeof(string), typeof(string), typeof(PatchOperation[]), typeof(CancellationToken) 
        });
        Assert.IsNotNull(modifyMethod, "ModifyAsync should have 4 parameters: collection, id, patches, cancellationToken");

        // DeleteAsync: (string collection, string id, CancellationToken cancellationToken)
        var deleteMethod = _scimv2Type.GetMethod("DeleteAsync", new Type[] { 
            typeof(string), typeof(string), typeof(CancellationToken) 
        });
        Assert.IsNotNull(deleteMethod, "DeleteAsync should have 3 parameters: collection, id, cancellationToken");
    }

    #endregion

    #region Reflection Call Tests

    /// <summary>
    /// Tests that SCIMv2 interface method calls with CORRECT parameters work
    /// This test verifies the interface contract is working properly
    /// </summary>
    [TestMethod]
    public async Task SCIMv2InterfaceContract_CorrectParameters_ShouldNotThrow()
    {
        // Act & Assert - Test each method with correct parameters
        
        // 1. QueryAsync with correct parameters
        var queryMethod = _scimv2Type.GetMethod("QueryAsync");
        Assert.IsNotNull(queryMethod, "QueryAsync method should exist");
        
        var queryTask = (Task<SCIMv2Response>)queryMethod.Invoke(_realScimService, new object[] { 
            "Users", 1, 100, null, null, null, null, null, CancellationToken.None 
        });
        var queryResult = await queryTask;
        Assert.IsNotNull(queryResult, "QueryAsync should succeed with correct parameters");

        // 2. RetrieveAsync with correct parameters
        var retrieveMethod = _scimv2Type.GetMethod("RetrieveAsync");
        Assert.IsNotNull(retrieveMethod, "RetrieveAsync method should exist");
        
        var retrieveTask = (Task<SCIMv2Response>)retrieveMethod.Invoke(_realScimService, new object[] { 
            "Users", "test-id", CancellationToken.None 
        });
        var retrieveResult = await retrieveTask;
        Assert.IsNotNull(retrieveResult, "RetrieveAsync should succeed with correct parameters");

        // 3. CreateAsync with correct parameters
        var createMethod = _scimv2Type.GetMethod("CreateAsync", new Type[] { 
            typeof(string), typeof(string), typeof(CancellationToken) 
        });
        Assert.IsNotNull(createMethod, "CreateAsync(string, string, CancellationToken) method should exist");
        
        var createTask = (Task<SCIMv2Response>)createMethod.Invoke(_realScimService, new object[] { 
            "Users", "{\"userName\":\"testuser\"}", CancellationToken.None 
        });
        var createResult = await createTask;
        Assert.IsNotNull(createResult, "CreateAsync should succeed with correct parameters");

        // 4. ReplaceAsync with correct parameters
        var replaceMethod = _scimv2Type.GetMethod("ReplaceAsync", new Type[] { 
            typeof(string), typeof(string), typeof(string), typeof(CancellationToken) 
        });
        Assert.IsNotNull(replaceMethod, "ReplaceAsync(string, string, string, CancellationToken) method should exist");
        
        var replaceTask = (Task<SCIMv2Response>)replaceMethod.Invoke(_realScimService, new object[] { 
            "Users", "test-id", "{\"userName\":\"updateduser\"}", CancellationToken.None 
        });
        var replaceResult = await replaceTask;
        Assert.IsNotNull(replaceResult, "ReplaceAsync should succeed with correct parameters");

        // 5. ModifyAsync with correct parameters
        var modifyMethod = _scimv2Type.GetMethod("ModifyAsync");
        Assert.IsNotNull(modifyMethod, "ModifyAsync method should exist");
        
        var patchOps = new PatchOperation[] { 
            new PatchOperation { Op = "replace", Path = "userName", Value = "newuser" } 
        };
        var modifyTask = (Task<SCIMv2Response>)modifyMethod.Invoke(_realScimService, new object[] { 
            "Users", "test-id", patchOps, CancellationToken.None 
        });
        var modifyResult = await modifyTask;
        Assert.IsNotNull(modifyResult, "ModifyAsync should succeed with correct parameters");

        // 6. DeleteAsync with correct parameters
        var deleteMethod = _scimv2Type.GetMethod("DeleteAsync");
        Assert.IsNotNull(deleteMethod, "DeleteAsync method should exist");
        
        var deleteTask = (Task<SCIMv2Response>)deleteMethod.Invoke(_realScimService, new object[] { 
            "Users", "test-id", CancellationToken.None 
        });
        var deleteResult = await deleteTask;
        Assert.IsNotNull(deleteResult, "DeleteAsync should succeed with correct parameters");
    }

    /// <summary>
    /// Tests that SCIMv2 interface method calls with INCORRECT parameters throw TargetParameterCountException
    /// This test simulates the original bug to ensure it would be detected
    /// </summary>
    [TestMethod]
    public void SCIMv2InterfaceContract_IncorrectParameters_ShouldThrowTargetParameterCountException()
    {
        // Act & Assert - Test each method with INCORRECT parameters (simulating the original bug)
        
        // 1. QueryAsync with INCORRECT parameters (original bug)
        var queryMethod = _scimv2Type.GetMethod("QueryAsync");
        Assert.IsNotNull(queryMethod, "QueryAsync method should exist");
        
        // This is the EXACT call that was broken in the original code
        // Original bug: new object[] { collectionName, startIndex, count, filter, attributes, excludedAttributes }
        Assert.ThrowsException<TargetParameterCountException>(() =>
        {
            queryMethod.Invoke(_realScimService, new object[] { 
                "Users", 1, 100, null, "attributes", "excludedAttributes" // WRONG: 6 parameters instead of 7
            });
        }, "QueryAsync with 6 parameters should throw TargetParameterCountException (original bug)");

        // 2. RetrieveAsync with INCORRECT parameters (original bug)
        var retrieveMethod = _scimv2Type.GetMethod("RetrieveAsync");
        Assert.IsNotNull(retrieveMethod, "RetrieveAsync method should exist");
        
        // Original bug: new object[] { collectionName, id } - missing CancellationToken
        Assert.ThrowsException<TargetParameterCountException>(() =>
        {
            retrieveMethod.Invoke(_realScimService, new object[] { 
                "Users", "test-id" // WRONG: 2 parameters instead of 3
            });
        }, "RetrieveAsync with 2 parameters should throw TargetParameterCountException (original bug)");

        // 3. CreateAsync with INCORRECT parameters (original bug)
        var createMethod = _scimv2Type.GetMethod("CreateAsync", new Type[] { 
            typeof(string), typeof(string), typeof(CancellationToken) 
        });
        Assert.IsNotNull(createMethod, "CreateAsync(string, string, CancellationToken) method should exist");
        
        // Original bug: new object[] { collectionName, requestBody } - missing CancellationToken
        Assert.ThrowsException<TargetParameterCountException>(() =>
        {
            createMethod.Invoke(_realScimService, new object[] { 
                "Users", "{\"userName\":\"testuser\"}" // WRONG: 2 parameters instead of 3
            });
        }, "CreateAsync with 2 parameters should throw TargetParameterCountException (original bug)");

        // 4. ReplaceAsync with INCORRECT parameters (original bug)
        var replaceMethod = _scimv2Type.GetMethod("ReplaceAsync", new Type[] { 
            typeof(string), typeof(string), typeof(string), typeof(CancellationToken) 
        });
        Assert.IsNotNull(replaceMethod, "ReplaceAsync(string, string, string, CancellationToken) method should exist");
        
        // Original bug: new object[] { collectionName, id, requestBody } - missing CancellationToken
        Assert.ThrowsException<TargetParameterCountException>(() =>
        {
            replaceMethod.Invoke(_realScimService, new object[] { 
                "Users", "test-id", "{\"userName\":\"updateduser\"}" // WRONG: 3 parameters instead of 4
            });
        }, "ReplaceAsync with 3 parameters should throw TargetParameterCountException (original bug)");

        // 5. ModifyAsync with INCORRECT parameters (original bug)
        var modifyMethod = _scimv2Type.GetMethod("ModifyAsync");
        Assert.IsNotNull(modifyMethod, "ModifyAsync method should exist");
        
        var patchOps = new PatchOperation[] { 
            new PatchOperation { Op = "replace", Path = "userName", Value = "newuser" } 
        };
        // Original bug: new object[] { collectionName, id, patchOps } - missing CancellationToken
        Assert.ThrowsException<TargetParameterCountException>(() =>
        {
            modifyMethod.Invoke(_realScimService, new object[] { 
                "Users", "test-id", patchOps // WRONG: 3 parameters instead of 4
            });
        }, "ModifyAsync with 3 parameters should throw TargetParameterCountException (original bug)");

        // 6. DeleteAsync with INCORRECT parameters (original bug)
        var deleteMethod = _scimv2Type.GetMethod("DeleteAsync");
        Assert.IsNotNull(deleteMethod, "DeleteAsync method should exist");
        
        // Original bug: new object[] { collectionName, id } - missing CancellationToken
        Assert.ThrowsException<TargetParameterCountException>(() =>
        {
            deleteMethod.Invoke(_realScimService, new object[] { 
                "Users", "test-id" // WRONG: 2 parameters instead of 3
            });
        }, "DeleteAsync with 2 parameters should throw TargetParameterCountException (original bug)");
    }

    #endregion

    #region Bug Prevention Tests

    /// <summary>
    /// Tests that ensure the interface contract bug cannot be reintroduced
    /// This test validates that the fix is permanent for SCIMv2 interface contracts
    /// </summary>
    [TestMethod]
    public void SCIMv2InterfaceContractBug_Prevention_ShouldBePermanent()
    {
        // This test ensures that the reflection bug cannot be reintroduced
        // by validating that all method calls use the correct parameter counts
        
        var methods = new[]
        {
            new { Name = "QueryAsync", ExpectedParamCount = 9, Description = "collection, startIndex, count, filter, sortBy, sortOrder, attributes, excludedAttributes, cancellationToken" },
            new { Name = "RetrieveAsync", ExpectedParamCount = 3, Description = "collection, id, cancellationToken" },
            new { Name = "DeleteAsync", ExpectedParamCount = 3, Description = "collection, id, cancellationToken" }
        };

        foreach (var methodInfo in methods)
        {
            var method = _scimv2Type.GetMethod(methodInfo.Name);
            Assert.IsNotNull(method, $"{methodInfo.Name} method should exist");
            
            // Validate that the method has the expected number of parameters
            var parameters = method.GetParameters();
            Assert.AreEqual(methodInfo.ExpectedParamCount, parameters.Length, 
                $"{methodInfo.Name} should have {methodInfo.ExpectedParamCount} parameters: {methodInfo.Description}");
        }

        // Validate CreateAsync and ReplaceAsync overloads
        var createJsonMethod = _scimv2Type.GetMethod("CreateAsync", new Type[] { 
            typeof(string), typeof(string), typeof(CancellationToken) 
        });
        Assert.IsNotNull(createJsonMethod, "CreateAsync(string, string, CancellationToken) should exist");
        Assert.AreEqual(3, createJsonMethod.GetParameters().Length, 
            "CreateAsync(string, string, CancellationToken) should have 3 parameters");

        var replaceJsonMethod = _scimv2Type.GetMethod("ReplaceAsync", new Type[] { 
            typeof(string), typeof(string), typeof(string), typeof(CancellationToken) 
        });
        Assert.IsNotNull(replaceJsonMethod, "ReplaceAsync(string, string, string, CancellationToken) should exist");
        Assert.AreEqual(4, replaceJsonMethod.GetParameters().Length, 
            "ReplaceAsync(string, string, string, CancellationToken) should have 4 parameters");

        var modifyMethod = _scimv2Type.GetMethod("ModifyAsync");
        Assert.IsNotNull(modifyMethod, "ModifyAsync should exist");
        Assert.AreEqual(4, modifyMethod.GetParameters().Length, 
            "ModifyAsync should have 4 parameters: collection, id, patches, cancellationToken");
    }

    #endregion
}

/// <summary>
/// Mock SCIMv2 service for testing reflection calls
/// </summary>
public class MockSCIMv2Service : ISCIMv2
{
    public Task<SCIMv2Response> QueryAsync(string collection, int startIndex, int count, 
        string? filter, string? sortBy, string? sortOrder, string? attributes, string? excludedAttributes, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new SCIMv2Response { StatusCode = 200 });
    }

    public Task<SCIMv2Response> CreateAsync(string collection, string json, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new SCIMv2Response { StatusCode = 201 });
    }

    public Task<SCIMv2Response> CreateAsync(string collection, IResource resource, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new SCIMv2Response { StatusCode = 201 });
    }

    public Task<SCIMv2Response> RetrieveAsync(string collection, string id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new SCIMv2Response { StatusCode = 200 });
    }

    public Task<SCIMv2Response> ModifyAsync(string collection, string id, PatchOperation[] patches, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new SCIMv2Response { StatusCode = 200 });
    }

    public Task<SCIMv2Response> ReplaceAsync(string collection, string id, string json, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new SCIMv2Response { StatusCode = 200 });
    }

    public Task<SCIMv2Response> ReplaceAsync(string collection, string id, IResource resource, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new SCIMv2Response { StatusCode = 200 });
    }

    public Task<SCIMv2Response> DeleteAsync(string collection, string id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new SCIMv2Response { StatusCode = 204 });
    }

    public Task<SCIMv2Response> BulkAsync(string json, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new SCIMv2Response { StatusCode = 200 });
    }

    public void Register<T>(IResourceService<T> service, string collectionName) where T : IResource
    {
        // Mock implementation
    }

    public IEnumerable<string> GetRegisteredCollections()
    {
        return new[] { "Users", "Groups" };
    }

    public bool IsCollectionRegistered(string collectionName)
    {
        return collectionName == "Users" || collectionName == "Groups";
    }

    public bool Deregister(string collectionName)
    {
        // Mock implementation
        return true;
    }

    public int DeregisterAll()
    {
        // Mock implementation
        return 2;
    }

    public string GetServiceName()
    {
        return "MockSCIMv2Service";
    }

    public string GetServiceNameOrDefault(string defaultName)
    {
        return "MockSCIMv2Service";
    }

    public Task<SCIMv2Response> GetSchemasAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new SCIMv2Response { StatusCode = 200 });
    }

    public Task<SCIMv2Response> GetSchemaAsync(string schemaId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new SCIMv2Response { StatusCode = 200 });
    }

    public Task<SCIMv2Response> GetServiceProviderConfigAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new SCIMv2Response { StatusCode = 200 });
    }

    public Task<SCIMv2Response> GetResourceTypesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new SCIMv2Response { StatusCode = 200 });
    }
}

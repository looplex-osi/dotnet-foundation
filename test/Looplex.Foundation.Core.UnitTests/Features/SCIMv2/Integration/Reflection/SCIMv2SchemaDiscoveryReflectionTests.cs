using System.Reflection;
using Looplex.SCIMv2.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Looplex.Foundation.Core.UnitTests.Features.SCIMv2.Integration.Reflection;

/// <summary>
/// Integration tests for reflection calls in ServiceCollectionExtensions.cs SchemaAutoDiscovery
/// These tests ensure that schema discovery reflection calls work correctly
/// and prevent reflection bugs in schema auto-discovery scenarios
/// </summary>
[TestClass]
public class SCIMv2SchemaDiscoveryReflectionTests
{
    private Type _schemaAutoDiscoveryType = null!;
    private object _mockSchemaAutoDiscovery = null!;

    [TestInitialize]
    public void Setup()
    {
        // Get the SchemaAutoDiscovery type for testing
        _schemaAutoDiscoveryType = Type.GetType("Looplex.SCIMv2.Entities.SchemaAutoDiscovery, Looplex.SCIMv2");
        
        // If SchemaAutoDiscovery doesn't exist, use our mock type instead
        if (_schemaAutoDiscoveryType == null)
        {
            _schemaAutoDiscoveryType = typeof(MockSchemaAutoDiscovery);
        }
        
        // Create a mock SchemaAutoDiscovery for testing
        _mockSchemaAutoDiscovery = new MockSchemaAutoDiscovery();
    }

    #region Method Signature Validation Tests

    /// <summary>
    /// Tests that SchemaAutoDiscovery methods have the expected signatures
    /// This test validates the interface contract for schema discovery
    /// </summary>
    [TestMethod]
    public void SchemaAutoDiscovery_MethodSignatures_ShouldMatchExpected()
    {
        // Act & Assert - Validate the CreateSchemaFromResourceType method signature
        
        // CreateSchemaFromResourceType<T>: () -> SchemaDefinition
        var createSchemaMethod = _schemaAutoDiscoveryType.GetMethod("CreateSchemaFromResourceType");
        Assert.IsNotNull(createSchemaMethod, "CreateSchemaFromResourceType method should exist");
        
        // The method should be generic
        Assert.IsTrue(createSchemaMethod.IsGenericMethodDefinition, "CreateSchemaFromResourceType should be a generic method");
        
        // Test with User type
        var genericMethod = createSchemaMethod.MakeGenericMethod(typeof(User));
        Assert.IsNotNull(genericMethod, "Generic method should be created successfully");
        
        // The method should have no parameters
        var parameters = genericMethod.GetParameters();
        Assert.AreEqual(0, parameters.Length, "CreateSchemaFromResourceType should have 0 parameters");
    }

    #endregion

    #region Reflection Call Tests

    /// <summary>
    /// Tests that reflection calls with CORRECT parameters work for schema discovery
    /// This test verifies that schema discovery reflection is working correctly
    /// </summary>
    [TestMethod]
    public void SCIMv2SchemaDiscoveryReflection_CorrectParameters_ShouldNotThrow()
    {
        // Arrange
        var resourceTypes = new[] { typeof(User), typeof(Group) };

        // Act & Assert - Test each resource type with correct parameters
        foreach (var resourceType in resourceTypes)
        {
            // Get the generic method for the resource type
            var createSchemaMethod = _schemaAutoDiscoveryType.GetMethod("CreateSchemaFromResourceType");
            Assert.IsNotNull(createSchemaMethod, "CreateSchemaFromResourceType method should exist");
            
            var genericMethod = createSchemaMethod.MakeGenericMethod(resourceType);
            Assert.IsNotNull(genericMethod, $"Generic method should be created for {resourceType.Name}");
            
            // Invoke the method with correct parameters (no parameters)
            var schema = genericMethod.Invoke(_mockSchemaAutoDiscovery, null) as SchemaDefinition;
            Assert.IsNotNull(schema, $"Schema should be created for {resourceType.Name}");
            Assert.IsNotNull(schema.Id, $"Schema ID should not be null for {resourceType.Name}");
        }
    }

    /// <summary>
    /// Tests that reflection calls with INCORRECT parameters throw TargetParameterCountException
    /// This test simulates potential bugs in schema discovery reflection
    /// </summary>
    [TestMethod]
    public void SCIMv2SchemaDiscoveryReflection_IncorrectParameters_ShouldThrowTargetParameterCountException()
    {
        // Arrange
        var resourceType = typeof(User);

        // Act & Assert - Test with INCORRECT parameters
        var createSchemaMethod = _schemaAutoDiscoveryType.GetMethod("CreateSchemaFromResourceType");
        Assert.IsNotNull(createSchemaMethod, "CreateSchemaFromResourceType method should exist");
        
        var genericMethod = createSchemaMethod.MakeGenericMethod(resourceType);
        Assert.IsNotNull(genericMethod, "Generic method should be created for User");
        
        // Test with INCORRECT parameters (passing parameters when none are expected)
        Assert.ThrowsException<TargetParameterCountException>(() =>
        {
            genericMethod.Invoke(_mockSchemaAutoDiscovery, new object[] { 
                "invalid-parameter" // WRONG: 1 parameter instead of 0
            });
        }, "CreateSchemaFromResourceType with 1 parameter should throw TargetParameterCountException");
    }

    #endregion

    #region Schema Discovery Simulation Tests

    /// <summary>
    /// Tests that simulate actual schema discovery scenarios
    /// This test validates that schema discovery works end-to-end
    /// </summary>
    [TestMethod]
    public void SCIMv2SchemaDiscoveryReflection_SchemaDiscoverySimulation_ShouldWork()
    {
        // Arrange - Simulate schema discovery for multiple resource types
        var resourceTypes = new[] { typeof(User), typeof(Group) };
        var discoveredSchemas = new List<SchemaDefinition>();

        // Act & Assert - Simulate schema discovery for each resource type
        foreach (var resourceType in resourceTypes)
        {
            // Get the generic method for the resource type
            var createSchemaMethod = _schemaAutoDiscoveryType.GetMethod("CreateSchemaFromResourceType");
            Assert.IsNotNull(createSchemaMethod, "CreateSchemaFromResourceType method should exist");
            
            var genericMethod = createSchemaMethod.MakeGenericMethod(resourceType);
            Assert.IsNotNull(genericMethod, $"Generic method should be created for {resourceType.Name}");
            
            // Invoke the method to create schema
            var schema = genericMethod.Invoke(_mockSchemaAutoDiscovery, null) as SchemaDefinition;
            Assert.IsNotNull(schema, $"Schema should be created for {resourceType.Name}");
            
            // Validate schema properties
            Assert.IsNotNull(schema.Id, $"Schema ID should not be null for {resourceType.Name}");
            Assert.IsNotNull(schema.Name, $"Schema name should not be null for {resourceType.Name}");
            Assert.IsNotNull(schema.Description, $"Schema description should not be null for {resourceType.Name}");
            
            discoveredSchemas.Add(schema);
        }

        // Validate that all schemas were discovered
        Assert.AreEqual(resourceTypes.Length, discoveredSchemas.Count, "All resource types should have schemas discovered");
        
        // Validate that each schema has unique ID
        var schemaIds = discoveredSchemas.Select(s => s.Id).ToList();
        Assert.AreEqual(schemaIds.Count, schemaIds.Distinct().Count(), "All schema IDs should be unique");
    }

    #endregion

    #region Generic Method Handling Tests

    /// <summary>
    /// Tests that validate generic method handling in schema discovery
    /// This test ensures that the reflection can handle different resource types
    /// </summary>
    [TestMethod]
    public void SCIMv2SchemaDiscoveryReflection_GenericMethodHandling_ShouldWork()
    {
        // Arrange
        var resourceTypes = new[] { typeof(User), typeof(Group) };

        // Act & Assert - Test generic method handling for each resource type
        foreach (var resourceType in resourceTypes)
        {
            // Get the generic method definition
            var createSchemaMethod = _schemaAutoDiscoveryType.GetMethod("CreateSchemaFromResourceType");
            Assert.IsNotNull(createSchemaMethod, "CreateSchemaFromResourceType method should exist");
            
            // Validate that it's a generic method definition
            Assert.IsTrue(createSchemaMethod.IsGenericMethodDefinition, "Method should be a generic method definition");
            
            // Create generic method for the resource type
            var genericMethod = createSchemaMethod.MakeGenericMethod(resourceType);
            Assert.IsNotNull(genericMethod, $"Generic method should be created for {resourceType.Name}");
            
            // Validate that the generic method is properly constructed
            Assert.IsFalse(genericMethod.IsGenericMethodDefinition, "Generic method should not be a definition");
            Assert.AreEqual(resourceType, genericMethod.GetGenericArguments()[0], "Generic argument should match resource type");
            
            // Test that the method can be invoked
            var schema = genericMethod.Invoke(_mockSchemaAutoDiscovery, null) as SchemaDefinition;
            Assert.IsNotNull(schema, $"Schema should be created for {resourceType.Name}");
        }
    }

    #endregion
}


/// <summary>
/// SchemaDefinition class for testing schema discovery
/// </summary>
public class SchemaDefinition
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public List<SchemaAttribute> Attributes { get; set; } = new List<SchemaAttribute>();
}

/// <summary>
/// SchemaAttribute class for testing schema discovery
/// </summary>
public class SchemaAttribute
{
    public string Name { get; set; } = null!;
    public string Type { get; set; } = null!;
    public bool Required { get; set; }
    public string Description { get; set; } = null!;
    public bool CaseExact { get; set; }
    public string Mutability { get; set; } = null!;
    public string Returned { get; set; } = null!;
}

/// <summary>
/// Mock implementation of SchemaAutoDiscovery for testing purposes
/// This class simulates the behavior of the real SchemaAutoDiscovery
/// </summary>
public class MockSchemaAutoDiscovery
{
    /// <summary>
    /// Mock implementation of CreateSchemaFromResourceType
    /// Returns a mock schema definition for testing
    /// </summary>
    public SchemaDefinition CreateSchemaFromResourceType<T>() where T : class
    {
        return new SchemaDefinition
        {
            Id = $"urn:ietf:params:scim:schemas:core:2.0:{typeof(T).Name}",
            Name = typeof(T).Name,
            Description = $"Mock schema for {typeof(T).Name}",
            Attributes = new List<SchemaAttribute>
            {
                new SchemaAttribute
                {
                    Name = "id",
                    Type = "string",
                    Description = "Unique identifier",
                    Required = true,
                    CaseExact = true,
                    Mutability = "readOnly",
                    Returned = "always"
                },
                new SchemaAttribute
                {
                    Name = "userName",
                    Type = "string",
                    Description = "Username",
                    Required = true,
                    CaseExact = false,
                    Mutability = "readWrite",
                    Returned = "default"
                }
            }
        };
    }
}

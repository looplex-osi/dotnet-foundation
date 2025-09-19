using Microsoft.VisualStudio.TestTools.UnitTesting;
using Looplex.Foundation.SCIMv2;
using Looplex.Foundation.SCIMv2.Entities;
using Looplex.Foundation.SCIMv2.Modules;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Looplex.Foundation.UnitTests.Features.SCIMv2.Entities
{
    /// <summary>
    /// Testes para funcionalidades de Deregister do SCIMv2
    /// </summary>
    [TestClass]
    public class DeregisterTests
    {
        [TestMethod]
        public void TestDeregisterSingleCollection()
        {
            // Arrange
            var scimService = new Looplex.Foundation.SCIMv2.SCIMv2();
            
            // Verify initial state
            var initialCollections = scimService.GetRegisteredCollections().ToList();
            Assert.IsTrue(initialCollections.Count >= 2, "Should have at least Users and Groups registered");
            Assert.IsTrue(scimService.IsCollectionRegistered("Users"), "Users should be registered");
            Assert.IsTrue(scimService.IsCollectionRegistered("Groups"), "Groups should be registered");

            // Act - Deregister Users
            var result = scimService.Deregister("Users");

            // Assert
            Assert.IsTrue(result, "Deregister should return true for existing collection");
            Assert.IsFalse(scimService.IsCollectionRegistered("Users"), "Users should no longer be registered");
            Assert.IsTrue(scimService.IsCollectionRegistered("Groups"), "Groups should still be registered");
            
            var remainingCollections = scimService.GetRegisteredCollections().ToList();
            Assert.IsFalse(remainingCollections.Contains("Users"), "Users should not be in remaining collections");
            Assert.IsTrue(remainingCollections.Contains("Groups"), "Groups should still be in remaining collections");
        }

        [TestMethod]
        public void TestDeregisterNonExistentCollection()
        {
            // Arrange
            var scimService = new Looplex.Foundation.SCIMv2.SCIMv2();

            // Act - Deregister non-existent collection
            var result = scimService.Deregister("NonExistentCollection");

            // Assert
            Assert.IsFalse(result, "Deregister should return false for non-existent collection");
        }

        [TestMethod]
        public void TestDeregisterWithEmptyCollectionName()
        {
            // Arrange
            var scimService = new Looplex.Foundation.SCIMv2.SCIMv2();

            // Act & Assert - Should throw ArgumentException
            Assert.ThrowsException<ArgumentException>(() => scimService.Deregister(""));
            Assert.ThrowsException<ArgumentException>(() => scimService.Deregister(null));
        }

        [TestMethod]
        public void TestDeregisterAll()
        {
            // Arrange
            var scimService = new Looplex.Foundation.SCIMv2.SCIMv2();
            
            // Verify initial state
            var initialCollections = scimService.GetRegisteredCollections().ToList();
            Assert.IsTrue(initialCollections.Count >= 2, "Should have at least Users and Groups registered");

            // Act - Deregister all
            var deregisteredCount = scimService.DeregisterAll();

            // Assert
            Assert.IsTrue(deregisteredCount >= 2, "Should have deregistered at least 2 collections");
            Assert.AreEqual(0, scimService.GetRegisteredCollections().Count(), "Should have no registered collections");
            Assert.IsFalse(scimService.IsCollectionRegistered("Users"), "Users should no longer be registered");
            Assert.IsFalse(scimService.IsCollectionRegistered("Groups"), "Groups should no longer be registered");
        }

        [TestMethod]
        public void TestDeregisterAllOnEmptyService()
        {
            // Arrange
            var scimService = new Looplex.Foundation.SCIMv2.SCIMv2();
            scimService.DeregisterAll(); // Clear all

            // Act - Deregister all on empty service
            var deregisteredCount = scimService.DeregisterAll();

            // Assert
            Assert.AreEqual(0, deregisteredCount, "Should return 0 for empty service");
        }

        [TestMethod]
        public void TestReRegisterAfterDeregister()
        {
            // Arrange
            var scimService = new Looplex.Foundation.SCIMv2.SCIMv2();
            
            // Deregister Users
            scimService.Deregister("Users");
            Assert.IsFalse(scimService.IsCollectionRegistered("Users"), "Users should be deregistered");

            // Act - Re-register Users
            scimService.Register<User>("Users");

            // Assert
            Assert.IsTrue(scimService.IsCollectionRegistered("Users"), "Users should be registered again");
        }

        [TestMethod]
        public void TestDeregisterAndQueryBehavior()
        {
            // Arrange
            var scimService = new Looplex.Foundation.SCIMv2.SCIMv2();
            
            // Deregister Users
            scimService.Deregister("Users");

            // Act & Assert - Query should fail for deregistered collection
            var queryTask = scimService.QueryAsync("Users", 1, 10, null, null, null);
            var result = queryTask.Result;
            
            Assert.AreEqual(404, result.StatusCode, "Query should return 404 for deregistered collection");
            Assert.IsNotNull(result.Error, "Error should be present");
            Assert.IsTrue(result.Error.Detail.Contains("not registered"), "Error should indicate collection not registered");
        }

        [TestMethod]
        public void TestDeregisterAndCreateBehavior()
        {
            // Arrange
            var scimService = new Looplex.Foundation.SCIMv2.SCIMv2();
            
            // Deregister Users
            scimService.Deregister("Users");

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "test.user",
                DisplayName = "Test User",
                Active = true,
                Schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:User" },
                Meta = new ResourceMeta
                {
                    ResourceType = "User",
                    Created = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    Location = "https://example.com/Users/" + Guid.NewGuid().ToString(),
                    Version = "1"
                }
            };

            // Act & Assert - Create should fail for deregistered collection
            var createTask = scimService.CreateAsync("Users", user);
            var result = createTask.Result;
            
            Assert.AreEqual(404, result.StatusCode, "Create should return 404 for deregistered collection");
            Assert.IsNotNull(result.Error, "Error should be present");
            Assert.IsTrue(result.Error.Detail.Contains("not registered"), "Error should indicate collection not registered");
        }
    }
}

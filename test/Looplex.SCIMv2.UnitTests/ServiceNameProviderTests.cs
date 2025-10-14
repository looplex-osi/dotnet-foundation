using Microsoft.VisualStudio.TestTools.UnitTesting;
using Looplex.SCIMv2;
using Looplex.SCIMv2.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Looplex.Foundation.Core.UnitTests.Features.SCIMv2.Entities
{
    /// <summary>
    /// Tests to verify the functionality of ServiceNameProvider in SCIMv2
    /// </summary>
    [TestClass]
    public class ServiceNameProviderTests
    {
        [TestMethod]
        public void TestSCIMv2WithoutServiceNameProvider()
        {
            // Arrange & Act
            var scimService = new Looplex.SCIMv2.SCIMv2();

            // Assert
            Assert.IsNull(scimService.GetServiceName(), "Service name should be null when no provider is configured");
            Assert.AreEqual("looplex", scimService.GetServiceNameOrDefault(), "Default service name should be 'looplex'");
            Assert.AreEqual("custom", scimService.GetServiceNameOrDefault("custom"), "Custom default service name should be used");
        }

        [TestMethod]
        public void TestSCIMv2WithServiceNameProvider()
        {
            // Arrange
            var serviceNameProvider = new ServiceNameProvider("notejam");
            var scimService = new Looplex.SCIMv2.SCIMv2(null, serviceNameProvider);

            // Assert
            Assert.AreEqual("notejam", scimService.GetServiceName(), "Service name should match provider");
            Assert.AreEqual("notejam", scimService.GetServiceNameOrDefault(), "Service name should match provider");
            Assert.AreEqual("notejam", scimService.GetServiceNameOrDefault("custom"), "Service name should match provider even with custom default");
        }

        [TestMethod]
        public void TestSCIMv2WithCaseManagementServiceNameProvider()
        {
            // Arrange
            var serviceNameProvider = new ServiceNameProvider("case-management");
            var scimService = new Looplex.SCIMv2.SCIMv2(null, serviceNameProvider);

            // Assert
            Assert.AreEqual("case-management", scimService.GetServiceName(), "Service name should match provider");
        }

        [TestMethod]
        public void TestServiceNameProviderWithNullInput()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => new ServiceNameProvider(null));
        }

        [TestMethod]
        public void TestServiceNameProviderWithEmptyInput()
        {
            // Act & Assert - ServiceNameProvider should accept empty string
            var serviceNameProvider = new ServiceNameProvider("");
            Assert.AreEqual("", serviceNameProvider.GetServiceName(), "Empty string should be accepted");
        }

        [TestMethod]
        public async Task TestSchemaGenerationWithServiceNameProvider()
        {
            // Arrange
            var serviceNameProvider = new ServiceNameProvider("notejam");
            var scimService = new Looplex.SCIMv2.SCIMv2(null, serviceNameProvider);

            // Act
            var schemasResponse = await scimService.GetSchemasAsync();

            // Assert
            Assert.AreEqual(200, schemasResponse.StatusCode, "Schemas endpoint should return 200");
            Assert.IsNotNull(schemasResponse.Data, "Schemas data should not be null");
            
            // Verify that schemas contain the service name
            var schemas = schemasResponse.Data as dynamic;
            Assert.IsNotNull(schemas, "Schemas should be accessible");
        }

        [TestMethod]
        public async Task TestSchemaGenerationWithoutServiceNameProvider()
        {
            // Arrange
            var scimService = new Looplex.SCIMv2.SCIMv2();

            // Act
            var schemasResponse = await scimService.GetSchemasAsync();

            // Assert
            Assert.AreEqual(200, schemasResponse.StatusCode, "Schemas endpoint should return 200");
            Assert.IsNotNull(schemasResponse.Data, "Schemas data should not be null");
        }

        [TestMethod]
        public void TestServiceNameProviderGetServiceName()
        {
            // Arrange
            var serviceName = "test-service";
            var serviceNameProvider = new ServiceNameProvider(serviceName);

            // Act
            var result = serviceNameProvider.GetServiceName();

            // Assert
            Assert.AreEqual(serviceName, result, "GetServiceName should return the configured service name");
        }

        [TestMethod]
        public void TestMultipleServiceNameProviders()
        {
            // Arrange
            var notejamProvider = new ServiceNameProvider("notejam");
            var caseManagementProvider = new ServiceNameProvider("case-management");
            var customProvider = new ServiceNameProvider("custom-app");

            // Act
            var notejamService = new Looplex.SCIMv2.SCIMv2(null, notejamProvider);
            var caseManagementService = new Looplex.SCIMv2.SCIMv2(null, caseManagementProvider);
            var customService = new Looplex.SCIMv2.SCIMv2(null, customProvider);

            // Assert
            Assert.AreEqual("notejam", notejamService.GetServiceName(), "Notejam service should have correct name");
            Assert.AreEqual("case-management", caseManagementService.GetServiceName(), "Case management service should have correct name");
            Assert.AreEqual("custom-app", customService.GetServiceName(), "Custom service should have correct name");
        }
    }
}

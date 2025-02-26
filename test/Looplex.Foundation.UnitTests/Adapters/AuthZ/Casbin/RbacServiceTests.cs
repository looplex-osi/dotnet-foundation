using System.Reflection;
using Casbin;
using Looplex.Foundation.Adapters.AuthZ.Casbin;
using Looplex.Foundation.OAuth2;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Looplex.Foundation.UnitTests.Adapters.AuthZ.Casbin
{
    [TestClass]
    public class RbacServiceTests
    {
        private RbacService _rbacService = null!;
        private IEnforcer _enforcer = null!;
        private ILogger<RbacService> _logger = null!;
        private IUserContext _userContext = null!;

        [TestInitialize]
        public void SetUp()
        {
            // Set up substitutes
            var testDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                                ?? throw new InvalidOperationException("Could not determine test directory");
            var modelPath = Path.Combine(testDirectory, "model.conf");
            var policyPath = Path.Combine(testDirectory, "policy.csv");

            if (!File.Exists(modelPath) || !File.Exists(policyPath))
            {
                throw new FileNotFoundException("Required Casbin configuration files are missing");
            }

            _enforcer = new Enforcer(modelPath, policyPath);
            _logger = Substitute.For<ILogger<RbacService>>();
            _rbacService = new RbacService(_enforcer, _logger);

            // Mock user context
            _userContext = Substitute.For<IUserContext>();
        }

        [TestMethod]
        public void ThrowIfUnauthorized_UserEmailIsEmpty_ExceptionIsThrown()
        {
            // Arrange
            _userContext.Tenant.Returns("tenant");
            _userContext.Email.Returns("");

            // Act & Assert
            var ex = Assert.ThrowsException<ArgumentNullException>(() => _rbacService.ThrowIfUnauthorized(_userContext, "resource", "read"));
            Assert.AreEqual("USER_EMAIL_REQUIRED_FOR_AUTHORIZATION (Parameter 'email')", ex.Message);
        }

        [TestMethod]
        public void ThrowIfUnauthorized_TenantIsNull_ExceptionIsThrown()
        {
            // Arrange
            _userContext.Tenant.Returns((string?)null);
            _userContext.Email.Returns("user@email.com");

            // Act & Assert
            var ex = Assert.ThrowsException<ArgumentNullException>(() => _rbacService.ThrowIfUnauthorized(_userContext, "resource", "read"));
            Assert.AreEqual("TENANT_REQUIRED_FOR_AUTHORIZATION (Parameter 'tenant')", ex.Message);
        }

        [TestMethod]
        public void ThrowIfUnauthorized_TenantIsEmpty_ExceptionIsThrown()
        {
            // Arrange
            _userContext.Tenant.Returns("");
            _userContext.Email.Returns("user@email.com");

            // Act & Assert
            var ex = Assert.ThrowsException<ArgumentNullException>(() => _rbacService.ThrowIfUnauthorized(_userContext, "resource", "read"));
            Assert.AreEqual("TENANT_REQUIRED_FOR_AUTHORIZATION (Parameter 'tenant')", ex.Message);
        }

        [TestMethod]
        [DataRow("read")]
        [DataRow("write")]
        [DataRow("delete")]
        public void ThrowIfUnauthorized_UserHasPermission_NoExceptionThrown(string action)
        {
            // Arrange
            _userContext.Tenant.Returns("looplex");
            _userContext.Email.Returns("bob.rivest@email.com");

            // Act & Assert (No exception should be thrown)
            _rbacService.ThrowIfUnauthorized(_userContext, "resource", action);
        }

        [TestMethod]
        [DataRow("execute")]
        public void ThrowIfUnauthorized_UserDoesNotHavePermission_ExceptionIsThrown(string action)
        {
            // Arrange
            _userContext.Tenant.Returns("looplex");
            _userContext.Email.Returns("bob.rivest@email.com");

            // Act & Assert
            var ex = Assert.ThrowsException<UnauthorizedAccessException>(() => _rbacService.ThrowIfUnauthorized(_userContext, "resource", action));
            Assert.AreEqual("UNAUTHORIZED_ACCESS", ex.Message);
        }
    }
}
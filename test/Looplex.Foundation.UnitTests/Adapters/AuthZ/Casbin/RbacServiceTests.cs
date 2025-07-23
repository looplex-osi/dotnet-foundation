using System.Reflection;
using System.Security.Claims;

using Casbin;
using Casbin.Model;

using Looplex.Foundation.Adapters.AuthZ.Casbin;

using Microsoft.Extensions.Logging;

using NSubstitute;

using static Org.BouncyCastle.Crypto.Engines.SM2Engine;

namespace Looplex.Foundation.UnitTests.Adapters.AuthZ.Casbin;

[TestClass]
public class RbacServiceTests
{
  private IEnforcer _enforcer = null!;
  private ILogger<RbacService> _logger = null!;
  private RbacService _rbacService = null!;
  private ClaimsPrincipal _user = null!;

  [TestInitialize]
  public void SetUp()
  {
    // Set up substitutes
    string testDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                           ?? throw new InvalidOperationException("Could not determine test directory");
    string policyPath = Path.Combine(testDirectory, "policy.csv");



    // Load model.conf as EmbeddedResource from Foundation
    var modelResource = "Looplex.Foundation.Adapters.AuthZ.Casbin.model.conf";
    var foundationAssembly = typeof(RbacService).Assembly;

    using Stream? modelStream = foundationAssembly.GetManifestResourceStream(modelResource);
    if (modelStream == null)
    {
      throw new FileNotFoundException($"Embedded resource '{modelResource}' not found in Looplex.Foundation.");
    }
    string modelPath = Path.Combine(Path.GetTempPath(), $"model-{Guid.NewGuid()}.conf");
    using (var fileStream = File.Create(modelPath))
    {
      modelStream.CopyTo(fileStream);
    }

    _enforcer = new Enforcer(modelPath, policyPath);
    _logger = Substitute.For<ILogger<RbacService>>();
    _rbacService = new RbacService(_enforcer, _logger);

    // Mock user context
    _user = Substitute.For<ClaimsPrincipal>();
  }

  [TestMethod]
  public void ThrowIfUnauthorized_UserEmailIsEmpty_ExceptionIsThrown()
  {
    // Arrange
    _user.Claims.Returns(new[] { new Claim(ClaimTypes.Email, ""), new Claim("tenant", "tenant") });

    // Act & Assert
    ArgumentNullException ex =
      Assert.ThrowsException<ArgumentNullException>(() =>
        _rbacService.ThrowIfUnauthorized(_user, "resource", "read"));
    Assert.AreEqual("USER_EMAIL_REQUIRED_FOR_AUTHORIZATION (Parameter 'email')", ex.Message);
  }

  [TestMethod]
  public void ThrowIfUnauthorized_TenantIsNull_ExceptionIsThrown()
  {
    // Arrange
    _user.Claims.Returns(new[] { new Claim(ClaimTypes.Email, "user@email.com") });

    // Act & Assert
    ArgumentNullException ex =
      Assert.ThrowsException<ArgumentNullException>(() =>
        _rbacService.ThrowIfUnauthorized(_user, "resource", "read"));
    Assert.AreEqual("TENANT_REQUIRED_FOR_AUTHORIZATION (Parameter 'tenant')", ex.Message);
  }

  [TestMethod]
  public void ThrowIfUnauthorized_TenantIsEmpty_ExceptionIsThrown()
  {
    // Arrange
    _user.Claims.Returns(new[] { new Claim(ClaimTypes.Email, "user@email.com"), new Claim("tenant", "") });

    // Act & Assert
    ArgumentNullException ex =
      Assert.ThrowsException<ArgumentNullException>(() =>
        _rbacService.ThrowIfUnauthorized(_user, "resource", "read"));
    Assert.AreEqual("TENANT_REQUIRED_FOR_AUTHORIZATION (Parameter 'tenant')", ex.Message);
  }

  [TestMethod]
  [DataRow("read")]
  [DataRow("write")]
  [DataRow("delete")]
  public void ThrowIfUnauthorized_UserHasPermission_NoExceptionThrown(string action)
  {
    // Arrange
    _user.Claims.Returns(new[] { new Claim(ClaimTypes.Email, "bob.rivest@email.com"), new Claim("tenant", "looplex") });

    // Act & Assert (No exception should be thrown)
    _rbacService.ThrowIfUnauthorized(_user, "resource", action);
  }

  [TestMethod]
  [DataRow("execute")]
  public void ThrowIfUnauthorized_UserDoesNotHavePermission_ExceptionIsThrown(string action)
  {
    // Arrange
    _user.Claims.Returns(new[] { new Claim(ClaimTypes.Email, "bob.rivest@email.com"), new Claim("tenant", "looplex") });

    // Act & Assert
    UnauthorizedAccessException ex =
      Assert.ThrowsException<UnauthorizedAccessException>(() =>
        _rbacService.ThrowIfUnauthorized(_user, "resource", action));
    Assert.AreEqual("UNAUTHORIZED_ACCESS", ex.Message);
  }

  [TestMethod]
  public void ThrowIfUnauthorized_UserHasDenyPolicy_ExceptionIsThrown()
  {
    _user.Claims.Returns(new[]
    {
    new Claim(ClaimTypes.Email, "alice.rivest@email.com"),
    new Claim("tenant", "looplex")
  });

    var ex = Assert.ThrowsException<UnauthorizedAccessException>(() =>
      _rbacService.ThrowIfUnauthorized(_user, "resource", "delete"));

    Assert.AreEqual("UNAUTHORIZED_ACCESS", ex.Message);
  }
  [TestMethod]
  public void UserInGroup1_CanReadResource()
  {
    _user.Claims.Returns(new[]
    {
    new Claim(ClaimTypes.Email, "bob.rivest@email.com"),
    new Claim("tenant", "looplex")
  });

    _rbacService.ThrowIfUnauthorized(_user, "resource", "read"); // Não lança exceção
  }
  [TestMethod]
  public void UserInGroup1_CanDeleteResource()
  {
    _user.Claims.Returns(new[]
    {
    new Claim(ClaimTypes.Email, "bob.rivest@email.com"),
    new Claim("tenant", "looplex")
  });

    _rbacService.ThrowIfUnauthorized(_user, "resource", "delete"); // Não lança exceção
  }
  [TestMethod]
  public void AliceInGroup2_CanExecuteResource()
  {
    _user.Claims.Returns(new[]
    {
    new Claim(ClaimTypes.Email, "alice.rivest@email.com"),
    new Claim("tenant", "looplex")
  });

    _rbacService.ThrowIfUnauthorized(_user, "resource", "execute"); 
  }
  [TestMethod]
  public void UserInGroup1_CanWriteResource()
  {
    _user.Claims.Returns(new[]
    {
    new Claim(ClaimTypes.Email, "bob.rivest@email.com"),
    new Claim("tenant", "looplex")
  });

    _rbacService.ThrowIfUnauthorized(_user, "resource", "write"); 
  }

  [TestMethod]
  public void AliceIsDeniedDeleteDirectly()
  {
    _user.Claims.Returns(new[]
    {
    new Claim(ClaimTypes.Email, "alice.rivest@email.com"),
    new Claim("tenant", "looplex")
  });

    var ex = Assert.ThrowsException<UnauthorizedAccessException>(() =>
      _rbacService.ThrowIfUnauthorized(_user, "resource", "delete"));
    Assert.AreEqual("UNAUTHORIZED_ACCESS", ex.Message);
  }
  [TestMethod]
  public void UserInGroup1_CannotExecute()
  {
    _user.Claims.Returns(new[]
    {
    new Claim(ClaimTypes.Email, "bob.rivest@email.com"),
    new Claim("tenant", "looplex")
  });

    var ex = Assert.ThrowsException<UnauthorizedAccessException>(() =>
      _rbacService.ThrowIfUnauthorized(_user, "resource", "execute"));
    Assert.AreEqual("UNAUTHORIZED_ACCESS", ex.Message);
  }
}
using System.Security.Claims;
using System.Text.Json;

using Looplex.OAuth2.Entities;
using Looplex.Foundation.Ports;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

using NSubstitute;

namespace Looplex.Foundation.Core.UnitTests.OAuth2.Entities
{
  [TestClass]
  public class ClientServicesTests
  {
    private ClientServices _clientServices = null!;
    private IRbacService _rbacService = null!;
    private IHttpContextAccessor _httpContextAccessor = null!;
    private IConfiguration _configuration = null!;
    private ClaimsPrincipal _user = null!;

    [TestInitialize]
    public void Setup()
    {
      _rbacService = Substitute.For<IRbacService>();
      _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
      _configuration = Substitute.For<IConfiguration>();
      _user = Substitute.For<ClaimsPrincipal>();

      var httpContext = Substitute.For<HttpContext>();
      httpContext.User.Returns(_user);
      _httpContextAccessor.HttpContext.Returns(httpContext);

      _configuration["ClientSecretDigestCost"] = "4";

      _clientServices = new ClientServices(_rbacService, _user, null, _configuration);
    }

    [TestMethod]
    public async Task QueryAsync_ShouldReturnListResponse()
    {
      // Arrange
      var cancellationToken = CancellationToken.None;
      int startIndex = 1;
      int count = 10;
      string? filter = null;
      string? sortBy = null;
      string? sortOrder = null;

      // Act
      var result = await _clientServices.QueryAsync(startIndex, count, filter, sortBy, sortOrder, cancellationToken);

      // Assert
      Assert.IsNotNull(result);
      // Verify the result is a valid response object
      var resultType = result.GetType();
      Assert.IsTrue(resultType.GetProperty("schemas") != null);
      Assert.IsTrue(resultType.GetProperty("totalResults") != null);
      Assert.IsTrue(resultType.GetProperty("itemsPerPage") != null);
      Assert.IsTrue(resultType.GetProperty("startIndex") != null);
      Assert.IsTrue(resultType.GetProperty("Resources") != null);
    }

    [TestMethod]
    public async Task CreateAsync_ShouldReturnCreatedResource()
    {
      // Arrange
      var cancellationToken = CancellationToken.None;
      var clientService = new ClientService
      {
        ClientName = "Test Client",
        UserName = "test@example.com"
      };

      // Act
      var result = await _clientServices.CreateAsync(clientService, cancellationToken);

      // Assert
      Assert.IsNotNull(result);
      // Verify the result is a valid resource object
      var resultType = result.GetType();
      Assert.IsTrue(resultType.GetProperty("schemas") != null);
      Assert.IsTrue(resultType.GetProperty("id") != null);
      Assert.IsTrue(resultType.GetProperty("userName") != null);
    }

    [TestMethod]
    public async Task RetrieveAsync_ShouldReturnResource()
    {
      // Arrange
      var cancellationToken = CancellationToken.None;
      var clientId = Guid.NewGuid();

      // Act
      var result = await _clientServices.RetrieveAsync(clientId, cancellationToken);

      // Assert
      Assert.IsNotNull(result);
      // Verify the result is a valid resource object
      var resultType = result.GetType();
      Assert.IsTrue(resultType.GetProperty("schemas") != null);
      Assert.IsTrue(resultType.GetProperty("id") != null);
      Assert.IsTrue(resultType.GetProperty("userName") != null);
    }

    [TestMethod]
    public async Task ReplaceAsync_ShouldReturnUpdatedResource()
    {
      // Arrange
      var cancellationToken = CancellationToken.None;
      var clientId = Guid.NewGuid();
      var clientService = new ClientService
      {
        ClientName = "Updated Client",
        UserName = "updated@example.com"
      };

      // Act
      var result = await _clientServices.ReplaceAsync(clientId, clientService, cancellationToken);

      // Assert
      Assert.IsNotNull(result);
      // Verify the result is a valid resource object
      var resultType = result.GetType();
      Assert.IsTrue(resultType.GetProperty("schemas") != null);
      Assert.IsTrue(resultType.GetProperty("id") != null);
      Assert.IsTrue(resultType.GetProperty("userName") != null);
    }

    [TestMethod]
    public async Task UpdateAsync_ShouldReturnUpdatedResource()
    {
      // Arrange
      var cancellationToken = CancellationToken.None;
      var clientId = Guid.NewGuid();
      var clientService = new ClientService
      {
        ClientName = "Patched Client",
        UserName = "patched@example.com"
      };
      var operations = JsonDocument.Parse("[]").RootElement;

      // Act
      var result = await _clientServices.UpdateAsync(clientId, clientService, operations, cancellationToken);

      // Assert
      Assert.IsNotNull(result);
      // Verify the result is a valid resource object
      var resultType = result.GetType();
      Assert.IsTrue(resultType.GetProperty("schemas") != null);
      Assert.IsTrue(resultType.GetProperty("id") != null);
      Assert.IsTrue(resultType.GetProperty("userName") != null);
    }

    [TestMethod]
    public async Task DeleteAsync_ShouldCompleteSuccessfully()
    {
      // Arrange
      var cancellationToken = CancellationToken.None;
      var clientId = Guid.NewGuid();

      // Act & Assert
      // Should not throw any exceptions
      await _clientServices.DeleteAsync(clientId, cancellationToken);
    }
  }
}
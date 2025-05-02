using System.Security.Claims;

using Looplex.Foundation.OAuth2.Entities;
using Looplex.Foundation.Ports;
using Looplex.Foundation.SCIMv2.Commands;
using Looplex.Foundation.SCIMv2.Queries;
using Looplex.OpenForExtension.Abstractions.Plugins;

using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

using NSubstitute;

namespace Looplex.Foundation.UnitTests.OAuth2.Entities
{
  [TestClass]
  public class ClientCredentialsTests
  {
    private ClientCredentials _clientCredentials = null!;
    private IRbacService _rbacService = null!;
    private IHttpContextAccessor _httpContextAccessor = null!;
    private IMediator _mediator = null!;
    private IConfiguration _configuration = null!;
    private List<IPlugin> _plugins = null!;
    private ClaimsPrincipal _user = null!;

    [TestInitialize]
    public void Setup()
    {
      _rbacService = Substitute.For<IRbacService>();
      _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
      _mediator = Substitute.For<IMediator>();
      _configuration = Substitute.For<IConfiguration>();
      _plugins = new List<IPlugin>();
      _user = new ClaimsPrincipal();

      var httpContext = Substitute.For<HttpContext>();
      httpContext.User.Returns(_user);
      _httpContextAccessor.HttpContext.Returns(httpContext);

      _configuration["ClientSecretDigestCost"] = "4";
        
      _clientCredentials = new ClientCredentials(_plugins, _rbacService, _httpContextAccessor, _mediator, _configuration);
    }

    [TestMethod]
    public async Task Query_ShouldReturnListResponse()
    {
      // Arrange
      var cancellationToken = CancellationToken.None;
      var expectedClientCredentials = new List<ClientCredential> { new ClientCredential() };
      int expectedTotal = 1;

      _mediator.Send(Arg.Any<QueryResource<ClientCredential>>(), cancellationToken)
        .Returns((expectedClientCredentials, expectedTotal));

      // Act
      var response = await _clientCredentials.Query(1, 10, "filter", "name", "asc", cancellationToken);

      // Assert
      Assert.IsNotNull(response);
      Assert.AreEqual(1, response.TotalResults);
      Assert.AreEqual(1, response.Resources.Count);
    }

    [TestMethod]
    public async Task Create_ShouldReturnGuid()
    {
      // Arrange
      var cancellationToken = CancellationToken.None;
      var clientCredential = new ClientCredential
      {
        ClientSecret = "secret"
      };
      var expectedId = Guid.NewGuid();

      _mediator.Send(Arg.Any<CreateResource<ClientCredential>>(), cancellationToken)
        .Returns(expectedId);

      // Act
      var result = await _clientCredentials.Create(clientCredential, cancellationToken);

      // Assert
      Assert.AreEqual(expectedId, result);
    }

    [TestMethod]
    public async Task Retrieve_ShouldReturnClientCredential()
    {
      // Arrange
      var cancellationToken = CancellationToken.None;
      var expectedClientCredential = new ClientCredential();
      var id = Guid.NewGuid();

      _mediator.Send(Arg.Any<RetrieveResource<ClientCredential>>(), cancellationToken)
        .Returns(expectedClientCredential);

      // Act
      var result = await _clientCredentials.Retrieve(id, cancellationToken);

      // Assert
      Assert.IsNotNull(result);
      Assert.AreEqual(expectedClientCredential, result);
    }

    [TestMethod]
    public async Task RetrieveWithVerify_ShouldReturnClientCredential()
    {
      // Arrange
      var cancellationToken = CancellationToken.None;
      var expectedClientCredential = new ClientCredential
      {
        Digest = "0a714f46-fac5-45a4-a861-ff8f84ba0151:XsMs85RI7tUR2621mObKZMqneEthl53U" 
      };
      var id = Guid.NewGuid();

      _mediator.Send(Arg.Any<RetrieveResource<ClientCredential>>(), cancellationToken)
        .Returns(expectedClientCredential);

      // Act
      var result = await _clientCredentials.Retrieve(id, "secret", cancellationToken);

      // Assert
      Assert.IsNotNull(result);
      Assert.AreEqual(expectedClientCredential, result);
    }

    [TestMethod]
    public async Task Update_ShouldReturnTrue_WhenRowsAffected()
    {
      // Arrange
      var cancellationToken = CancellationToken.None;
      var clientCredential = new ClientCredential();
      var id = Guid.NewGuid();

      _mediator.Send(Arg.Any<UpdateResource<ClientCredential>>(), cancellationToken)
        .Returns(1); // Simulating that one row was affected

      // Act
      var result = await _clientCredentials.Update(id, clientCredential, null, cancellationToken);

      // Assert
      Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task Delete_ShouldReturnTrue_WhenRowsAffected()
    {
      // Arrange
      var cancellationToken = CancellationToken.None;
      var id = Guid.NewGuid();

      _mediator.Send(Arg.Any<DeleteResource<ClientCredential>>(), cancellationToken)
        .Returns(1); // Simulating that one row was affected

      // Act
      var result = await _clientCredentials.Delete(id, cancellationToken);

      // Assert
      Assert.IsTrue(result);
    }
  }
}
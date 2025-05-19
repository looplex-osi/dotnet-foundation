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
  public class ClientServicesTests
  {
    private ClientServices _clientServices = null!;
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

      _clientServices = new ClientServices(_plugins, _rbacService, _httpContextAccessor, _mediator, _configuration);
    }

    [TestMethod]
    public async Task Query_ShouldReturnListResponse()
    {
      // Arrange
      var cancellationToken = CancellationToken.None;
      var expectedClientServices = new List<ClientService> { new ClientService() };
      int expectedTotal = 1;

      _mediator.Send(Arg.Any<QueryResource<ClientService>>(), cancellationToken)
        .Returns((expectedClientServices, expectedTotal));

      // Act
      var response = await _clientServices.Query(1, 10, "filter", "name", "asc", cancellationToken);

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
      var clientService = new ClientService
      {
        ClientSecret = "secret"
      };
      var expectedId = Guid.NewGuid();

      _mediator.Send(Arg.Any<CreateResource<ClientService>>(), cancellationToken)
        .Returns(expectedId);

      // Act
      var result = await _clientServices.Create(clientService, cancellationToken);

      // Assert
      Assert.AreEqual(expectedId, result);
    }

    [TestMethod]
    public async Task Retrieve_ShouldReturnClientService()
    {
      // Arrange
      var cancellationToken = CancellationToken.None;
      var expectedClientService = new ClientService();
      var id = Guid.NewGuid();

      _mediator.Send(Arg.Any<RetrieveResource<ClientService>>(), cancellationToken)
        .Returns(expectedClientService);

      // Act
      var result = await _clientServices.Retrieve(id, cancellationToken);

      // Assert
      Assert.IsNotNull(result);
      Assert.AreEqual(expectedClientService, result);
    }

    [TestMethod]
    public async Task RetrieveWithVerify_ShouldReturnClientService()
    {
      // Arrange
      var cancellationToken = CancellationToken.None;
      var expectedClientService = new ClientService
      {
        Digest = "0a714f46-fac5-45a4-a861-ff8f84ba0151:XsMs85RI7tUR2621mObKZMqneEthl53U"
      };
      var id = Guid.NewGuid();

      _mediator.Send(Arg.Any<RetrieveResource<ClientService>>(), cancellationToken)
        .Returns(expectedClientService);

      // Act
      var result = await _clientServices.Retrieve(id, "secret", cancellationToken);

      // Assert
      Assert.IsNotNull(result);
      Assert.AreEqual(expectedClientService, result);
    }

    // TODO: Fix tests to match correct semantic
    // [TestMethod]
    // public async Task Update_ShouldReturnTrue_WhenRowsAffected()
    // {
    //   // Arrange
    //   var cancellationToken = CancellationToken.None;
    //   var clientService = new ClientService();
    //   var id = Guid.NewGuid();

    //   _mediator.Send(Arg.Any<UpdateResource<ClientService>>(), cancellationToken)
    //     .Returns(1); // Simulating that one row was affected

    //   // Act
    //   var result = await _clientServices.Update(id, clientService, null, cancellationToken);

    //   // Assert
    //   Assert.IsTrue(result);
    // }

    [TestMethod]
    public async Task Delete_ShouldReturnTrue_WhenRowsAffected()
    {
      // Arrange
      var cancellationToken = CancellationToken.None;
      var id = Guid.NewGuid();

      _mediator.Send(Arg.Any<DeleteResource<ClientService>>(), cancellationToken)
        .Returns(1); // Simulating that one row was affected

      // Act
      var result = await _clientServices.Delete(id, cancellationToken);

      // Assert
      Assert.IsTrue(result);
    }
  }
}
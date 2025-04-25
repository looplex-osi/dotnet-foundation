using System.Security.Claims;

using Looplex.Foundation.Ports;
using Looplex.Foundation.SCIMv2.Commands;
using Looplex.Foundation.SCIMv2.Entities;
using Looplex.Foundation.SCIMv2.Queries;
using Looplex.OpenForExtension.Abstractions.Plugins;

using MediatR;

using Microsoft.AspNetCore.Http;

using NSubstitute;

namespace Looplex.Foundation.UnitTests.SCIMv2.Entities
{
  [TestClass]
  public class GroupsTests
  {
    private Groups _groups = null!;
    private IRbacService _rbacService = null!;
    private IHttpContextAccessor _httpContextAccessor = null!;
    private IMediator _mediator = null!;
    private List<IPlugin> _plugins = null!;
    private ClaimsPrincipal _user = null!;

    [TestInitialize]
    public void Setup()
    {
      _rbacService = Substitute.For<IRbacService>();
      _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
      _mediator = Substitute.For<IMediator>();
      _plugins = new List<IPlugin>();
      _user = new ClaimsPrincipal();

      var httpContext = Substitute.For<HttpContext>();
      httpContext.User.Returns(_user);
      _httpContextAccessor.HttpContext.Returns(httpContext);

      _groups = new Groups(_plugins, _rbacService, _httpContextAccessor, _mediator);
    }

    [TestMethod]
    public async Task Query_ShouldReturnListResponse()
    {
      // Arrange
      var cancellationToken = CancellationToken.None;
      var expectedGroups = new List<Group> { new Group() };
      int expectedTotal = 1;

      _mediator.Send(Arg.Any<QueryResource<Group>>(), cancellationToken)
        .Returns((expectedGroups, expectedTotal));

      // Act
      var response = await _groups.Query(1, 10, "filter", "name", "asc", cancellationToken);

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
      var group = new Group();
      var expectedId = Guid.NewGuid();

      _mediator.Send(Arg.Any<CreateResource<Group>>(), cancellationToken)
        .Returns(expectedId);

      // Act
      var result = await _groups.Create(group, cancellationToken);

      // Assert
      Assert.AreEqual(expectedId, result);
    }

    [TestMethod]
    public async Task Retrieve_ShouldReturnGroup()
    {
      // Arrange
      var cancellationToken = CancellationToken.None;
      var expectedGroup = new Group();
      var id = Guid.NewGuid();

      _mediator.Send(Arg.Any<RetrieveResource<Group>>(), cancellationToken)
        .Returns(expectedGroup);

      // Act
      var result = await _groups.Retrieve(id, cancellationToken);

      // Assert
      Assert.IsNotNull(result);
      Assert.AreEqual(expectedGroup, result);
    }

    [TestMethod]
    public async Task Update_ShouldReturnTrue_WhenRowsAffected()
    {
      // Arrange
      var cancellationToken = CancellationToken.None;
      var group = new Group();
      var id = Guid.NewGuid();

      _mediator.Send(Arg.Any<UpdateResource<Group>>(), cancellationToken)
        .Returns(1); // Simulating that one row was affected

      // Act
      var result = await _groups.Update(id, group, null, cancellationToken);

      // Assert
      Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task Delete_ShouldReturnTrue_WhenRowsAffected()
    {
      // Arrange
      var cancellationToken = CancellationToken.None;
      var id = Guid.NewGuid();

      _mediator.Send(Arg.Any<DeleteResource<Group>>(), cancellationToken)
        .Returns(1); // Simulating that one row was affected

      // Act
      var result = await _groups.Delete(id, cancellationToken);

      // Assert
      Assert.IsTrue(result);
    }
  }
}
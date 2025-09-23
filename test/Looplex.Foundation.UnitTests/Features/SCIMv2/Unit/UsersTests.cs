using System.Security.Claims;

using Looplex.Foundation.Ports;
using Looplex.Foundation.SCIMv2.Commands;
using Looplex.Foundation.SCIMv2.Entities;
using Looplex.Foundation.SCIMv2.Modules;
using Looplex.Foundation.SCIMv2.Queries;
using Looplex.OpenForExtension.Abstractions.Plugins;

using MediatR;

using Microsoft.AspNetCore.Http;

using NSubstitute;

namespace Looplex.Foundation.UnitTests.SCIMv2.Entities
{
  [TestClass]
  public class UsersTests
  {
    private UserService _users = null!;
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

      // Create mock repository for UserService
      var mockRepository = Substitute.For<IResourceRepository<User>>();
      _users = new UserService(mockRepository);
    }

    [TestMethod]
    public async Task Query_ShouldReturnListResponse()
    {
      // Arrange
      var cancellationToken = CancellationToken.None;
      var expectedUsers = new List<User> { new User() };
      int expectedTotal = 1;

      // Configure mock repository to return expected data
      var mockRepository = Substitute.For<IResourceRepository<User>>();
      mockRepository.QueryAsync(1, 10, "filter", "name", "asc", cancellationToken)
        .Returns(new ListResponse<User> { Resources = expectedUsers, TotalResults = expectedTotal });
      
      // Recreate UserService with configured mock
      _users = new UserService(mockRepository);

      // Act
      var response = await _users.Query(1, 10, "filter", "name", "asc", cancellationToken);

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
      var user = new User();
      var expectedId = Guid.NewGuid();

      // Configure mock repository to return expected ID
      var mockRepository = Substitute.For<IResourceRepository<User>>();
      mockRepository.CreateAsync(user, cancellationToken)
        .Returns(expectedId);
      
      // Recreate UserService with configured mock
      _users = new UserService(mockRepository);

      // Act
      var result = await _users.Create(user, cancellationToken);

      // Assert
      Assert.AreEqual(expectedId, result);
    }

    [TestMethod]
    public async Task Retrieve_ShouldReturnUser()
    {
      // Arrange
      var cancellationToken = CancellationToken.None;
      var expectedUser = new User();
      var id = Guid.NewGuid();

      // Configure mock repository to return expected user
      var mockRepository = Substitute.For<IResourceRepository<User>>();
      mockRepository.RetrieveAsync(id, cancellationToken)
        .Returns(expectedUser);
      
      // Recreate UserService with configured mock
      _users = new UserService(mockRepository);

      // Act
      var result = await _users.Retrieve(id, cancellationToken);

      // Assert
      Assert.IsNotNull(result);
      Assert.AreEqual(expectedUser, result);
    }

    // TODO: Fix tests to match correct semantic
    // [TestMethod]
    // public async Task Update_ShouldReturnTrue_WhenRowsAffected()
    // {
    //   // Arrange
    //   var cancellationToken = CancellationToken.None;
    //   var user = new User();
    //   var id = Guid.NewGuid();

    //   _mediator.Send(Arg.Any<UpdateResource<User>>(), cancellationToken)
    //     .Returns(1); // Simulating that one row was affected

    //   // Act
    //   var result = await _users.Update(id, user, null, cancellationToken);

    //   // Assert
    //   Assert.IsTrue(result);
    // }

    [TestMethod]
    public async Task Delete_ShouldReturnTrue_WhenRowsAffected()
    {
      // Arrange
      var cancellationToken = CancellationToken.None;
      var id = Guid.NewGuid();

      // Configure mock repository to return success
      var mockRepository = Substitute.For<IResourceRepository<User>>();
      mockRepository.DeleteAsync(id, cancellationToken)
        .Returns(true);
      
      // Recreate UserService with configured mock
      _users = new UserService(mockRepository);

      // Act
      var result = await _users.Delete(id, cancellationToken);

      // Assert
      Assert.IsTrue(result);
    }
  }
}
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
    private IResourceRepository<User> _mockRepository = null!;

    [TestInitialize]
    public void Setup()
    {
      // Create mock repository for UserService
      _mockRepository = Substitute.For<IResourceRepository<User>>();
      _users = new UserService(_mockRepository);
    }

    [TestMethod]
    public async Task Query_ShouldReturnListResponse()
    {
      // Arrange
      var cancellationToken = CancellationToken.None;
      var expectedUsers = new List<User> { new User() };
      int expectedTotal = 1;

      // Configure mock repository to return expected data
      _mockRepository.QueryAsync(1, 10, "filter", cancellationToken)
        .Returns((expectedUsers, expectedTotal));

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
      var expectedUser = new User { Id = Guid.NewGuid().ToString() };

      // Configure mock repository to return expected user
      _mockRepository.CreateAsync(user, cancellationToken)
        .Returns(expectedUser);

      // Act
      var result = await _users.Create(user, cancellationToken);

      // Assert
      Assert.AreEqual(Guid.Parse(expectedUser.Id), result);
    }

    [TestMethod]
    public async Task Retrieve_ShouldReturnUser()
    {
      // Arrange
      var cancellationToken = CancellationToken.None;
      var expectedUser = new User();
      var id = Guid.NewGuid().ToString();

      // Configure mock repository to return expected user
      _mockRepository.GetByIdAsync(id, cancellationToken)
        .Returns(expectedUser);

      // Act
      var result = await _users.Retrieve(Guid.Parse(id), cancellationToken);

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
      var id = Guid.NewGuid().ToString();

      // Configure mock repository to return success
      _mockRepository.DeleteAsync(id, cancellationToken)
        .Returns(true);

      // Act
      var result = await _users.Delete(Guid.Parse(id), cancellationToken);

      // Assert
      Assert.IsTrue(result);
    }
  }
}
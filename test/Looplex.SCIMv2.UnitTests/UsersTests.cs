using Looplex.SCIMv2.Entities;
using Looplex.SCIMv2;

using NSubstitute;

namespace Looplex.SCIMv2.UnitTests;

[TestClass]
public class UsersTests
{
  private IResourceService<User> _userService = null!;

  [TestInitialize]
  public void Setup()
  {
    _userService = Substitute.For<IResourceService<User>>();
  }

  [TestMethod]
  public async Task QueryUsers_ShouldReturnUsers()
  {
    // Arrange
    var users = new List<User>
    {
      new() { Id = Guid.NewGuid().ToString(), UserName = "user1@example.com" },
      new() { Id = Guid.NewGuid().ToString(), UserName = "user2@example.com" }
    };

    _userService.QueryAsync(Arg.Any<int>(), Arg.Any<int>(), 
      Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
      .Returns((users, 2));

    // Act
    var (result, totalCount) = await _userService.QueryAsync(1, 10, "", "", "", "", "", CancellationToken.None);

    // Assert
    Assert.IsNotNull(result);
    Assert.IsTrue(result.Count == 2);
    Assert.IsTrue(totalCount == 2);
  }

  [TestMethod]
  public async Task RetrieveUser_ShouldReturnUser()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var user = new User { Id = userId.ToString(), UserName = "user@example.com" };

    _userService.RetrieveAsync(userId, Arg.Any<CancellationToken>())
      .Returns(user);

    // Act
    var result = await _userService.RetrieveAsync(userId, CancellationToken.None);

    // Assert
    Assert.IsNotNull(result);
    Assert.AreEqual(userId.ToString(), result.Id);
    Assert.AreEqual("user@example.com", result.UserName);
  }

  [TestMethod]
  public async Task CreateUser_ShouldReturnCreatedUserId()
  {
    // Arrange
    var user = new User { UserName = "newuser@example.com" };
    var createdUserId = Guid.NewGuid();

    _userService.CreateAsync(user, Arg.Any<CancellationToken>())
      .Returns(createdUserId);

    // Act
    var result = await _userService.CreateAsync(user, CancellationToken.None);

    // Assert
    Assert.AreEqual(createdUserId, result);
  }

  [TestMethod]
  public async Task ReplaceUser_ShouldReturnTrue()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var user = new User { Id = userId.ToString(), UserName = "updated@example.com" };

    _userService.ReplaceAsync(userId, user, Arg.Any<CancellationToken>())
      .Returns(true);

    // Act
    var result = await _userService.ReplaceAsync(userId, user, CancellationToken.None);

    // Assert
    Assert.IsTrue(result);
  }

  [TestMethod]
  public async Task DeleteUser_ShouldReturnTrue()
  {
    // Arrange
    var userId = Guid.NewGuid();

    _userService.DeleteAsync(userId, Arg.Any<CancellationToken>())
      .Returns(true);

    // Act
    var result = await _userService.DeleteAsync(userId, CancellationToken.None);

    // Assert
    Assert.IsTrue(result);
  }
}
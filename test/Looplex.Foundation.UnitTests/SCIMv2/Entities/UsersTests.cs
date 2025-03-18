using System.Data.Common;
using System.Security.Claims;

using Looplex.Foundation.Ports;
using Looplex.Foundation.SCIMv2.Entities;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Looplex.OpenForExtension.Abstractions.Plugins;

using Microsoft.AspNetCore.Http;

using NSubstitute;

namespace Looplex.Foundation.UnitTests.SCIMv2.Entities;

[TestClass]
public class SCIMv2Tests
{
  private Users _users = null!;
  private IRbacService _mockRbacService = null!;
  private ClaimsPrincipal _mockUser = null!;
  private DbConnection _mockDbConnection = null!;
  private DbCommand _mockDbCommand = null!;
  private DbDataReader _mockDbDataReader = null!;
  private IContext _mockContext = null!;
  private CancellationToken _cancellationToken;

  [TestInitialize]
  public void Setup()
  {
    _mockRbacService = Substitute.For<IRbacService>();
    _mockUser = Substitute.For<ClaimsPrincipal>();
    var mockHttpAccessor = Substitute.For<IHttpContextAccessor>();
    var httpContext = new DefaultHttpContext() { User = _mockUser };
    mockHttpAccessor.HttpContext.Returns(httpContext);
    _mockDbConnection = Substitute.For<DbConnection>();
    _mockDbCommand = Substitute.For<DbCommand>();
    _mockDbDataReader = Substitute.For<DbDataReader>();
    _mockContext = Substitute.For<IContext>();

    _mockDbConnection.CreateCommand().Returns(_mockDbCommand);
    _mockDbCommand.ExecuteReaderAsync(Arg.Any<CancellationToken>())
      .Returns(Task.FromResult((DbDataReader)_mockDbDataReader));
    _mockDbCommand.ExecuteScalarAsync(Arg.Any<CancellationToken>())!.Returns(Task.FromResult<object>(Guid.NewGuid()));
    _mockDbCommand.ExecuteNonQueryAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

    _cancellationToken = CancellationToken.None;
    _users = new Users(new List<IPlugin>(), _mockRbacService, mockHttpAccessor, _mockDbConnection);
  }

  #region QueryAsync Tests

  [TestMethod]
  public async Task QueryAsync_ValidRequest_ReturnsListResponse()
  {
    // Arrange
    _mockDbDataReader.ReadAsync().Returns(Task.FromResult(true), Task.FromResult(false));
    _mockDbDataReader["UserName"].Returns("TestUser");

    // Act
    var result = await _users.Query(1, 10, Arg.Any<string?>(), Arg.Any<string?>(), "name=test",
      _cancellationToken);

    // Assert
    Assert.IsNotNull(result);
    Assert.AreEqual(1, result.StartIndex);
    Assert.AreEqual(10, result.ItemsPerPage);
  }

  [TestMethod]
  public async Task QueryAsync_NullFilter_ThrowsArgumentNullException()
  {
    await Assert.ThrowsExceptionAsync<ArgumentNullException>(
      () => _users.Query(1, 10, null, Arg.Any<string?>(), Arg.Any<string?>(), _cancellationToken));
  }

  #endregion

  #region CreateAsync Tests

  [TestMethod]
  public async Task CreateAsync_ValidResource_ReturnsGuid()
  {
    // Arrange
    var user = new User { UserName = "TestUser" };

    // Act
    var result = await _users.Create(user, _cancellationToken);

    // Assert
    Assert.IsInstanceOfType(result, typeof(Guid));
  }

  [TestMethod]
  public async Task CreateAsync_MissingUserName_ThrowsException()
  {
    // Arrange
    var user = new User(); // UserName is null

    // Act & Assert
    await Assert.ThrowsExceptionAsync<Exception>(() => _users.Create(user, _cancellationToken));
  }

  #endregion

  #region RetrieveAsync Tests

  [TestMethod]
  public async Task RetrieveAsync_ValidId_ReturnsUser()
  {
    // Arrange
    Guid userId = Guid.NewGuid();
    _mockDbDataReader.ReadAsync().Returns(Task.FromResult(true));
    _mockDbDataReader["UserName"].Returns("TestUser");

    // Act
    var result = await _users.Retrieve(userId, _cancellationToken);

    // Assert
    Assert.IsNotNull(result);
    Assert.AreEqual("TestUser", result.UserName);
  }

  [TestMethod]
  public async Task RetrieveAsync_NonExistingId_ReturnsNull()
  {
    // Arrange
    Guid userId = Guid.NewGuid();
    _mockDbDataReader.ReadAsync().Returns(Task.FromResult(false));

    // Act
    var result = await _users.Retrieve(userId, _cancellationToken);

    // Assert
    Assert.IsNull(result);
  }

  #endregion

  #region UpdateAsync Tests

  [TestMethod]
  public async Task UpdateAsync_ValidIdAndResource_ReturnsTrue()
  {
    // Arrange
    Guid userId = Guid.NewGuid();
    var user = new User { UserName = "UpdatedUser" };

    // Act
    var result = await _users.Update(userId, user, null, _cancellationToken);

    // Assert
    Assert.IsTrue(result);
  }

  [TestMethod]
  public async Task UpdateAsync_InvalidId_ReturnsFalse()
  {
    // Arrange
    Guid userId = Guid.NewGuid();
    var user = new User { UserName = "UpdatedUser" };
    _mockDbCommand.ExecuteNonQueryAsync(_cancellationToken).Returns(Task.FromResult(0)); // Simulate no update

    // Act
    var result = await _users.Update(userId, user, null, _cancellationToken);

    // Assert
    Assert.IsFalse(result);
  }

  #endregion

  #region DeleteAsync Tests

  [TestMethod]
  public async Task DeleteAsync_ValidId_ReturnsTrue()
  {
    // Arrange
    Guid userId = Guid.NewGuid();

    // Act
    var result = await _users.Delete(userId, _cancellationToken);

    // Assert
    Assert.IsTrue(result);
  }

  [TestMethod]
  public async Task DeleteAsync_InvalidId_ReturnsFalse()
  {
    // Arrange
    Guid userId = Guid.NewGuid();
    _mockDbCommand.ExecuteNonQueryAsync(_cancellationToken).Returns(Task.FromResult(0)); // Simulate no deletion

    // Act
    var result = await _users.Delete(userId, _cancellationToken);

    // Assert
    Assert.IsFalse(result);
  }

  #endregion
}
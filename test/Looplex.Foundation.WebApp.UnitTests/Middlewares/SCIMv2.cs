using System.Net;
using System.Text;
using System.Text.Json;

using Looplex.Foundation.SCIMv2.Entities;
using Looplex.Foundation.WebApp.Middlewares;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NSubstitute;


namespace Looplex.Foundation.WebApp.UnitTests.Middlewares;

[TestClass]
public class SCIMv2Tests
{
  private HttpClient _client = null!;
  private IHost _host = null!;
  private Users _users = null!;

  [TestInitialize]
  public Task Setup()
  {
    _users = Substitute.For<Users>();

    _host = Host.CreateDefaultBuilder()
      .ConfigureWebHostDefaults(webBuilder =>
      {
        webBuilder.UseTestServer();
        webBuilder.ConfigureServices(services =>
        {
          services.AddRouting();
          services.AddSingleton(_users);
          services.AddSingleton<ServiceProviderConfiguration>();
        });
        webBuilder.Configure(app =>
        {
          app.UseRouting();
          app.UseEndpoints(endpoints =>
          {
            endpoints.UseSCIMv2<User, Users>("/users", authorize: false);
          });
        });
      })
      .Start();

    _client = _host.GetTestClient();
    return Task.CompletedTask;
  }

  [TestCleanup]
  public async Task Cleanup()
  {
    _client.Dispose();
    await _host.StopAsync();
    _host.Dispose();
  }

  #region Create Tests

  [TestMethod]
  public async Task CreateUser_ValidRequest_ReturnsCreated()
  {
    // Arrange
    User user = new() { UserName = "TestUser" };
    _users
      .Create(Arg.Any<User>(), Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(Guid.NewGuid()));

    StringContent content = new(JsonSerializer.Serialize(user), Encoding.UTF8, "application/json");

    // Act
    HttpResponseMessage response = await _client.PostAsync("/users", content);

    // Assert
    Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
    Assert.IsTrue(response.Headers.Location != null);
  }

  #endregion

  #region Query Tests

  [TestMethod]
  public async Task QueryUsers_ValidRequest_ReturnsOk()
  {
    // Arrange
    _users.Query(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
        Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(new ListResponse<User>()));

    // Act
    HttpResponseMessage response = await _client.GetAsync("/users?page=1&pageSize=10");

    // Assert
    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
  }

  #endregion

  #region Retrieve Tests

  [TestMethod]
  public async Task RetrieveUser_NotFound_ReturnsNotFound()
  {
    // Arrange
    _users.Retrieve(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
      .Returns(Task.FromResult<User?>(null));

    // Act
    HttpResponseMessage response = await _client.GetAsync("/users/" + Guid.NewGuid());

    // Assert
    Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
  }

  [TestMethod]
  public async Task RetrieveUser_Found_ReturnsOk()
  {
    // Arrange
    User user = new() { UserName = "ExistingUser" };
    _users.Retrieve(Arg.Any<Guid>(), Arg.Any<CancellationToken>())!
      .Returns(Task.FromResult(user));

    // Act
    HttpResponseMessage response = await _client.GetAsync("/users/" + Guid.NewGuid());

    // Assert
    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
  }

  #endregion

  #region Update Tests

  [TestMethod]
  public async Task UpdateUser_NotFound_ReturnsNotFound()
  {
    // Arrange
    _users.Retrieve(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
      .Returns(Task.FromResult<User?>(null));

    // Act
    HttpResponseMessage response = await _client.PatchAsync("/users/" + Guid.NewGuid(), null);

    // Assert
    Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
  }

  [TestMethod]
  public async Task UpdateUser_Found_ReturnsNoContent()
  {
    // Arrange
    User user = new() { UserName = "ExistingUser" };
    _users.Retrieve(Arg.Any<Guid>(), Arg.Any<CancellationToken>())!
      .Returns(Task.FromResult(user));
    _users.Update(Arg.Any<Guid>(), Arg.Any<User>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(true));

    // Act
    HttpResponseMessage response = await _client.PatchAsync("/users/" + Guid.NewGuid(), null);

    // Assert
    Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
  }

  [TestMethod]
  public async Task UpdateUser_Found_Fails_ReturnsInternalServerError()
  {
    // Arrange
    User user = new() { UserName = "ExistingUser" };
    _users.Retrieve(Arg.Any<Guid>(), Arg.Any<CancellationToken>())!
      .Returns(Task.FromResult(user));
    _users.Update(Arg.Any<Guid>(), Arg.Any<User>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(false));

    // Act
    HttpResponseMessage response = await _client.PatchAsync("/users/" + Guid.NewGuid(), null);

    // Assert
    Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
  }

  #endregion

  #region Delete Tests

  [TestMethod]
  public async Task DeleteUser_NotFound_ReturnsNotFound()
  {
    // Arrange
    _users.Delete(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(false));

    // Act
    HttpResponseMessage response = await _client.DeleteAsync("/users/" + Guid.NewGuid());

    // Assert
    Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
  }

  [TestMethod]
  public async Task DeleteUser_Found_ReturnsNoContent()
  {
    // Arrange
    _users.Delete(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(true));

    // Act
    HttpResponseMessage response = await _client.DeleteAsync("/users/" + Guid.NewGuid());

    // Assert
    Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
  }

  #endregion
}
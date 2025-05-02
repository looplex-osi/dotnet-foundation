using System.Net;
using System.Text;
using System.Text.Json;

using Looplex.Foundation.OAuth2.Entities;
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
  private Groups _groups = null!;
  private ClientServices _clientServices = null!;

  [TestInitialize]
  public Task Setup()
  {
    _users = Substitute.For<Users>();
    _groups = Substitute.For<Groups>();
    _clientServices = Substitute.For<ClientServices>();

    _host = Host.CreateDefaultBuilder()
      .ConfigureWebHostDefaults(webBuilder =>
      {
        webBuilder.UseTestServer();
        webBuilder.ConfigureServices(services =>
        {
          services.AddRouting();
          services.AddSingleton(_users);
          services.AddSingleton(_groups);
          services.AddSingleton(_clientServices);
          services.AddSingleton<ServiceProviderConfiguration>();
        });
        webBuilder.Configure(app =>
        {
          app.UseRouting();
          app.UseEndpoints(endpoints =>
          {
            endpoints.UseSCIMv2<User, Users>("/Users", authorize: false);
            endpoints.UseSCIMv2<Group, Groups>("/Groups", authorize: false);
            endpoints.UseSCIMv2<ClientService, ClientServices>("/Api-Keys", authorize: false);
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

  #region Users

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

  #endregion

  #region Groups

  #region Create Tests

  [TestMethod]
  public async Task CreateGroup_ValidRequest_ReturnsCreated()
  {
    // Arrange
    Group group = new() { DisplayName = "TestGroup" };
    _groups
      .Create(Arg.Any<Group>(), Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(Guid.NewGuid()));

    StringContent content = new(JsonSerializer.Serialize(group), Encoding.UTF8, "application/json");

    // Act
    HttpResponseMessage response = await _client.PostAsync("/Groups", content);

    // Assert
    Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
    Assert.IsTrue(response.Headers.Location != null);
  }

  #endregion

  #region Query Tests

  [TestMethod]
  public async Task QueryGroups_ValidRequest_ReturnsOk()
  {
    // Arrange
    _groups.Query(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
        Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(new ListResponse<Group>()));

    // Act
    HttpResponseMessage response = await _client.GetAsync("/Groups?page=1&pageSize=10");

    // Assert
    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
  }

  #endregion

  #region Retrieve Tests

  [TestMethod]
  public async Task RetrieveGroup_NotFound_ReturnsNotFound()
  {
    // Arrange
    _groups.Retrieve(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
      .Returns(Task.FromResult<Group?>(null));

    // Act
    HttpResponseMessage response = await _client.GetAsync("/Groups/" + Guid.NewGuid());

    // Assert
    Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
  }

  [TestMethod]
  public async Task RetrieveGroup_Found_ReturnsOk()
  {
    // Arrange
    Group group = new() { DisplayName = "ExistingGroup" };
    _groups.Retrieve(Arg.Any<Guid>(), Arg.Any<CancellationToken>())!
      .Returns(Task.FromResult(group));

    // Act
    HttpResponseMessage response = await _client.GetAsync("/Groups/" + Guid.NewGuid());

    // Assert
    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
  }

  #endregion

  #region Update Tests

  [TestMethod]
  public async Task UpdateGroup_NotFound_ReturnsNotFound()
  {
    // Arrange
    _groups.Retrieve(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
      .Returns(Task.FromResult<Group?>(null));

    // Act
    HttpResponseMessage response = await _client.PatchAsync("/Groups/" + Guid.NewGuid(), null);

    // Assert
    Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
  }

  [TestMethod]
  public async Task UpdateGroup_Found_ReturnsNoContent()
  {
    // Arrange
    Group group = new() { DisplayName = "ExistingGroup" };
    _groups.Retrieve(Arg.Any<Guid>(), Arg.Any<CancellationToken>())!
      .Returns(Task.FromResult(group));
    _groups.Update(Arg.Any<Guid>(), Arg.Any<Group>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(true));

    // Act
    HttpResponseMessage response = await _client.PatchAsync("/Groups/" + Guid.NewGuid(), null);

    // Assert
    Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
  }

  [TestMethod]
  public async Task UpdateGroup_Found_Fails_ReturnsInternalServerError()
  {
    // Arrange
    Group group = new() { DisplayName = "ExistingGroup" };
    _groups.Retrieve(Arg.Any<Guid>(), Arg.Any<CancellationToken>())!
      .Returns(Task.FromResult(group));
    _groups.Update(Arg.Any<Guid>(), Arg.Any<Group>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(false));

    // Act
    HttpResponseMessage response = await _client.PatchAsync("/Groups/" + Guid.NewGuid(), null);

    // Assert
    Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
  }

  #endregion

  #region Delete Tests

  [TestMethod]
  public async Task DeleteGroup_NotFound_ReturnsNotFound()
  {
    // Arrange
    _groups.Delete(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(false));

    // Act
    HttpResponseMessage response = await _client.DeleteAsync("/Groups/" + Guid.NewGuid());

    // Assert
    Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
  }

  [TestMethod]
  public async Task DeleteGroup_Found_ReturnsNoContent()
  {
    // Arrange
    _groups.Delete(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(true));

    // Act
    HttpResponseMessage response = await _client.DeleteAsync("/Groups/" + Guid.NewGuid());

    // Assert
    Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
  }

  #endregion

  #endregion

  #region ClientServices

  #region Create Tests

  [TestMethod]
  public async Task CreateClientCredential_ValidRequest_ReturnsCreated()
  {
    // Arrange
    ClientService clientService = new() { Digest = "TestClientCredential" };
    _clientServices
      .Create(Arg.Any<ClientService>(), Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(Guid.NewGuid()));

    StringContent content = new(JsonSerializer.Serialize(clientService), Encoding.UTF8, "application/json");

    // Act
    HttpResponseMessage response = await _client.PostAsync("/Api-Keys", content);

    // Assert
    Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
    Assert.IsTrue(response.Headers.Location != null);
  }

  #endregion

  #region Query Tests

  [TestMethod]
  public async Task QueryClientCredentials_ValidRequest_ReturnsOk()
  {
    // Arrange
    _clientServices.Query(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(),
        Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(new ListResponse<ClientService>()));

    // Act
    HttpResponseMessage response = await _client.GetAsync("/Api-Keys?page=1&pageSize=10");

    // Assert
    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
  }

  #endregion

  #region Retrieve Tests

  [TestMethod]
  public async Task RetrieveClientCredential_NotFound_ReturnsNotFound()
  {
    // Arrange
    _clientServices.Retrieve(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
      .Returns(Task.FromResult<ClientService?>(null));

    // Act
    HttpResponseMessage response = await _client.GetAsync("/Api-Keys/" + Guid.NewGuid());

    // Assert
    Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
  }

  [TestMethod]
  public async Task RetrieveClientCredential_Found_ReturnsOk()
  {
    // Arrange
    ClientService clientService = new() { Digest = "ExistingClientCredential" };
    _clientServices.Retrieve(Arg.Any<Guid>(), Arg.Any<CancellationToken>())!
      .Returns(Task.FromResult(clientService));

    // Act
    HttpResponseMessage response = await _client.GetAsync("/Api-Keys/" + Guid.NewGuid());

    // Assert
    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
  }

  #endregion

  #region Update Tests

  [TestMethod]
  public async Task UpdateClientCredential_NotFound_ReturnsNotFound()
  {
    // Arrange
    _clientServices.Retrieve(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
      .Returns(Task.FromResult<ClientService?>(null));

    // Act
    HttpResponseMessage response = await _client.PatchAsync("/Api-Keys/" + Guid.NewGuid(), null);

    // Assert
    Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
  }

  [TestMethod]
  public async Task UpdateClientCredential_Found_ReturnsNoContent()
  {
    // Arrange
    ClientService clientService = new() { Digest = "ExistingClientCredential" };
    _clientServices.Retrieve(Arg.Any<Guid>(), Arg.Any<CancellationToken>())!
      .Returns(Task.FromResult(clientService));
    _clientServices.Update(Arg.Any<Guid>(), Arg.Any<ClientService>(), Arg.Any<string?>(),
        Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(true));

    // Act
    HttpResponseMessage response = await _client.PatchAsync("/Api-Keys/" + Guid.NewGuid(), null);

    // Assert
    Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
  }

  [TestMethod]
  public async Task UpdateClientCredential_Found_Fails_ReturnsInternalServerError()
  {
    // Arrange
    ClientService clientService = new() { Digest = "ExistingClientCredential" };
    _clientServices.Retrieve(Arg.Any<Guid>(), Arg.Any<CancellationToken>())!
      .Returns(Task.FromResult(clientService));
    _clientServices.Update(Arg.Any<Guid>(), Arg.Any<ClientService>(), Arg.Any<string?>(),
        Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(false));

    // Act
    HttpResponseMessage response = await _client.PatchAsync("/Api-Keys/" + Guid.NewGuid(), null);

    // Assert
    Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
  }

  #endregion

  #region Delete Tests

  [TestMethod]
  public async Task DeleteClientCredential_NotFound_ReturnsNotFound()
  {
    // Arrange
    _clientServices.Delete(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(false));

    // Act
    HttpResponseMessage response = await _client.DeleteAsync("/Api-Keys/" + Guid.NewGuid());

    // Assert
    Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
  }

  [TestMethod]
  public async Task DeleteClientCredential_Found_ReturnsNoContent()
  {
    // Arrange
    _clientServices.Delete(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(true));

    // Act
    HttpResponseMessage response = await _client.DeleteAsync("/Api-Keys/" + Guid.NewGuid());

    // Assert
    Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
  }

  #endregion

  #endregion
}
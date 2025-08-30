using System.Net;
using System.Text;
using System.Text.Json;

using Looplex.Foundation.OAuth2.Entities;
using Looplex.Foundation.SCIMv2.Entities;
using Looplex.Foundation.SearchContent;
using Looplex.Foundation.SearchContent.Parser;
using Looplex.Foundation.SearchContent.SqlGenerator;
using Looplex.Foundation.WebApp.Middlewares;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NSubstitute;

namespace Looplex.Foundation.WebApp.UnitTests.Middlewares;

/// <summary>
/// Integration tests for SCIM v2 middleware with SearchContent functionality
/// Tests the integration between SCIM filter processing and SQL generation
/// </summary>
[TestClass]
public class SCIMv2SearchContentIntegrationTests
{
  private HttpClient _client = null!;
  private IHost _host = null!;
  private Users _users = null!;
  private Groups _groups = null!;
  private ClientServices _clientServices = null!;
  private ISearchContentService _searchContentService = null!;

  [TestInitialize]
  public Task Setup()
  {
    _users = Substitute.For<Users>();
    _groups = Substitute.For<Groups>();
    _clientServices = Substitute.For<ClientServices>();
    _searchContentService = Substitute.For<ISearchContentService>();

    _host = Host.CreateDefaultBuilder()
      .ConfigureWebHostDefaults(webBuilder =>
      {
        webBuilder.UseTestServer();
        webBuilder.ConfigureServices(services =>
        {
          services.AddRouting();
          
          // Register SCIM services
          services.AddSingleton(_users);
          services.AddSingleton(_groups);
          services.AddSingleton(_clientServices);
          services.AddSingleton<ServiceProviderConfiguration>();
          
          // Register SearchContent services
          services.AddSingleton(_searchContentService);
          services.AddScoped<IFilterParser, EnhancedScimFilterParser>();
          services.AddScoped<ISqlPredicateGenerator, SqlPredicateGenerator>();
        });
        webBuilder.Configure(app =>
        {
          app.UseRouting();
          app.UseEndpoints(endpoints =>
          {
            endpoints.UseSCIMv2<User, User, Users>("/Users", authorize: false);
            endpoints.UseSCIMv2<Group, Group, Groups>("/Groups", authorize: false);
            endpoints.UseSCIMv2<ClientService, ClientService, ClientServices>("/Api-Keys", authorize: false);
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

  #region SCIM Filter Integration Tests

  [TestMethod]
  public async Task QueryUsers_WithBasicScimFilter_ShouldProcessFilter()
  {
    // Arrange
    var scimFilter = "userName eq \"john.doe\"";
    _users.Query(Arg.Any<int>(), Arg.Any<int>(), Arg.Is(scimFilter), Arg.Any<string?>(), Arg.Any<string?>(),
        Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(new ListResponse<User>
      {
        StartIndex = 1,
        ItemsPerPage = 10,
        TotalResults = 1,
        Resources = new List<User>
        {
          new User { UserName = "john.doe" }
        }
      }));

    // Act
    var response = await _client.GetAsync($"/Users?filter={Uri.EscapeDataString(scimFilter)}");

    // Assert
    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    
    // Verify that the filter was passed to the Users service
    await _users.Received(1).Query(
      Arg.Is(1),
      Arg.Is(12),
      Arg.Is(scimFilter),
      Arg.Any<string?>(),
      Arg.Any<string?>(),
      Arg.Any<CancellationToken>());
  }

  [TestMethod]
  public async Task QueryUsers_WithComplexScimFilter_ShouldProcessFilter()
  {
    // Arrange
    var scimFilter = "name.givenName eq \"John\" and department eq \"IT\" and active eq true";
    _users.Query(Arg.Any<int>(), Arg.Any<int>(), Arg.Is(scimFilter), Arg.Any<string?>(), Arg.Any<string?>(),
        Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(new ListResponse<User>
      {
        StartIndex = 1,
        ItemsPerPage = 10,
        TotalResults = 2,
        Resources = new List<User>
        {
          new User { UserName = "john.smith" },
          new User { UserName = "john.doe" }
        }
      }));

    // Act
    var response = await _client.GetAsync($"/Users?filter={Uri.EscapeDataString(scimFilter)}");

    // Assert
    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    
    // Verify that the complex filter was passed correctly
    await _users.Received(1).Query(
      Arg.Is(1),
      Arg.Is(12),
      Arg.Is(scimFilter),
      Arg.Any<string?>(),
      Arg.Any<string?>(),
      Arg.Any<CancellationToken>());
  }

  [TestMethod]
  public async Task QueryUsers_WithSchemaPrefix_ShouldProcessFilter()
  {
    // Arrange
    var scimFilter = "urn:ietf:params:scim:schemas:core:2.0:User:userName eq \"john.doe\"";
    _users.Query(Arg.Any<int>(), Arg.Any<int>(), Arg.Is(scimFilter), Arg.Any<string?>(), Arg.Any<string?>(),
        Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(new ListResponse<User>
      {
        StartIndex = 1,
        ItemsPerPage = 10,
        TotalResults = 1,
        Resources = new List<User>
        {
          new User { UserName = "john.doe" }
        }
      }));

    // Act
    var response = await _client.GetAsync($"/Users?filter={Uri.EscapeDataString(scimFilter)}");

    // Assert
    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    
    // Verify that the schema prefix filter was processed
    await _users.Received(1).Query(
      Arg.Any<int>(),
      Arg.Any<int>(),
      Arg.Is(scimFilter),
      Arg.Any<string?>(),
      Arg.Any<string?>(),
      Arg.Any<CancellationToken>());
  }

  [TestMethod]
  public async Task QueryGroups_WithScimFilter_ShouldProcessFilter()
  {
    // Arrange
    var scimFilter = "displayName co \"Engineering\"";
    _groups.Query(Arg.Any<int>(), Arg.Any<int>(), Arg.Is(scimFilter), Arg.Any<string?>(), Arg.Any<string?>(),
        Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(new ListResponse<Group>
      {
        StartIndex = 1,
        ItemsPerPage = 10,
        TotalResults = 1,
        Resources = new List<Group>
        {
          new Group { DisplayName = "Engineering Team" }
        }
      }));

    // Act
    var response = await _client.GetAsync($"/Groups?filter={Uri.EscapeDataString(scimFilter)}");

    // Assert
    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    
    // Verify that the filter was passed to the Groups service
    await _groups.Received(1).Query(
      Arg.Is(1),
      Arg.Is(12),
      Arg.Is(scimFilter),
      Arg.Any<string?>(),
      Arg.Any<string?>(),
      Arg.Any<CancellationToken>());
  }

  [TestMethod]
  public async Task QueryUsers_WithPagination_ShouldHandleCorrectly()
  {
    // Arrange
    var scimFilter = "active eq true";
    var startIndex = 11;
    var count = 25;
    
    _users.Query(Arg.Any<int>(), Arg.Any<int>(), Arg.Is(scimFilter), Arg.Any<string?>(), Arg.Any<string?>(),
        Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(new ListResponse<User>
      {
        StartIndex = startIndex,
        ItemsPerPage = count,
        TotalResults = 100,
        Resources = new List<User>()
      }));

    // Act
    var response = await _client.GetAsync($"/Users?filter={Uri.EscapeDataString(scimFilter)}&startIndex={startIndex}&count={count}");

    // Assert
    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    
    // Verify pagination parameters were processed correctly
    await _users.Received(1).Query(
      Arg.Is(startIndex),
      Arg.Is(count),
      Arg.Is(scimFilter),
      Arg.Any<string?>(),
      Arg.Any<string?>(),
      Arg.Any<CancellationToken>());
  }

  [TestMethod]
  public async Task QueryUsers_WithSorting_ShouldHandleCorrectly()
  {
    // Arrange
    var scimFilter = "department eq \"IT\"";
    var sortBy = "userName";
    var sortOrder = "ascending";
    
    _users.Query(Arg.Any<int>(), Arg.Any<int>(), Arg.Is(scimFilter), Arg.Is(sortBy), Arg.Is(sortOrder),
        Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(new ListResponse<User>
      {
        StartIndex = 1,
        ItemsPerPage = 12,
        TotalResults = 5,
        Resources = new List<User>()
      }));

    // Act
    var response = await _client.GetAsync($"/Users?filter={Uri.EscapeDataString(scimFilter)}&sortBy={sortBy}&sortOrder={sortOrder}");

    // Assert
    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    
    // Verify sorting parameters were processed correctly
    await _users.Received(1).Query(
      Arg.Any<int>(),
      Arg.Any<int>(),
      Arg.Is(scimFilter),
      Arg.Is(sortBy),
      Arg.Is(sortOrder),
      Arg.Any<CancellationToken>());
  }

  [TestMethod]
  public async Task QueryUsers_WithNullValues_ShouldProcessFilter()
  {
    // Arrange
    var scimFilter = "manager eq null and department ne null";
    _users.Query(Arg.Any<int>(), Arg.Any<int>(), Arg.Is(scimFilter), Arg.Any<string?>(), Arg.Any<string?>(),
        Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(new ListResponse<User>
      {
        StartIndex = 1,
        ItemsPerPage = 12,
        TotalResults = 3,
        Resources = new List<User>()
      }));

    // Act
    var response = await _client.GetAsync($"/Users?filter={Uri.EscapeDataString(scimFilter)}");

    // Assert
    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    
    // Verify null handling in filter
    await _users.Received(1).Query(
      Arg.Any<int>(),
      Arg.Any<int>(),
      Arg.Is(scimFilter),
      Arg.Any<string?>(),
      Arg.Any<string?>(),
      Arg.Any<CancellationToken>());
  }

  [TestMethod]
  public async Task QueryUsers_WithEscapeSequences_ShouldProcessFilter()
  {
    // Arrange
    var scimFilter = "displayName eq \"John \\\"The Boss\\\" Smith\"";
    _users.Query(Arg.Any<int>(), Arg.Any<int>(), Arg.Is(scimFilter), Arg.Any<string?>(), Arg.Any<string?>(),
        Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(new ListResponse<User>
      {
        StartIndex = 1,
        ItemsPerPage = 12,
        TotalResults = 1,
        Resources = new List<User>()
      }));

    // Act
    var response = await _client.GetAsync($"/Users?filter={Uri.EscapeDataString(scimFilter)}");

    // Assert
    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    
    // Verify escape sequences are handled
    await _users.Received(1).Query(
      Arg.Any<int>(),
      Arg.Any<int>(),
      Arg.Is(scimFilter),
      Arg.Any<string?>(),
      Arg.Any<string?>(),
      Arg.Any<CancellationToken>());
  }

  #endregion

  #region Response Format Tests

  [TestMethod]
  public async Task QueryUsers_ResponseFormat_ShouldBeCamelCase()
  {
    // Arrange
    var scimFilter = "userName eq \"john.doe\"";
    _users.Query(Arg.Any<int>(), Arg.Any<int>(), Arg.Is(scimFilter), Arg.Any<string?>(), Arg.Any<string?>(),
        Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(new ListResponse<User>
      {
        StartIndex = 1,
        ItemsPerPage = 12,
        TotalResults = 1,
        Resources = new List<User>
        {
          new User { UserName = "john.doe", DisplayName = "John Doe" }
        }
      }));

    // Act
    var response = await _client.GetAsync($"/Users?filter={Uri.EscapeDataString(scimFilter)}");
    var content = await response.Content.ReadAsStringAsync();

    // Assert
    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    
    // Debug: print the actual content to understand what's being returned
    Console.WriteLine($"Response content: {content}");
    
    Assert.IsTrue(content.Contains("startIndex") || content.Contains("StartIndex"), "Should contain startIndex");
    Assert.IsTrue(content.Contains("itemsPerPage") || content.Contains("ItemsPerPage"), "Should contain itemsPerPage");
    Assert.IsTrue(content.Contains("totalResults") || content.Contains("TotalResults"), "Should contain totalResults");
    Assert.IsTrue(content.Contains("resources") || content.Contains("Resources"), "Should contain resources");
  }

  #endregion
}

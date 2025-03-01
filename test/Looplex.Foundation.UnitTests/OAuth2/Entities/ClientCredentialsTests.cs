using System.Dynamic;
using System.Text;
using Looplex.Foundation.OAuth2;
using Looplex.Foundation.OAuth2.Dtos;
using Looplex.Foundation.OAuth2.Entities;
using Looplex.Foundation.Ports;
using Looplex.Foundation.SCIMv2.Entities;
using Looplex.Foundation.Serialization;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using NSubstitute;

namespace Looplex.Foundation.UnitTests.OAuth2.Entities;

[TestClass]
public class ClientCredentialsTests
{
    private IRbacService _mockRbacService = null!;
    private IConfiguration _mockConfiguration = null!;
    private IJsonSchemaProvider _mockJsonSchemaProvider = null!;
    private IUserContext _mockUserContext = null!;
    private CancellationToken _cancellationToken;

    [TestInitialize]
    public void SetUp()
    {
        ClientCredentials.Data.Clear();
        _mockRbacService = Substitute.For<IRbacService>();
        _mockConfiguration = Substitute.For<IConfiguration>();
        _mockConfiguration["ClientSecretByteLength"].Returns("72"); 
        _mockConfiguration["ClientSecretDigestCost"].Returns("4"); 
        _mockJsonSchemaProvider = Substitute.For<IJsonSchemaProvider>();
        _mockUserContext = Substitute.For<IUserContext>();
        _cancellationToken = new CancellationToken();
    }

    [TestMethod]
    public async Task QueryAsync_ShouldReturnPaginatedResults()
    {
        // Arrange
        var service = new ClientCredentials([], _mockRbacService, _mockConfiguration, _mockJsonSchemaProvider, _mockUserContext);
        ClientCredentials.Data.Add(new ClientCredential { Id = "1" });
            
        // Act
        var resultJson = await service.QueryAsync(1, 10, _cancellationToken);
        var result = JsonConvert.DeserializeObject<ListResponse<ClientCredential>>(resultJson);
            
        // Assert
        Assert.AreEqual(1, result!.TotalResults);
        Assert.AreEqual("1", result.Resources[0].Id);
    }

    [TestMethod]
    public async Task RetrieveAsync_ValidId_ShouldReturnClientCredential()
    {
        // Arrange
        var service = new ClientCredentials([], _mockRbacService, _mockConfiguration, _mockJsonSchemaProvider, _mockUserContext);
        var clientCredential = new ClientCredential { Id = "123" };
        ClientCredentials.Data.Add(clientCredential);
            
        // Act
        var resultJson = await service.RetrieveAsync("123", _cancellationToken);
        var result = JsonConvert.DeserializeObject<ClientCredential>(resultJson);
            
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("123", result.Id);
    }

    [TestMethod]
    public async Task CreateAsync_ShouldGenerateClientIdAndDigest()
    {
        // Arrange
        var service = new ClientCredentials([], _mockRbacService, _mockConfiguration, _mockJsonSchemaProvider, _mockUserContext);
        var inputJson = JsonConvert.SerializeObject(new ClientCredential { ClientName = "client-1" });

        // Act
        var result = await service.CreateAsync(inputJson, _cancellationToken);
        var clientCredentialDto = result.JsonDeserialize<ClientCredentialDto>();
        var createdCredential = ClientCredentials.Data.FirstOrDefault(c => c.ClientId == clientCredentialDto.ClientId);

        // Assert
        Assert.IsNotNull(createdCredential);
        Assert.IsFalse(clientCredentialDto.ClientId == Guid.Empty);
        Assert.IsFalse(string.IsNullOrEmpty(clientCredentialDto.ClientSecret));
        Assert.IsFalse(createdCredential.ClientId == Guid.Empty);
        Assert.IsFalse(string.IsNullOrEmpty(createdCredential.Digest));
    }

    [TestMethod]
    public async Task DeleteAsync_ValidId_ShouldRemoveCredential()
    {
        // Arrange
        var service = new ClientCredentials([], _mockRbacService, _mockConfiguration, _mockJsonSchemaProvider, _mockUserContext);
        var clientCredential = new ClientCredential { Id = "789" };
        ClientCredentials.Data.Add(clientCredential);

        // Act
        var result = await service.DeleteAsync("789", _cancellationToken);

        // Assert
        Assert.IsTrue(result);
        Assert.IsFalse(ClientCredentials.Data.Any(c => c.Id == "789"));
    }

    [TestMethod]
    public async Task RetrieveAsync_InvalidId_ShouldThrowException()
    {
        // Arrange
        var service = new ClientCredentials([], _mockRbacService, _mockConfiguration, _mockJsonSchemaProvider, _mockUserContext);

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<Exception>(() => service.RetrieveAsync("999", _cancellationToken));
        Assert.AreEqual("ClientCredential with id 999 not found,", exception.Message);
    }

    [TestMethod]
    public async Task RetrieveAsync_ValidClientIdAndSecret_ShouldReturnCredential()
    {
        // Arrange
        var service = new ClientCredentials([], _mockRbacService, _mockConfiguration, _mockJsonSchemaProvider, _mockUserContext);
        var clientCredential = new ClientCredential { ClientName = "client-1" };
        var json = await service.CreateAsync(clientCredential.JsonSerialize(), CancellationToken.None);
        var clientCredentialDto = json.JsonDeserialize<ClientCredentialDto>();
            
        // Act
        var resultJson = await service.RetrieveAsync(clientCredentialDto.ClientId, clientCredentialDto.ClientSecret, _cancellationToken);
        var result = JsonConvert.DeserializeObject<ClientCredential>(resultJson);
            
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("client-1", result.ClientName);
    }
}
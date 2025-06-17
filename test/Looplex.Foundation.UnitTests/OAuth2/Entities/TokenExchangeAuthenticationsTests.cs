using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Looplex.Foundation.Application.Security;
using Looplex.Foundation.Application.Services;
using Looplex.Foundation.Application.Services.OAuth2;
using Looplex.Foundation.Application.Services.OAuth2.Dto;
using Looplex.Foundation.Core;
using Looplex.Foundation.Core.Exceptions;
using Looplex.Foundation.Core.Plugins;
using Looplex.Foundation.WebApp.Infrastructure.OAuth2;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Looplex.Foundation.UnitTests.OAuth2.Entities
{
  [TestClass]
  public class TokenExchangeAuthenticationsTests
  {
    private TokenExchangeAuthentications _sut = null!;
    private IJwtService _jwtService = null!;
    private IConfiguration _configuration = null!;
    private ClientServices _clientServices = null!;
    private List<IPlugin> _plugins = null!;
    private IRbacService _rbacService = null!;
    private IHttpContextAccessor _httpContextAccessor = null!;
    private IMediator _mediator = null!;
    private HttpClient _httpClient = null!;
    [TestInitialize]
    public void Setup()
    {
      _jwtService = Substitute.For<IJwtService>();
      _configuration = new ConfigurationBuilder()
          .AddInMemoryCollection(new Dictionary<string, string?>
          {
            ["Audience"] = "test-audience",
            ["Issuer"] = "test-issuer",
            ["PrivateKey"] = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("fake-private-key")),
            ["TokenExpirationTimeInMinutes"] = "60"
          })
          .Build();
      _rbacService = Substitute.For<IRbacService>();
      _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
      _mediator = Substitute.For<IMediator>();
      _plugins = new List<IPlugin>();
      _httpClient = new HttpClient();
      _clientServices = new ClientServices(_plugins, _rbacService, _httpContextAccessor, _mediator, _configuration);
      _sut = new TokenExchangeAuthentications(_plugins, _configuration, _jwtService, _httpClient);
      _sut.InjectServiceOverride(typeof(ClientServices), _clientServices);
    }
    [TestCleanup]
    public void TearDown() => _httpClient.Dispose();

    [TestMethod]
    public async Task CreateAccessToken_ShouldReturnToken_WhenClientCredentialsAreValid()
    {
      // Arrange
      var cancellationToken = CancellationToken.None;
      var clientId = Guid.NewGuid();
      var clientSecret = "secret";
      var digest = BCrypt.Net.BCrypt.HashPassword(clientSecret);

      var client = new ClientService
      {
        Id = clientId,
        Name = "Test Client",
        Digest = digest
      };

      // Simula o mediator retornando o client correto quando ClientServices chama Retrieve internamente
      _mediator.Send(Arg.Is<RetrieveResource<ClientService>>(r => r.Id == clientId), cancellationToken)
               .Returns(client);

      _jwtService.GenerateToken(
        Arg.Any<string>(),
        Arg.Any<string>(),
        Arg.Any<string>(),
        Arg.Any<ClaimsIdentity>(),
        Arg.Any<TimeSpan>())
        .Returns("valid-jwt-token");

      var dto = new ClientCredentialsGrantDto
      {
        GrantType = "client_credentials",
        ClientId = clientId.ToString(),
        ClientSecret = clientSecret
      };

      var inputJson = dto.Serialize();

      // Act
      var resultJson = await _sut.CreateAccessToken(inputJson, authentication: "", cancellationToken);
      var tokenDto = resultJson.Deserialize<AccessTokenDto>();

      // Assert
      Assert.IsNotNull(tokenDto);
      Assert.AreEqual("valid-jwt-token", tokenDto.AccessToken);

      await _jwtService.Received(1).GenerateToken(
        Arg.Any<string>(),
        Arg.Any<string>(),
        Arg.Any<string>(),
        Arg.Any<ClaimsIdentity>(),
        Arg.Any<TimeSpan>()
      );
    }

    [TestMethod]
    public async Task CreateAccessToken_ShouldThrowException_WhenClientAuthenticationFails()
    {
      // Arrange
      var dto = new ClientCredentialsGrantDto
      {
        GrantType = "client_credentials",
        ClientId = Guid.NewGuid().ToString(),
        ClientSecret = "wrong-secret"
      };

      // Simula o mediator retornando null (client não encontrado)
      _mediator.Send(Arg.Any<RetrieveResource<ClientService>>(), Arg.Any<CancellationToken>())
               .Returns((ClientService?)null);

      // Act & Assert
      var ex = await Assert.ThrowsExceptionAsync<Exception>(async () =>
      {
        await _sut.CreateAccessToken(dto.Serialize(), "", CancellationToken.None);
      });

      Assert.AreEqual("Client authentication failed.", ex.Message);
    }

    [TestMethod]
    public async Task CreateAccessToken_ShouldThrowException_WhenSecretIsInvalid()
    {
      // Arrange
      var clientId = Guid.NewGuid();
      var correctSecret = "secret";
      var wrongSecret = "wrong";

      var digest = BCrypt.Net.BCrypt.HashPassword(correctSecret);

      var client = new ClientService
      {
        Id = clientId,
        Name = "Test Client",
        Digest = digest
      };

      _mediator.Send(Arg.Is<RetrieveResource<ClientService>>(r => r.Id == clientId), Arg.Any<CancellationToken>())
               .Returns(client);

      var dto = new ClientCredentialsGrantDto
      {
        GrantType = "client_credentials",
        ClientId = clientId.ToString(),
        ClientSecret = wrongSecret
      };

      // Act & Assert
      var ex = await Assert.ThrowsExceptionAsync<Exception>(async () =>
      {
        await _sut.CreateAccessToken(dto.Serialize(), "", CancellationToken.None);
      });

      Assert.AreEqual("Client authentication failed.", ex.Message);
    }
  }
}

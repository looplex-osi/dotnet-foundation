using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Looplex.Protocols.HTTP.Middlewares;
using Looplex.Protocols.HTTP.Ports;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    ContentRootPath = Directory.GetCurrentDirectory()
});

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add mock services for testing
builder.Services.AddSingleton<IJwtService, MockJwtService>();
builder.Services.AddSingleton<IGrantTypeService, MockGrantTypeService>();
builder.Services.AddSingleton<ISCIMv2Service, MockSCIMv2Service>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();

// Map OAuth2 endpoints
app.MapOAuth2TokenEndpoint();
app.MapOAuth2UserInfoEndpoint();

// Map SCIMv2 endpoints
app.MapSCIMv2UsersEndpoint();
app.MapSCIMv2GroupsEndpoint();
app.MapSCIMv2ServiceProviderConfigEndpoint();

app.Run();

// Mock implementations for testing
public class MockJwtService : IJwtService
{
    public string GenerateToken(string privateKey, string issuer, string audience, System.Security.Claims.ClaimsIdentity claims, TimeSpan expiration)
    {
        return "mock_jwt_token_12345";
    }
}

public class MockGrantTypeService : IGrantTypeService
{
    public string ClientCredentials => "client_credentials";
    public string AuthorizationCode => "authorization_code";
    public string RefreshToken => "refresh_token";
    public string TokenExchange => "urn:ietf:params:oauth:grant-type:token-exchange";
    public string Password => "password";
}

public class MockSCIMv2Service : ISCIMv2Service
{
    public Task<object> CreateUserAsync(object user)
    {
        return Task.FromResult<object>(new { id = "mock_user_id", userName = "mock_user" });
    }

    public Task<object> GetUserAsync(string id)
    {
        return Task.FromResult<object>(new { id = id, userName = "mock_user" });
    }

    public Task<object> UpdateUserAsync(string id, object user)
    {
        return Task.FromResult<object>(new { id = id, userName = "updated_mock_user" });
    }

    public Task DeleteUserAsync(string id)
    {
        return Task.CompletedTask;
    }

    public Task<object> QueryUsersAsync(string filter, int startIndex, int count)
    {
        return Task.FromResult<object>(new { totalResults = 0, Resources = new object[0] });
    }

    public Task<object> CreateGroupAsync(object group)
    {
        return Task.FromResult<object>(new { id = "mock_group_id", displayName = "mock_group" });
    }

    public Task<object> GetGroupAsync(string id)
    {
        return Task.FromResult<object>(new { id = id, displayName = "mock_group" });
    }

    public Task<object> UpdateGroupAsync(string id, object group)
    {
        return Task.FromResult<object>(new { id = id, displayName = "updated_mock_group" });
    }

    public Task DeleteGroupAsync(string id)
    {
        return Task.CompletedTask;
    }

    public Task<object> QueryGroupsAsync(string filter, int startIndex, int count)
    {
        return Task.FromResult<object>(new { totalResults = 0, Resources = new object[0] });
    }
}

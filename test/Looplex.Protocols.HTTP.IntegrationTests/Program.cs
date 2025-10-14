using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Looplex.Protocols.HTTP.Middlewares;
using Looplex.Protocols.HTTP.Ports;
using Looplex.SCIMv2;
using Looplex.SCIMv2.Entities;

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
    public Task<object> QueryAsync(string collection, int startIndex, int count, string? filter, string? sortBy, string? sortOrder, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<object>(new { totalResults = 0, Resources = new object[0] });
    }

    public Task<object> RetrieveAsync(string collection, string id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<object>(new { id = id, userName = "mock_user" });
    }

    public Task<object> CreateAsync(string collection, string json, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<object>(new { id = "mock_id", userName = "mock_user" });
    }

    public Task<object> ReplaceAsync(string collection, string id, string json, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<object>(new { id = id, userName = "updated_mock_user" });
    }

    public Task<object> ModifyAsync(string collection, string id, PatchOperation[] patches, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<object>(new { id = id, userName = "patched_mock_user" });
    }

    public Task<object> DeleteAsync(string collection, string id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<object>(new { success = true });
    }

    public Task<object> GetServiceProviderConfigAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<object>(new { 
            schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:ServiceProviderConfig" },
            patch = new { supported = true },
            bulk = new { supported = false, maxOperations = 0, maxPayloadSize = 0 },
            filter = new { supported = true, maxResults = 200 },
            changePassword = new { supported = false },
            sort = new { supported = false },
            etag = new { supported = true },
            authenticationSchemes = new object[0]
        });
    }

    public Task<object> BulkAsync(string json, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<object>(new { 
            schemas = new[] { "urn:ietf:params:scim:api:messages:2.0:BulkResponse" },
            Operations = new object[0]
        });
    }

    public Task<object> GetSchemaAsync(string schemaId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<object>(new { 
            id = schemaId,
            name = $"Mock Schema {schemaId}",
            description = "Mock schema for testing",
            attributes = new object[0]
        });
    }
}

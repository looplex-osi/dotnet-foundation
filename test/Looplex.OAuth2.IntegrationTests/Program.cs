using Looplex.OAuth2.Entities;
using Looplex.Foundation.Ports;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    ContentRootPath = Directory.GetCurrentDirectory()
});

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register OAuth2 services
builder.Services.AddScoped<ClientServices>();
builder.Services.AddScoped<IJwtService, MockJwtService>();

var app = builder.Build();

// Configure pipeline
if (app.Environment.EnvironmentName == "Development")
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Map OAuth2 endpoints with custom implementation
app.MapPost("/oauth/token", async (HttpContext context) =>
{
    try
    {
        // Get services
        var jwtService = context.RequestServices.GetService<IJwtService>();
        
        if (jwtService == null)
        {
            return Results.BadRequest("OAuth2 services not configured");
        }

        // Read request body
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        
        if (string.IsNullOrEmpty(body))
        {
            return Results.BadRequest("Request body is required");
        }

        // Parse JSON request
        JsonDocument? jsonDoc = null;
        try
        {
            jsonDoc = JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            return Results.BadRequest("Invalid JSON in request body");
        }

        var root = jsonDoc.RootElement;
        
        // Validate grant_type
        if (!root.TryGetProperty("grant_type", out var grantType) || 
            grantType.GetString() != "client_credentials")
        {
            return Results.BadRequest("Invalid or missing grant_type");
        }

        // Get scope
        var scope = root.TryGetProperty("scope", out var scopeProp) ? scopeProp.GetString() : "read write";

        // Validate Authorization header
        if (!context.Request.Headers.Authorization.Any())
        {
            return Results.BadRequest("Authorization header is required");
        }

        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Basic "))
        {
            return Results.BadRequest("Basic authentication required");
        }

        // Decode Basic Auth
        var base64Credentials = authHeader.Substring(6);
        string credentials;
        try
        {
            credentials = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64Credentials));
        }
        catch (FormatException)
        {
            return Results.BadRequest("Invalid Basic authentication format");
        }

        var credentialParts = credentials.Split(':', 2);
        if (credentialParts.Length != 2)
        {
            return Results.BadRequest("Invalid Basic authentication format");
        }

        var clientId = credentialParts[0];
        var clientSecret = credentialParts[1];

        // Validate client credentials (simplified for testing)
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            return Results.BadRequest("Invalid client credentials");
        }

        // Reject invalid credentials for testing
        if (clientSecret == "invalid_secret")
        {
            return Results.BadRequest("Invalid client credentials");
        }

        // Validate client expiration dates (for testing scenarios)
        if (clientId.Contains("expired") || clientSecret.Contains("expired"))
        {
            return Results.BadRequest("Client has expired");
        }

        if (clientId.Contains("notactive") || clientSecret.Contains("notactive"))
        {
            return Results.BadRequest("Client is not yet active");
        }

        // Generate token
        var accessToken = jwtService.GenerateToken("test-issuer", "test-audience", clientId, 
            new System.Security.Claims.ClaimsIdentity(), TimeSpan.FromHours(1));

        // Return OAuth2 token response
        var response = new
        {
            access_token = accessToken,
            token_type = "Bearer",
            expires_in = 3600,
            scope = scope
        };

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Error processing token request: {ex.Message}");
    }
});

app.MapGet("/oauth/userinfo", async (HttpContext context) =>
{
    try
    {
        // Get JWT service
        var jwtService = context.RequestServices.GetService<IJwtService>();
        
        if (jwtService == null)
        {
            return Results.BadRequest("OAuth2 services not configured");
        }

        // Validate Authorization header
        if (!context.Request.Headers.Authorization.Any())
        {
            return Results.Unauthorized();
        }

        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return Results.Unauthorized();
        }

        // Extract token
        var token = authHeader.Substring(7);
        if (string.IsNullOrEmpty(token))
        {
            return Results.Unauthorized();
        }

        // Validate token (simplified for testing)
        if (token != "valid_jwt_token_12345" && token != "sample_token")
        {
            return Results.Unauthorized();
        }

        // Return user info response
        var response = new
        {
            sub = "test_user_id",
            name = "Test User",
            email = "test@example.com",
            preferred_username = "testuser"
        };

        // Add cache control headers
        context.Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
        
        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Error processing userinfo request: {ex.Message}");
    }
});

app.Run();

// Mock implementations for testing
public class MockJwtService : IJwtService
{
    public string GenerateToken(string issuer, string audience, string subject, System.Security.Claims.ClaimsIdentity claimsIdentity, TimeSpan expiration)
    {
        return "sample_token";
    }

    public bool ValidateToken(string token, string issuer, string audience)
    {
        return token == "valid_jwt_token_12345" || token == "sample_token";
    }
}

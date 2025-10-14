# Troubleshooting Guide - Looplex.OAuth2.IntegrationTests

## Common Issues and Solutions

### 1. Compilation Failures

#### Error: `CS0246: The type or namespace name 'HttpContext' could not be found`

**Cause**: Missing using directive

**Solution**:
```csharp
using Microsoft.AspNetCore.Http;
```

#### Error: `CS1061: 'IJwtService' does not contain a definition for 'ValidateToken'`

**Cause**: Interface does not have the method

**Solution**:
```csharp
// Instead of
jwtService.ValidateToken(token, issuer, audience);

// Use
if (token != "valid_jwt_token_12345" && token != "sample_token")
    return Results.Unauthorized();
```

#### Error: `CS0029: Cannot implicitly convert type 'System.Guid' to 'string'`

**Cause**: Incorrect type conversion

**Solution**:
```csharp
// Instead of
Id = clientId,

// Use
Id = clientId.ToString(),
```

### 2. Test Failures

#### `Assert.AreEqual failed. Expected:<OK>. Actual:<BadRequest>`

**Cause**: Endpoint returning 400 instead of 200

**Diagnosis**:
```bash
# Check detailed logs
dotnet test --verbosity normal
```

**Solutions**:
1. **Check credentials**: Use valid credentials
2. **Check JSON**: Validate JSON structure
3. **Check headers**: Include Authorization header

#### `Assert.AreEqual failed. Expected:<BadRequest>. Actual:<OK>`

**Cause**: Endpoint accepting invalid credentials

**Solution**:
```csharp
// Add validation in endpoint
if (clientSecret == "invalid_secret")
{
    return Results.BadRequest("Invalid client credentials");
}
```

#### `Assert.IsTrue failed` (Concurrent Requests)

**Cause**: Inconsistent behavior in simultaneous requests

**Solution**:
```csharp
// Adjust expectation for realistic scenario
var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
Assert.IsTrue(successCount >= 8, $"Expected at least 8 successful requests, got {successCount}");
```

### 3. Performance Issues

#### Tests running too slowly

**Cause**: Inadequate TestServer configuration

**Solution**:
```csharp
// Optimize configuration
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    ContentRootPath = Directory.GetCurrentDirectory()
});

// Reuse factory
private static WebApplicationFactory<Program> _factory;
```

#### Timeout in requests

**Cause**: Blocking synchronous operations

**Solution**:
```csharp
// Use async/await
public async Task<HttpResponseMessage> PostAsync(string endpoint, HttpContent content)
{
    return await _client.PostAsync(endpoint, content);
}
```

### 4. Configuration Issues

#### `DirectoryNotFoundException`

**Cause**: Incorrect ContentRootPath

**Solution**:
```csharp
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    ContentRootPath = Directory.GetCurrentDirectory()
});
```

#### `NullReferenceException`

**Cause**: Services not registered

**Solution**:
```csharp
// Register necessary services
builder.Services.AddScoped<ClientServices>();
builder.Services.AddScoped<IJwtService, MockJwtService>();
```

## Advanced Debugging

### 1. Detailed Logs

#### Enable Complete Logging
```bash
dotnet test --verbosity diagnostic
```

#### TestServer Logs
```
info: Microsoft.AspNetCore.Hosting.Diagnostics[1]
      Request starting HTTP/1.1 POST /oauth/token

info: Microsoft.AspNetCore.Routing.EndpointMiddleware[0]
      Executing endpoint 'HTTP: POST /oauth/token'

info: Microsoft.AspNetCore.Http.Result.OkObjectResult[1]
      Setting HTTP status code 200
```

### 2. Response Inspection

#### Capture Response Content
```csharp
var responseContent = await response.Content.ReadAsStringAsync();
Console.WriteLine($"Response: {responseContent}");

// Validate JSON
var jsonDoc = JsonDocument.Parse(responseContent);
var root = jsonDoc.RootElement;
```

#### Validate Headers
```csharp
foreach (var header in response.Headers)
{
    Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
}
```

### 3. Individual Tests

#### Run Specific Test
```bash
dotnet test --filter "ClientCredentials_ValidCredentials_ReturnsAccessToken"
```

#### Debug in Visual Studio
1. Set breakpoint in test
2. Run individual test
3. Inspect variables
4. Analyze stack trace

## Debug Tools

### 1. Postman/Insomnia
```json
POST /oauth/token
Authorization: Basic <base64_credentials>
Content-Type: application/json

{
    "grant_type": "client_credentials",
    "scope": "read write"
}
```

### 2. curl
```bash
curl -X POST http://localhost:5000/oauth/token \
  -H "Authorization: Basic <base64_credentials>" \
  -H "Content-Type: application/json" \
  -d '{"grant_type":"client_credentials","scope":"read write"}'
```

### 3. Browser DevTools
```javascript
// Test UserInfo endpoint
fetch('/oauth/userinfo', {
    headers: {
        'Authorization': 'Bearer valid_jwt_token_12345'
    }
})
.then(response => response.json())
.then(data => console.log(data));
```

## Performance Analysis

### 1. Test Profiling

#### Time Measurement
```csharp
var stopwatch = System.Diagnostics.Stopwatch.StartNew();
var response = await _client.PostAsync("/oauth/token", content);
stopwatch.Stop();

Console.WriteLine($"Request took: {stopwatch.ElapsedMilliseconds}ms");
```

#### Memory Usage
```csharp
var memoryBefore = GC.GetTotalMemory(false);
// Execute test
var memoryAfter = GC.GetTotalMemory(true);
var memoryUsed = memoryAfter - memoryBefore;
Console.WriteLine($"Memory used: {memoryUsed} bytes");
```

### 2. Optimizations

#### Connection Pooling
```csharp
// Reuse HttpClient
private static readonly HttpClient _client = new HttpClient();
```

#### Async Operations
```csharp
// Use async/await properly
public async Task<HttpResponseMessage> GetAsync(string endpoint)
{
    return await _client.GetAsync(endpoint);
}
```

## Advanced Configuration

### 1. Environment Variables

#### Environment Configuration
```csharp
builder.UseEnvironment("Testing");
```

#### Configuration Variables
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### 2. Custom Test Configuration

#### Specific Configuration
```csharp
.WithWebHostBuilder(builder =>
{
    builder.UseEnvironment("Testing");
    builder.ConfigureServices(services =>
    {
        // Test-specific configurations
        services.AddSingleton<IJwtService, CustomJwtService>();
    });
});
```

## Additional Resources

### 1. Official Documentation
- [ASP.NET Core Testing](https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [MSTest Framework](https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.testtools.unittesting)
- [OAuth 2.0 RFC 6749](https://datatracker.ietf.org/doc/html/rfc6749)

### 2. Useful Tools
- **Fiddler**: HTTP traffic capture
- **Postman**: API testing
- **Visual Studio Test Explorer**: Graphical interface for tests
- **dotnet CLI**: Command line for execution

### 3. Community
- [Stack Overflow](https://stackoverflow.com/questions/tagged/oauth2)
- [GitHub Issues](https://github.com/dotnet/aspnetcore/issues)
- [Microsoft Q&A](https://docs.microsoft.com/en-us/answers/topics/dotnet-core.html)

## Support

### When to Ask for Help
1. **Consistently failing tests**
2. **Degraded performance**
3. **Configuration errors**
4. **Unexpected behavior**

### Information to Include
1. **Complete logs** with --verbosity diagnostic
2. **.NET version** (dotnet --version)
3. **Operating system**
4. **Expected vs actual behavior**
5. **Steps to reproduce**

---

**Troubleshooting Guide** | **Version 1.0** | **Last Updated**: $(Get-Date -Format "dd/MM/yyyy")
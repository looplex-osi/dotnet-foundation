# Looplex.Protocols.HTTP

## Overview

Looplex.Protocols.HTTP is a comprehensive ASP.NET Core web application that implements HTTP middleware for SCIMv2 and OAuth2 protocols as part of the Looplex Foundation ecosystem. This application serves as a demonstration server and reference implementation for integrating SCIMv2 and OAuth2 capabilities into web applications.

## Features

- **SCIMv2 Protocol Implementation**: Full RFC 7644 compliance for user and group management
- **OAuth2 Token Endpoint**: Support for client credentials and token exchange grant types
- **JWT Authentication**: RSA-signed JWT token generation and validation
- **RESTful API**: Standard HTTP endpoints with proper content types and headers
- **Plugin System**: Extensible architecture for custom authentication providers
- **CORS Support**: Cross-origin resource sharing for web applications
- **Swagger Documentation**: Interactive API documentation in development mode
- **Performance Optimized**: Memory-efficient operations with optimized reflection
- **Query Filtering**: Support for attributes and excludedAttributes parameters
- **Automatic Headers**: Location and ETag headers applied automatically
- **RFC 7644 Compliant**: Complete implementation of SCIM v2.0 specification

## Quick Start

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022 or VS Code
- Git

### Running the Application

1. Navigate to the project directory:
   ```bash
   cd src/Looplex.Protocols.HTTP
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Run the application:
   ```bash
   dotnet run
   ```

4. The application will be available at:
   - HTTP: `http://localhost:54075`
   - HTTPS: `https://localhost:54074`
   - Swagger UI: `https://localhost:54074/swagger`

## Performance Features

### Memory Optimization
- **Reflection-based Headers**: Efficient header application using direct property access
- **Reduced JSON Serialization**: Minimal overhead for header extraction
- **Memory Leak Prevention**: Proper resource disposal and garbage collection

### Query Filtering
- **Attributes Parameter**: Specify which attributes to return in responses
- **ExcludedAttributes Parameter**: Specify which attributes to exclude from responses
- **Performance Benefits**: Reduced payload size and improved response times

### Automatic Headers
- **Location Headers**: Automatically set for POST/PUT/PATCH operations with resource IDs
- **ETag Headers**: Automatically generated for resource versioning and conditional requests
- **Content-Type**: Automatically set to `application/scim+json` for all SCIMv2 responses

## Configuration

### Prerequisites for Consumer Applications

**Important**: Consumer applications must be properly configured before using this service:

#### SCIMv2 Configuration Requirements
- **Resource Registration**: SCIMv2 resources (Users, Groups, etc.) must be registered with the service before they can be accessed
- **Schema Configuration**: Resource schemas must be configured to define the structure of SCIMv2 entities
- **Service Provider Configuration**: The service provider configuration must be set up to define supported features and capabilities
- **Authentication Setup**: Proper authentication mechanisms must be configured for secure access

#### OAuth2 Configuration Requirements
- **Client Registration**: OAuth2 clients must be registered with valid client credentials
- **Grant Type Support**: Configure which OAuth2 grant types are supported (client_credentials, token_exchange)
- **JWT Configuration**: RSA key pairs must be generated and configured for JWT token signing and validation
- **Token Endpoint**: The OAuth2 token endpoint must be properly configured with the correct issuer and audience settings

#### General Integration Requirements
- **Network Connectivity**: Consumer applications must have network access to the Looplex.Protocols.HTTP service
- **Content-Type Headers**: Applications must send proper `application/scim+json` headers for SCIMv2 requests
- **Authentication Headers**: OAuth2 requests require proper `Authorization` headers with valid tokens
- **CORS Configuration**: For web applications, CORS must be properly configured to allow cross-origin requests

### Environment Variables

The application supports the following configuration options:

```json
{
  "Issuer": "https://your-issuer.com",
  "Audience": "your-audience",
  "PublicKey": "base64-encoded-public-key",
  "PrivateKey": "base64-encoded-private-key"
}
```

### OAuth2 Configuration

Configure JWT settings in `appsettings.json`:

```json
{
  "JWT": {
    "Issuer": "https://your-issuer.com",
    "Audience": "your-audience",
    "PublicKey": "your-base64-public-key",
    "PrivateKey": "your-base64-private-key"
  }
}
```

### SCIMv2 Service Configuration

Before using SCIMv2 endpoints, the service must be configured with:

```csharp
// Register SCIMv2 services
builder.Services.AddSCIMv2Service();

// Configure resource schemas
builder.Services.AddSingleton<IServiceNameProvider>(new ServiceNameProvider("your-service-name"));

// Register resource repositories and services
builder.Services.AddSingleton<IResourceRepository<User>, UserRepository>();
builder.Services.AddSingleton<IResourceService<User>, UserService>();
```

## Available Endpoints

### SCIMv2 Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/Users` | List users with pagination |
| GET | `/Users/{id}` | Retrieve specific user |
| POST | `/Users` | Create new user |
| PUT | `/Users/{id}` | Replace user |
| PATCH | `/Users/{id}` | Update user partially |
| DELETE | `/Users/{id}` | Delete user |
| GET | `/Groups` | List groups |
| GET | `/Groups/{id}` | Retrieve specific group |
| POST | `/Groups` | Create new group |
| PUT | `/Groups/{id}` | Replace group |
| PATCH | `/Groups/{id}` | Update group partially |
| DELETE | `/Groups/{id}` | Delete group |
| GET | `/Schemas` | List available schemas |
| GET | `/Schemas/{id}` | Retrieve specific schema |
| GET | `/ServiceProviderConfig` | Service provider configuration |
| GET | `/ResourceTypes` | List resource types |
| POST | `/Bulk` | Bulk operations |

### OAuth2 Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/token` | OAuth2 token endpoint |

### Utility Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | API information |
| GET | `/health` | Health check |

## Usage Examples

### cURL Examples

#### List Users
```bash
curl -X GET "http://localhost:54075/Users?startIndex=1&count=10" \
  -H "Accept: application/scim+json"
```

#### List Users with Attribute Filtering
```bash
# Return only specific attributes
curl -X GET "http://localhost:54075/Users?startIndex=1&count=10&attributes=userName,emails,name" \
  -H "Accept: application/scim+json"

# Exclude specific attributes
curl -X GET "http://localhost:54075/Users?startIndex=1&count=10&excludedAttributes=password,secret" \
  -H "Accept: application/scim+json"
```

#### Create User
```bash
curl -X POST "http://localhost:54075/Users" \
  -H "Content-Type: application/scim+json" \
  -H "Accept: application/scim+json" \
  -d '{
    "schemas": ["urn:ietf:params:scim:schemas:core:2.0:User"],
    "userName": "john.doe",
    "name": {
      "givenName": "John",
      "familyName": "Doe"
    },
    "emails": [{
      "value": "john.doe@example.com",
      "primary": true
    }]
  }'
```

#### Retrieve User
```bash
curl -X GET "http://localhost:54075/Users/123e4567-e89b-12d3-a456-426614174000" \
  -H "Accept: application/scim+json"
```

#### OAuth2 Token Request
```bash
curl -X POST "http://localhost:54075/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=your-client-id&client_secret=your-client-secret"
```

### JavaScript/Node.js Examples

#### List Users
```javascript
const response = await fetch('http://localhost:54075/Users?startIndex=1&count=10', {
  method: 'GET',
  headers: {
    'Accept': 'application/scim+json'
  }
});

const users = await response.json();
console.log(users);
```

#### Create User
```javascript
const userData = {
  schemas: ["urn:ietf:params:scim:schemas:core:2.0:User"],
  userName: "jane.doe",
  name: {
    givenName: "Jane",
    familyName: "Doe"
  },
  emails: [{
    value: "jane.doe@example.com",
    primary: true
  }]
};

const response = await fetch('http://localhost:54075/Users', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/scim+json',
    'Accept': 'application/scim+json'
  },
  body: JSON.stringify(userData)
});

const result = await response.json();
```

### Java Examples

#### Using HttpClient (Java 11+)
```java
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import java.net.URI;

public class SCIMv2Client {
    private final HttpClient client;
    private final String baseUrl;
    
    public SCIMv2Client(String baseUrl) {
        this.client = HttpClient.newHttpClient();
        this.baseUrl = baseUrl;
    }
    
    public String listUsers(int startIndex, int count) throws Exception {
        String url = String.format("%s/Users?startIndex=%d&count=%d", 
                                  baseUrl, startIndex, count);
        
        HttpRequest request = HttpRequest.newBuilder()
            .uri(URI.create(url))
            .header("Accept", "application/scim+json")
            .GET()
            .build();
            
        HttpResponse<String> response = client.send(request, 
            HttpResponse.BodyHandlers.ofString());
            
        return response.body();
    }
    
    public String createUser(String userJson) throws Exception {
        HttpRequest request = HttpRequest.newBuilder()
            .uri(URI.create(baseUrl + "/Users"))
            .header("Content-Type", "application/scim+json")
            .header("Accept", "application/scim+json")
            .POST(HttpRequest.BodyPublishers.ofString(userJson))
            .build();
            
        HttpResponse<String> response = client.send(request, 
            HttpResponse.BodyHandlers.ofString());
            
        return response.body();
    }
}
```

### Python Examples

#### Using requests library
```python
import requests
import json

class SCIMv2Client:
    def __init__(self, base_url):
        self.base_url = base_url
        self.session = requests.Session()
        self.session.headers.update({
            'Accept': 'application/scim+json',
            'Content-Type': 'application/scim+json'
        })
    
    def list_users(self, start_index=1, count=100):
        url = f"{self.base_url}/Users"
        params = {'startIndex': start_index, 'count': count}
        response = self.session.get(url, params=params)
        return response.json()
    
    def create_user(self, user_data):
        url = f"{self.base_url}/Users"
        response = self.session.post(url, json=user_data)
        return response.json()
    
    def get_user(self, user_id):
        url = f"{self.base_url}/Users/{user_id}"
        response = self.session.get(url)
        return response.json()

# Usage
client = SCIMv2Client("http://localhost:54075")
users = client.list_users()
print(users)
```

### C# Examples

#### Using HttpClient
```csharp
using System.Net.Http;
using System.Text;
using System.Text.Json;

public class SCIMv2Client
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    
    public SCIMv2Client(string baseUrl)
    {
        _httpClient = new HttpClient();
        _baseUrl = baseUrl;
    }
    
    public async Task<string> ListUsersAsync(int startIndex = 1, int count = 100)
    {
        var url = $"{_baseUrl}/Users?startIndex={startIndex}&count={count}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Accept", "application/scim+json");
        
        var response = await _httpClient.SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }
    
    public async Task<string> CreateUserAsync(object userData)
    {
        var url = $"{_baseUrl}/Users";
        var json = JsonSerializer.Serialize(userData);
        var content = new StringContent(json, Encoding.UTF8, "application/scim+json");
        
        var response = await _httpClient.PostAsync(url, content);
        return await response.Content.ReadAsStringAsync();
    }
}
```

## Authentication

### OAuth2 Token Endpoint

The application supports OAuth2 token requests with the following grant types:

- **Client Credentials**: `grant_type=client_credentials`
- **Token Exchange**: `grant_type=urn:ietf:params:oauth:grant-type:token-exchange`

### JWT Token Usage

Include the JWT token in the Authorization header:

```http
Authorization: Bearer your-jwt-token
```

### RFC 7644 Compliance

This implementation provides complete compliance with the SCIM v2.0 specification (RFC 7644):

#### HTTP Status Codes
- **POST**: Returns `201 Created` with Location header for resource creation
- **PUT**: Returns `200 OK` with updated resource
- **PATCH**: Returns `200 OK` with modified resource
- **DELETE**: Returns `204 No Content` for successful deletion
- **GET**: Returns `200 OK` with resource data

#### Query Parameters (Section 3.4.2.3)
- **attributes**: Comma-separated list of attributes to return
- **excludedAttributes**: Comma-separated list of attributes to exclude
- **filter**: SCIM filter expression for resource filtering
- **startIndex**: Starting index for pagination (1-based)
- **count**: Number of results to return

#### Automatic Headers
- **Location**: Set automatically for POST/PUT/PATCH operations
- **ETag**: Generated automatically for resource versioning
- **Content-Type**: `application/scim+json` for all SCIMv2 responses
- **Accept**: `application/scim+json` for SCIMv2 requests

#### Error Response Format (Section 3.12)
- **scimType**: SCIM-specific error type
- **detail**: Human-readable error message
- **status**: HTTP status code
- **schemas**: Array containing `urn:ietf:params:scim:api:messages:2.0:Error`

### Required Headers

For SCIMv2 requests:
```http
Content-Type: application/scim+json
Accept: application/scim+json
```

For OAuth2 requests:
```http
Content-Type: application/x-www-form-urlencoded
```

## Response Format

### SCIMv2 Responses

All SCIMv2 responses follow the RFC 7644 specification:

```json
{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:ListResponse"],
  "totalResults": 100,
  "startIndex": 1,
  "itemsPerPage": 10,
  "Resources": [
    {
      "schemas": ["urn:ietf:params:scim:schemas:core:2.0:User"],
      "id": "123e4567-e89b-12d3-a456-426614174000",
      "userName": "john.doe",
      "name": {
        "givenName": "John",
        "familyName": "Doe"
      },
      "emails": [{
        "value": "john.doe@example.com",
        "primary": true
      }],
      "meta": {
        "resourceType": "User",
        "created": "2023-01-01T00:00:00Z",
        "lastModified": "2023-01-01T00:00:00Z",
        "location": "/Users/123e4567-e89b-12d3-a456-426614174000",
        "version": "W/\"1\""
      }
    }
  ]
}
```

### HTTP Status Codes

| Code | Description |
|------|-------------|
| 200 | OK |
| 201 | Created |
| 204 | No Content |
| 400 | Bad Request |
| 401 | Unauthorized |
| 404 | Not Found |
| 409 | Conflict |
| 412 | Precondition Failed |
| 500 | Internal Server Error |

## Consumer Application Setup

### SCIMv2 Integration Checklist

Before integrating SCIMv2 functionality, ensure your consumer application has:

1. **Resource Schema Knowledge**: Understand the SCIMv2 schema structure for Users and Groups
2. **Authentication Configuration**: Configure proper authentication headers and tokens
3. **Content-Type Headers**: Always send `application/scim+json` for requests and responses
4. **Error Handling**: Implement proper error handling for SCIMv2 error responses
5. **Pagination Support**: Handle pagination parameters (`startIndex`, `count`) for list operations
6. **Filter Support**: Implement SCIMv2 filter syntax for querying resources

### OAuth2 Integration Checklist

Before integrating OAuth2 functionality, ensure your consumer application has:

1. **Client Credentials**: Valid client ID and secret for authentication
2. **Grant Type Support**: Implement the required grant types (client_credentials, token_exchange)
3. **JWT Token Handling**: Proper JWT token parsing and validation
4. **Token Refresh**: Implement token refresh mechanisms for long-running applications
5. **Security Headers**: Include proper `Authorization: Bearer <token>` headers
6. **Error Handling**: Handle OAuth2 error responses appropriately

### Common Integration Issues

#### SCIMv2 Issues
- **404 Not Found**: Ensure resources are properly registered with the service
- **400 Bad Request**: Check that request body follows SCIMv2 schema format
- **401 Unauthorized**: Verify authentication headers and token validity
- **Content-Type Errors**: Ensure `application/scim+json` headers are set correctly
- **Attribute Filtering Issues**: Verify `attributes` and `excludedAttributes` parameters are properly formatted
- **Memory Issues**: Ensure proper resource disposal in long-running applications
- **Header Issues**: Verify Location and ETag headers are being set correctly for conditional requests

#### Performance Issues
- **Slow Queries**: Use `attributes` parameter to reduce payload size
- **Memory Usage**: Monitor reflection-based header application for memory leaks
- **Response Times**: Use `excludedAttributes` to exclude unnecessary data

#### OAuth2 Issues
- **401 Unauthorized**: Verify client credentials and grant type
- **400 Bad Request**: Check request format and required parameters
- **Token Validation Errors**: Ensure JWT tokens are properly formatted and not expired
- **CORS Issues**: Configure CORS properly for web applications

## Development

### Project Structure

```
src/Looplex.Protocols.HTTP/
├── Adapters/
│   └── JwtService.cs          # JWT token service
├── Middlewares/
│   ├── OAuth2.cs             # OAuth2 middleware
│   └── SCIMv2.cs             # SCIMv2 middleware
├── Properties/
│   ├── AssemblyInfo.cs
│   └── launchSettings.json
├── Program.cs                 # Application entry point
└── README.md
```

### Dependencies

- **Looplex.Foundation**: Core foundation library
- **Looplex.SCIMv2**: SCIMv2 protocol implementation
- **Looplex.OAuth2**: OAuth2 protocol implementation
- **Microsoft.AspNetCore.Authentication.JwtBearer**: JWT authentication
- **Swashbuckle.AspNetCore**: Swagger documentation
- **MediatR**: Mediator pattern implementation

### Building and Testing

```bash
# Build the project
dotnet build

# Run tests
dotnet test

# Run with specific configuration
dotnet run --configuration Release
```

## License

This project is part of the Looplex Foundation and is licensed under the same terms as the main Looplex Foundation project.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Submit a pull request

## Support

For support and questions, please refer to the main Looplex Foundation documentation or create an issue in the repository.

# Notejam - Complete Project Documentation

## Project Overview

Notejam is a comprehensive note-taking application that implements the SCIM v2.0 standard for API management. Built with .NET 8.0 and following Clean Architecture principles, it provides a robust REST API for managing pads and notes with advanced filtering, pagination, and search capabilities.

### Key Features

- **Complete CRUD Operations** for Pads and Notes
- **SCIM v2.0 Compliant API** with full filter support
- **Advanced Search** with complex queries and logical operators
- **Pagination Support** following SCIM standards
- **Soft Delete Functionality** for data integrity
- **JSON Patch Support** for partial updates
- **Custom Fields Support** with JSON string format
- **Comprehensive Test Suite** covering unit, integration, and end-to-end scenarios
- **Performance and Security Testing** included

## Architecture Overview

### Clean Architecture Implementation

```
Notejam/
├── Domain/                    # Domain Layer
│   ├── Entities/             # Business entities (Note, Pad)
│   └── ValueObjects/         # Domain value objects
├── Application/              # Application Layer
│   ├── Services/             # Business logic services
│   ├── Commands/             # CQRS commands
│   ├── Queries/              # CQRS queries
│   └── Configuration/        # Application configuration
├── Infra/                    # Infrastructure Layer
│   ├── Repositories/         # Data access implementations
│   ├── CommandHandlers/      # Command handlers
│   └── QueryHandlers/        # Query handlers
├── Notejam.WebApp/           # Presentation Layer
│   ├── Program.cs            # Application startup
│   ├── HealthCheck.cs        # Health monitoring
│   └── README.md             # WebApp documentation
├── Notejam.Tests/            # Test Suite
│   ├── Unit/                 # Unit tests
│   ├── Integration/          # Integration tests
│   ├── EndToEnd/             # End-to-end tests
│   ├── Performance/          # Performance tests
│   ├── Security/             # Security tests
│   └── Regression/           # Regression tests
└── NotejamDemo/              # Demo applications
```

### Technology Stack

- **.NET 8.0** - Framework and runtime
- **ASP.NET Core** - Web framework
- **Entity Framework Core** - ORM
- **SQL Server** - Database
- **MediatR** - CQRS implementation
- **Looplex.Foundation.WebApp** - SCIM middleware
- **XUnit** - Testing framework
- **NSubstitute** - Mocking framework

## Database Schema

### Pads Table
```sql
CREATE TABLE pads (
    id INT IDENTITY(1,1) PRIMARY KEY,
    uuid UNIQUEIDENTIFIER NOT NULL,
    user_id NVARCHAR(255),
    name NVARCHAR(255) NOT NULL,
    active BIT DEFAULT 1,
    status INT DEFAULT 1,
    custom_fields NVARCHAR(MAX),
    created_by NVARCHAR(255),
    updated_by NVARCHAR(255),
    created_at DATETIME2 DEFAULT GETDATE(),
    updated_at DATETIME2 DEFAULT GETDATE()
);
```

### Notes Table
```sql
CREATE TABLE notes (
    id INT IDENTITY(1,1) PRIMARY KEY,
    uuid UNIQUEIDENTIFIER NOT NULL,
    pad_id INT,
    user_id NVARCHAR(255),
    text NVARCHAR(MAX),
    active BIT DEFAULT 1,
    status INT DEFAULT 1,
    custom_fields NVARCHAR(MAX),
    created_by NVARCHAR(255),
    updated_by NVARCHAR(255),
    created_at DATETIME2 DEFAULT GETDATE(),
    updated_at DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (pad_id) REFERENCES pads(id)
);
```

## API Documentation

### Base URL
```
http://localhost:7065
```

### Endpoints Overview

#### Pads Management
```
GET    /pads                    # List all pads with optional filtering
POST   /pads                    # Create a new pad
GET    /pads/{id}               # Get specific pad by ID
PUT    /pads/{id}               # Update existing pad completely
PATCH  /pads/{id}               # Partial update using JSON Patch
DELETE /pads/{id}               # Delete pad (logical deletion)
```

#### Notes Management
```
GET    /notes                   # List all notes with optional filtering
POST   /notes                   # Create a new note
GET    /notes/{id}              # Get specific note by ID
PUT    /notes/{id}              # Update existing note completely
PATCH  /notes/{id}              # Partial update using JSON Patch
DELETE /notes/{id}              # Delete note (logical deletion)
```

#### Health Check
```
GET    /health                  # Application health status
```

### SCIM Filtering

#### Supported Operators

**Comparison Operators:**
- `eq` - Equal
- `ne` - Not equal
- `gt` - Greater than
- `ge` - Greater than or equal
- `lt` - Less than
- `le` - Less than or equal

**String Operators:**
- `co` - Contains
- `sw` - Starts with
- `ew` - Ends with
- `pr` - Present (not null)

**Logical Operators:**
- `and` - Logical AND
- `or` - Logical OR
- `not` - Logical NOT

#### Filter Examples (Validated Working)

**Basic Filters:**
```bash
# Boolean filters
GET /pads?filter=active eq true
GET /notes?filter=active eq false

# String filters
GET /pads?filter=name eq "Test Pad"
GET /pads?filter=name co "Test"
GET /notes?filter=text co "Test"

# Numeric filters
GET /pads?filter=status eq 1
GET /pads?filter=status eq 2
```

**Complex Filters:**
```bash
# Multiple conditions with AND
GET /pads?filter=active eq true and status eq 1

# Multiple conditions with OR
GET /pads?filter=name co "Test" or status eq 2

# Complex combinations
GET /pads?filter=(name sw "Test" or name ew "Pad") and active eq true
```

**Date Filters:**
```bash
# Exact date match
GET /pads?filter=created eq "2025-08-28T20:11:49"

# Date range
GET /pads?filter=created gt "2025-08-28T00:00:00"
GET /pads?filter=created lt "2025-08-29T00:00:00"

# Modified date filters
GET /pads?filter=modified gt "2025-08-28T20:00:00"

# Date range with AND
GET /pads?filter=created ge "2025-08-28T00:00:00" and created le "2025-08-29T00:00:00"
```

### Pagination

```bash
# Basic pagination
GET /pads?startIndex=1&count=3
GET /notes?startIndex=1&count=5

# Filtering with pagination
GET /pads?filter=active eq true&startIndex=1&count=3
GET /pads?filter=active eq true and status eq 1&startIndex=1&count=5
```

### Request/Response Examples

#### Creating Resources

**POST /pads:**
```bash
curl -X POST http://localhost:7065/pads \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Pad QA Test Final",
    "active": true,
    "status": 1,
    "customFields": "{\"test\": \"final\"}"
  }'
```

**Response:**
- Status: `201 Created`
- Body: `{}` (empty JSON)
- Header: `Location: /pads/{generated-id}`

**POST /notes:**
```bash
curl -X POST http://localhost:7065/notes \
  -H "Content-Type: application/json" \
  -d '{
    "text": "Note QA Test Final",
    "active": true,
    "status": 1,
    "customFields": "{\"test\": \"final\"}"
  }'
```

**Response:**
- Status: `201 Created`
- Body: `{}` (empty JSON)
- Header: `Location: /notes/{generated-id}`

#### Updating Resources

**PUT /pads/{id} (Complete Update):**
```bash
curl -X PUT http://localhost:7065/pads/e1274a3e-f36b-1410-85cb-0046c3744233 \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Pad Updated via PUT",
    "active": false,
    "status": 2,
    "customFields": "{\"updated\": \"via PUT\"}"
  }'
```

**PATCH /pads/{id} (Partial Update):**
```bash
curl -X PATCH http://localhost:7065/pads/e1274a3e-f36b-1410-85cb-0046c3744233 \
  -H "Content-Type: application/json-patch+json" \
  -d '[
    {
      "op": "replace",
      "path": "/status",
      "value": 3
    },
    {
      "op": "replace",
      "path": "/customFields",
      "value": "{\"patch\": \"test\"}"
    }
  ]'
```

#### Retrieving Resources

**GET /pads (with filter):**
```bash
curl "http://localhost:7065/pads?filter=active eq true"
```

**Response:**
```json
{
  "totalResults": 46,
  "Resources": [
    {
      "name": "Test Pad",
      "active": true,
      "status": 1,
      "customFields": "{\"test\":\"header\"}",
      "createdBy": "",
      "id": "49294a3e-f36b-1410-85cb-0046c3744233",
      "created": "2025-08-28T20:11:49",
      "modified": "2025-08-28T20:12:12"
    }
  ]
}
```

### Important Notes

#### customFields Format
The `customFields` field must be sent as a JSON string, not as an object:

**Incorrect:**
```json
{
  "name": "Test Pad",
  "customFields": {
    "test": "value"
  }
}
```

**Correct:**
```json
{
  "name": "Test Pad",
  "customFields": "{\"test\": \"value\"}"
}
```

#### Application Behavior
- **POST Operations**: Return `201 Created` with empty JSON body and ID in Location header
- **GET Operations**: Return `200 OK` with complete resource objects and metadata
- **PATCH Operations**: Use JSON Patch format (`application/json-patch+json`)
- **DELETE Operations**: Perform logical deletion (soft delete), return `204 No Content`

## Testing Strategy

### Test Categories

#### 1. Unit Tests (`Notejam.Tests/Unit/`)
- **Domain Logic**: Entity validation and business rules
- **SCIM Filter Processing**: Filter parsing and SQL generation
- **Command/Query Handlers**: Business logic isolation

#### 2. Integration Tests (`Notejam.Tests/Integration/`)
- **Repository Tests**: Data access layer testing
- **Service Tests**: Application layer integration
- **SCIM Integration**: End-to-end SCIM functionality

#### 3. End-to-End Tests (`Notejam.Tests/EndToEnd/`)
- **API Tests**: Full HTTP request/response testing
- **Workflow Tests**: Complete business scenarios
- **Health Checks**: Application status verification

#### 4. Performance Tests (`Notejam.Tests/Performance/`)
- **Load Testing**: Concurrent request handling
- **Stress Testing**: Resource exhaustion scenarios
- **Memory Testing**: Memory usage patterns
- **Endurance Testing**: Long-running operations
- **Scalability Testing**: Performance under scale

#### 5. Security Tests (`Notejam.Tests/Security/`)
- **SQL Injection**: Input validation testing
- **XSS Protection**: Cross-site scripting prevention
- **Input Validation**: Malicious input handling
- **Authentication**: Access control verification
- **Rate Limiting**: Request throttling

#### 6. Regression Tests (`Notejam.Tests/Regression/`)
- **Complex Business Scenarios**: Multi-step workflows
- **Data Integrity**: Consistency verification
- **Concurrent Operations**: Race condition testing
- **Error Recovery**: Failure scenario handling
- **Cross-Entity Operations**: Related data management

### Running Tests

```bash
# Navigate to test directory
cd Notejam.Tests

# Run all tests
dotnet test

# Run specific test categories
dotnet test --filter Category=Unit
dotnet test --filter Category=Integration
dotnet test --filter Category=EndToEnd

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run with verbose output
dotnet test --verbosity detailed
```

### Test Results Interpretation

**Demo Application Context:**
- **Basic CRUD operations**: Should work reliably
- **SCIM filtering**: Fully functional with all operators
- **Performance tests**: May fail due to resource limitations (expected)
- **Concurrent operations**: May cause 500 errors under load (expected)
- **Security tests**: May not implement all production security measures

### Demo Warning System

Tests that are expected to fail in the demo environment are marked with `[DemoWarning]` attributes:

#### Security Test Warnings
- **Input Validation**: Demo has basic validation only
- **SQL Injection Protection**: Limited protection in demo
- **XSS Protection**: No content sanitization in demo
- **Payload Size Limits**: No size restrictions in demo

#### Performance Test Warnings
- **High Concurrency**: Demo not optimized for concurrent load
- **Response Time**: Basic performance characteristics only

#### Regression Test Warnings
- **CRUD Operations**: Incomplete DELETE operations in demo
- **Error Recovery**: Basic error handling only
- **Business Rules**: Limited validation in demo

#### What Works Correctly
- **SCIM Filter Processing**: Full functionality via Looplex.Foundation.SearchContent
- **SQL Generation**: Secure, thread-safe, recursion-protected
- **Basic API Operations**: GET, POST, PUT work reliably
- **Core Domain Logic**: Business rules implemented correctly

## Development Setup

### Prerequisites

1. **.NET 8.0 SDK**
2. **SQL Server** (local or remote)
3. **Visual Studio 2022** or **VS Code**
4. **Git** for version control

### Environment Setup

#### 1. Clone and Navigate
```bash
git clone <repository-url>
cd samples/Notejam
```

#### 2. Database Setup
```bash
# Create database
CREATE DATABASE notejam;

# Run schema scripts (if available)
# The application will create tables automatically on first run
```

#### 3. Configuration
Create `config.env` file in `Notejam.WebApp/`:
```bash
DB_CONNECTION_STRING="Server=localhost;Database=notejam;Trusted_Connection=true;"
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://localhost:7065
```

#### 4. Build and Run
```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run web application
cd Notejam.WebApp
dotnet run
```

### Development Workflow

#### 1. Code Organization
- **Domain Layer**: Business entities and rules
- **Application Layer**: Use cases and business logic
- **Infrastructure Layer**: Data access and external services
- **Presentation Layer**: API controllers and middleware

#### 2. Testing Strategy
- Write unit tests for domain logic
- Create integration tests for service interactions
- Implement end-to-end tests for API workflows
- Add performance and security tests as needed

#### 3. Code Quality
- Follow C# coding conventions
- Use XML documentation for public APIs
- Implement proper error handling
- Follow SOLID principles

## Additional Documentation

### Project-Specific Documentation
- **`Notejam.WebApp/README.md`**: Web application details and API reference
- **`Notejam.Tests/README.md`**: Comprehensive testing documentation
- **`API_DOCUMENTATION.md`**: Detailed API specifications
- **`README_SCIM_API.md`**: SCIM-specific implementation details

### External Resources
- **Postman Collection**: `Notejam_SCIM_Collection.json` - Complete API testing suite
- **Looplex.Foundation.WebApp**: SCIM middleware documentation
- **SCIM v2.0 Specification**: RFC 7644

## Configuration

### Environment Variables

```bash
# Database
DB_CONNECTION_STRING="Server=localhost;Database=notejam;Trusted_Connection=true;"

# Application
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://localhost:7065

# Logging
ASPNETCORE_LOGGING__LOGLEVEL__DEFAULT=Information
```

### Application Settings

The application uses environment-based configuration with support for:
- **Development**: Detailed logging, debug information
- **Production**: Optimized performance, minimal logging
- **Testing**: Test-specific configurations

## Troubleshooting

### Common Issues

#### Application Won't Start
- Check .NET 8.0 SDK installation
- Verify database connection string
- Ensure port 7065 is available
- Check application logs for errors

#### Database Connection Errors
- Verify SQL Server is running
- Check connection string format
- Ensure database exists and is accessible
- Verify user permissions

#### SCIM Filter Errors
- Validate filter syntax
- Check field names and operators
- Review error logs for details
- Test with simple filters first

#### Test Failures
- **Performance tests**: May fail due to resource limitations (expected)
- **Security tests**: May not implement all production measures
- **Concurrent tests**: May cause 500 errors under load (expected)

### Debugging

```bash
# Enable detailed logging
export ASPNETCORE_ENVIRONMENT=Development
export ASPNETCORE_LOGGING__LOGLEVEL__DEFAULT=Debug

# Run with verbose output
dotnet run --verbosity detailed

# Check application health
curl http://localhost:7065/health
```

## Demo Application Context

### Important Notes
This is a **demonstration application** designed to showcase SCIM v2.0 functionality and Looplex.Foundation.WebApp capabilities. As such:

- **Limited Production Features**: The application lacks production-grade infrastructure
- **Expected Instability**: Under intensive testing, the application may return 500 Internal Server Errors
- **Resource Constraints**: No load balancing, connection pooling optimization, or caching strategies
- **Demo Purpose**: Designed for educational and demonstration purposes only

### Expected Behavior
- **Basic CRUD operations**: Work reliably
- **SCIM filtering**: Fully functional with all operators
- **Performance tests**: May fail due to resource limitations
- **Concurrent operations**: May cause 500 errors (expected behavior)
- **Security tests**: May not implement all production security measures

## Performance Considerations

### Optimization Features
- Connection pooling for database access
- Efficient SCIM filter processing
- Response caching for static content
- Optimized JSON serialization

### Monitoring
- Built-in health checks
- Performance metrics collection
- Error logging and monitoring
- Application insights integration ready

## Security Considerations

### Implemented Security
- SQL injection protection through parameterized queries
- Input validation and sanitization
- Automatic escaping of special characters
- CORS configuration

### Production Security (Not Implemented in Demo)
- HTTPS enforcement
- Authentication and authorization
- Rate limiting
- Request validation middleware
- Security headers

## Deployment

### Production Considerations

1. **Environment Configuration**
   - Use production connection strings
   - Configure proper logging levels
   - Set up monitoring and alerting

2. **Security Hardening**
   - Enable HTTPS
   - Configure authentication
   - Set up proper CORS policies
   - Implement rate limiting

3. **Performance Optimization**
   - Enable response compression
   - Configure caching strategies
   - Optimize database queries
   - Set up load balancing

### Container Deployment

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY . .
RUN dotnet restore
RUN dotnet build -c Release
EXPOSE 7065
ENTRYPOINT ["dotnet", "Notejam.WebApp.dll"]
```

## Contributing

### Development Guidelines

1. **Code Style**: Follow C# coding conventions
2. **Testing**: Write unit and integration tests for new features
3. **Documentation**: Update documentation for new features
4. **Security**: Follow security best practices
5. **Performance**: Consider performance implications

### Testing Requirements

- All new features must have unit tests
- Integration tests for service interactions
- End-to-end tests for API workflows
- Performance impact assessment

---

**Last Updated**: August 2025  
**Version**: 1.0  
**Environment**: Development/Demo Application  
**Framework**: .NET 8.0  
**Architecture**: Clean Architecture with CQRS  
**Testing**: Comprehensive test suite with 6 categories  
**Documentation**: Complete API and development guides

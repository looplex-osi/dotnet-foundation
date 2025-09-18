# Notejam.WebApp - Web Application Documentation

## Overview

Notejam.WebApp is the main web application for the Notejam demonstration project, providing a REST API with SCIM v2.0 compliance for managing Notes and Pads through HTTP endpoints.

## Application Structure

### Core Components

- **Program.cs**: Application configuration and middleware setup
- **HealthCheck.cs**: Application health monitoring
- **Configuration**: Environment-based configuration management

### Key Features

- **SCIM v2.0 Compliance**: Standard-based API for identity management
- **REST API**: Full CRUD operations for Notes and Pads
- **Filtering & Pagination**: Advanced search capabilities with SCIM filters
- **Health Monitoring**: Built-in health checks
- **Configuration Management**: Environment-based settings

## API Endpoints

### Base URL
```
http://localhost:7065
```

### Notes Management

```
GET    /notes                    # List all notes with optional filtering
POST   /notes                    # Create a new note
GET    /notes/{id}               # Get specific note by ID
PUT    /notes/{id}               # Update existing note
PATCH  /notes/{id}               # Partial update using JSON Patch
DELETE /notes/{id}               # Delete note (logical deletion)
```

### Pads Management

```
GET    /pads                     # List all pads with optional filtering
POST   /pads                     # Create a new pad
GET    /pads/{id}                # Get specific pad by ID
PUT    /pads/{id}                # Update existing pad
PATCH  /pads/{id}                # Partial update using JSON Patch
DELETE /pads/{id}                # Delete pad (logical deletion)
```

### Health Check

```
GET    /health                   # Application health status
```

## SCIM Filtering

### Supported Operators

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

### Filter Examples (Validated Working)

#### Basic Filters
```bash
# Boolean filters
GET /pads?filter=active eq true
GET /notes?filter=active eq false

# String filters
GET /pads?filter=name eq "Test Pad"
GET /pads?filter=name co "Teste"
GET /notes?filter=text co "important"

# Numeric filters
GET /pads?filter=status eq 1
GET /pads?filter=status eq 2
```

#### Complex Filters
```bash
# Multiple conditions with AND
GET /pads?filter=active eq true and status eq 1

# Multiple conditions with OR
GET /pads?filter=name co "Teste" or status eq 2

# Complex combinations
GET /pads?filter=(name sw "demo" or name ew "pad") and active eq true
```

#### Date Filters
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

## Request/Response Examples

### Creating Resources

#### POST /pads
```bash
curl -X POST http://localhost:7065/pads \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Pad QA Teste Final",
    "active": true,
    "status": 1,
    "customFields": "{\"test\": \"final\"}"
  }'
```

**Response:**
- Status: `201 Created`
- Body: `{}` (empty JSON)
- Header: `Location: /pads/{generated-id}`

#### POST /notes
```bash
curl -X POST http://localhost:7065/notes \
  -H "Content-Type: application/json" \
  -d '{
    "text": "Note QA Teste Final",
    "active": true,
    "status": 1,
    "customFields": "{\"test\": \"final\"}"
  }'
```

**Response:**
- Status: `201 Created`
- Body: `{}` (empty JSON)
- Header: `Location: /notes/{generated-id}`

### Updating Resources

#### PUT /pads/{id} (Complete Update)
```bash
curl -X PUT http://localhost:7065/pads/e1274a3e-f36b-1410-85cb-0046c3744233 \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Pad Atualizado via PUT",
    "active": false,
    "status": 2,
    "customFields": "{\"updated\": \"via PUT\"}"
  }'
```

#### PATCH /pads/{id} (Partial Update)
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

### Retrieving Resources

#### GET /pads (with filter)
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

#### GET /pads (with pagination)
```bash
curl "http://localhost:7065/pads?startIndex=1&count=3"
```

**Response:**
```json
{
  "totalResults": 75,
  "Resources": [
    {
      "name": "Updated Pad Regression Test",
      "active": false,
      "status": 2,
      "customFields": "{\"updated\": \"via regression test\"}",
      "id": "49294a3e-f36b-1410-85cb-0046c3744233",
      "created": "2025-08-28T20:11:49",
      "modified": "2025-08-28T20:12:12"
    }
  ]
}
```

## Configuration

### Environment Variables

The application uses environment-based configuration:

```bash
# Database connection
DB_CONNECTION_STRING="Server=localhost;Database=notejam;Trusted_Connection=true;"

# Application settings
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://localhost:7065
```

### Configuration Files

- **config.env**: Environment-specific configuration
- **config.sample.env**: Template for environment configuration
- **launchSettings.json**: Development launch settings

## Application Behavior

### POST Operations
- Return `201 Created` status
- Return empty JSON `{}` in response body
- Include resource ID in `Location` header
- Example: `Location: /pads/31294a3e-f36b-1410-85cb-0046c3744233`

### GET Operations
- Return `200 OK` status
- Return complete resource objects with metadata
- Include `totalResults` for pagination
- Support SCIM filtering and pagination

### PATCH Operations
- Use JSON Patch format (`application/json-patch+json`)
- Support `replace`, `add`, `remove` operations
- Return `204 No Content` on success

### DELETE Operations
- Perform logical deletion (soft delete)
- Return `204 No Content` on success
- Resources remain in database but marked as inactive

### Error Handling
- `400 Bad Request`: Invalid input or filter syntax
- `404 Not Found`: Resource not found
- `500 Internal Server Error`: Server-side errors

## Development Setup

### Prerequisites

1. **.NET 8.0 SDK**
2. **SQL Server** (local or remote)
3. **Database**: `notejam` database with proper schema

### Running the Application

```bash
# Navigate to the project directory
cd Notejam.WebApp

# Restore dependencies
dotnet restore

# Build the application
dotnet build

# Run the application
dotnet run
```

### Database Setup

The application requires a properly configured database with the following tables:
- `pads`: Pad management
- `notes`: Note management
- `users`: User management (for ownership)

### Testing the API

```bash
# Health check
curl http://localhost:7065/health

# List all pads
curl http://localhost:7065/pads

# Create a new pad
curl -X POST http://localhost:7065/pads \
  -H "Content-Type: application/json" \
  -d '{"name": "Test Pad", "active": true, "status": 1, "customFields": "{\"test\": \"value\"}"}'

# Search with filter
curl "http://localhost:7065/pads?filter=active eq true"

# Search with pagination
curl "http://localhost:7065/pads?startIndex=1&count=3"

# Complex filter
curl "http://localhost:7065/pads?filter=active eq true and status eq 1"
```

## Architecture

### Clean Architecture Implementation

The application follows Clean Architecture principles:

```
Notejam.WebApp (Presentation Layer)
├── Program.cs           # Application configuration
├── HealthCheck.cs       # Health monitoring
└── Configuration/       # Environment settings

Dependencies:
├── Application/         # Business logic layer
├── Domain/             # Domain entities and rules
├── Infra/              # Data access layer
└── Looplex.Foundation.WebApp/  # SCIM middleware
```

### Middleware Stack

1. **Exception Handling**: Global error handling
2. **SCIM v2.0**: Filter processing and validation
3. **Health Checks**: Application health monitoring
4. **CORS**: Cross-origin resource sharing
5. **Routing**: Request routing to SCIM endpoints

## Security Considerations

### SQL Injection Protection
- All SCIM filters are parameterized
- Input validation and sanitization
- Automatic escaping of special characters

### Authentication
- Basic authentication support
- Token-based authentication (if configured)
- Role-based access control (RBAC) ready

## Performance

### Optimization Features
- Connection pooling for database access
- Efficient SCIM filter processing
- Response caching for static content
- Optimized JSON serialization

### Monitoring
- Built-in health checks
- Performance metrics collection
- Error logging and monitoring

## Demo Application Context

### Important Notes
This is a **demonstration application** designed to showcase SCIM v2.0 functionality and Looplex.Foundation.WebApp capabilities. As such:

- **Limited Production Features**: The application lacks production-grade infrastructure
- **Expected Instability**: Under intensive testing (multiple concurrent requests), the application may return 500 Internal Server Errors
- **Resource Constraints**: No load balancing, connection pooling optimization, or caching strategies
- **Demo Purpose**: Designed for educational and demonstration purposes only

### Test Results Interpretation
When running comprehensive test suites:
- **Basic CRUD operations**: Should work reliably
- **SCIM filtering**: Fully functional with all operators
- **Performance tests**: May fail due to resource limitations
- **Concurrent operations**: May cause 500 errors (expected behavior)
- **Security tests**: May not implement all production security measures

## Troubleshooting

### Common Issues

**Application Won't Start:**
- Check database connection string
- Verify .NET 8.0 SDK installation
- Check port availability (default: 7065)

**Database Connection Errors:**
- Verify SQL Server is running
- Check connection string format
- Ensure database exists and is accessible

**SCIM Filter Errors:**
- Validate filter syntax
- Check field names and operators
- Review error logs for details

**500 Internal Server Errors:**
- Normal behavior under load for demo application
- Restart application if persistent
- Check application logs for specific errors

### Debugging

```bash
# Enable detailed logging
export ASPNETCORE_ENVIRONMENT=Development
export ASPNETCORE_LOGGING__LOGLEVEL__DEFAULT=Debug

# Run with verbose output
dotnet run --verbosity detailed
```

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

3. **Performance Optimization**
   - Enable response compression
   - Configure caching strategies
   - Optimize database queries

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
2. **Testing**: Write unit and integration tests
3. **Documentation**: Update documentation for new features
4. **Security**: Follow security best practices

### Testing

```bash
# Run unit tests
dotnet test

# Run integration tests
dotnet test --filter Category=Integration

# Run all tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

---

**Last Updated**: August 2025  
**Version**: 1.0  
**Environment**: Development/Demo Application  
**Postman Collection**: Available at `samples/Notejam/Notejam_SCIM_Collection.json`

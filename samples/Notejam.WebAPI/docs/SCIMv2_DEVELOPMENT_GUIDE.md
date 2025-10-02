# 🚀 SCIMv2 Development Guide - Building Applications with Looplex.Foundation

## 📋 Table of Contents

1. [Overview](#overview)
2. [Prerequisites](#prerequisites)
3. [Architecture Overview](#architecture-overview)
4. [Step-by-Step Implementation](#step-by-step-implementation)
5. [Configuration Guide](#configuration-guide)
6. [Testing & Validation](#testing--validation)
7. [Troubleshooting](#troubleshooting)
8. [Best Practices](#best-practices)
9. [Examples](#examples)

---

## 🎯 Overview

This guide demonstrates how to build SCIMv2-compliant applications using **Looplex.Foundation**, using Notejam as a practical example. SCIMv2 (System for Cross-domain Identity Management) is a standard protocol for managing user identities across different systems.

### What You'll Learn
- How to configure Looplex.Foundation for SCIMv2
- How to implement resource repositories
- How to create stored procedures
- How to test SCIMv2 endpoints
- How to handle SCIMv2 filters and operations

---

## 🔧 Prerequisites

### Required Knowledge
- **.NET 8** and ASP.NET Core
- **C#** (Intermediate level)
- **SQL Server** and stored procedures
- **HTTP/REST** concepts
- **JSON** data format
- **SCIMv2** basics (RFC 7644)

### Required Tools
- Visual Studio 2022 or VS Code
- SQL Server (LocalDB or full instance)
- .NET 8 SDK
- Git

### Required Experience Level
- **Pleno (3-5 years)** - Ideal
- **Sênior (5+ years)** - Excellent
- **Júnior (1-2 years)** - Challenging but possible with mentorship

---

## 🏗️ Architecture Overview

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    SCIMv2 Client                            │
└─────────────────────┬───────────────────────────────────────┘
                      │ HTTP/JSON
┌─────────────────────▼───────────────────────────────────────┐
│                ASP.NET Core Web API                        │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │   SCIMv2        │  │   Resource      │  │   Health    │ │
│  │   Middleware    │  │   Services      │  │   Checks    │ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
└─────────────────────┬───────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────┐
│                Looplex.Foundation                         │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │   SCIMv2        │  │   Filter        │  │   Schema    │ │
│  │   Core          │  │   Processing    │  │   Validation│ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
└─────────────────────┬───────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────┐
│                Repository Layer                            │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │   Note          │  │   Pad           │  │   Custom    │ │
│  │   Repository    │  │   Repository    │  │   Repository│ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
└─────────────────────┬───────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────┐
│                SQL Server Database                         │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │   Stored        │  │   Tables        │  │   Indexes    │ │
│  │   Procedures    │  │   (notes, pads) │  │   & Views    │ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### Key Components

1. **SCIMv2 Middleware** - Handles HTTP requests/responses
2. **Resource Services** - Business logic layer
3. **Repositories** - Data access layer
4. **Stored Procedures** - Database operations
5. **Foundation Core** - SCIMv2 protocol implementation

---

## 🛠️ Step-by-Step Implementation

### Step 1: Project Setup

#### 1.1 Create New Project
```bash
dotnet new webapi -n MySCIMv2App
cd MySCIMv2App
```

#### 1.2 Add Looplex.Foundation Packages
```xml
<PackageReference Include="Looplex.Foundation" Version="1.0.0" />
<PackageReference Include="Looplex.Foundation.Protocols.Http" Version="1.0.0" />
```

#### 1.3 Configure Program.cs
```csharp
using Looplex.Foundation.SCIMv2;
using Looplex.Foundation.Protocols.Http.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// Add required services
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IDbConnections, DbConnections>();

// Configure SCIMv2 BEFORE creating instances
ConfigureSCIMv2();

// Register SCIMv2 services
builder.Services.AddSingleton<ISCIMv2, Looplex.Foundation.SCIMv2.SCIMv2>();
builder.Services.AddSingleton<ISCIMv2Validation, Looplex.Foundation.SCIMv2.SCIMv2>();

var app = builder.Build();

// Use SCIMv2 middleware
app.UseSCIMv2Discovery(authorize: false);
app.UseSCIMv2("myresources", authorize: false);

app.Run();
```

### Step 2: Configure SCIMv2 Resources

#### 2.1 Define Your Resource Entity
```csharp
public class MyResource
{
    public string Id { get; set; }
    public string ExternalId { get; set; }
    public string Name { get; set; }
    public bool Active { get; set; }
    public int Status { get; set; }
    public string[] Schemas { get; set; }
    public ResourceMeta Meta { get; set; }
}
```

#### 2.2 Configure SCIMv2 Attributes and Mappings
```csharp
private static void ConfigureSCIMv2()
{
    // Configure attributes for your resource
    Looplex.Foundation.SCIMv2.SCIMv2.ConfigureAttributes("MyResource", new HashSet<string> { 
        "id", "externalId", "name", "active", "status", 
        "meta.created", "meta.lastModified" 
    });
    
    // Configure attribute mappings (SCIM → Database)
    Looplex.Foundation.SCIMv2.SCIMv2.ConfigureMapping("MyResource", new Dictionary<string, string> {
        { "meta.created", "mr.created_at" },
        { "meta.lastModified", "mr.updated_at" },
        { "active", "mr.active" },
        { "name", "mr.name" },
        { "status", "mr.status" },
        { "id", "mr.id" },
        { "externalId", "mr.external_id" }
    });
}
```

### Step 3: Implement Repository Pattern

#### 3.1 Create Repository Interface
```csharp
public interface IMyResourceRepository
{
    Task<(List<MyResource> Resources, int TotalCount)> QueryAsync(
        int startIndex, int count, string? filter, CancellationToken cancellationToken = default);
    Task<MyResource?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<MyResource> CreateAsync(MyResource resource, CancellationToken cancellationToken = default);
    Task<MyResource> UpdateAsync(string id, MyResource resource, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
```

#### 3.2 Implement Repository with Stored Procedures
```csharp
public class MyResourceRepositoryStoredProcedure : IMyResourceRepository, IResourceRepository<MyResource>
{
    private readonly IDbConnections _connections;
    private readonly ILogger<MyResourceRepositoryStoredProcedure> _logger;

    public MyResourceRepositoryStoredProcedure(
        IDbConnections connections, 
        ILogger<MyResourceRepositoryStoredProcedure> logger)
    {
        _connections = connections;
        _logger = logger;
    }

    public async Task<(List<MyResource> Resources, int TotalCount)> QueryAsync(
        int startIndex, int count, string? filter, CancellationToken cancellationToken = default)
    {
        try
        {
            // Convert SCIM pagination to page-based
            var page = (startIndex - 1) / count + 1;
            var pageSize = count;

            // Configure allowed attributes and mappings
            var allowedAttributes = new HashSet<string> { 
                "id", "externalId", "name", "active", "status", 
                "meta.created", "meta.lastModified" 
            };
            
            var attributeMapper = new Dictionary<string, string> {
                { "meta.created", "mr.created_at" },
                { "meta.lastModified", "mr.updated_at" },
                { "active", "mr.active" },
                { "name", "mr.name" },
                { "status", "mr.status" },
                { "id", "mr.id" },
                { "externalId", "mr.external_id" }
            };
            
            // Convert SCIM filter to SQL predicate
            string? filters = filter?.ToSqlPredicate(attributeMapper, allowedAttributes);

            await using var dbCommand = await _connections.CommandConnection();
            await using var command = dbCommand.CreateCommand();

            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "USP_myresources_pquery";

            // Add parameters
            command.Parameters.Add(Dbs.CreateParameter(command, "@page", page, DbType.Int32));
            command.Parameters.Add(Dbs.CreateParameter(command, "@page_size", pageSize, DbType.Int32));
            command.Parameters.Add(Dbs.CreateParameter(command, "@do_count", true, DbType.Boolean));
            command.Parameters.Add(Dbs.CreateParameter(command, "@order_by", "updated_at DESC", DbType.String));
            
            // Add filter if provided
            if (filters != null)
                command.Parameters.Add(Dbs.CreateParameter(command, "@__dangerouslySetPredicate", filters, DbType.String));

            var (resources, totalCount) = await ExecuteStoredProcedureWithCount((SqlCommand)command, cancellationToken);
            return (resources, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying resources");
            throw new InvalidOperationException($"Failed to query resources: {ex.Message}", ex);
        }
    }

    // Implement other methods...
    public async Task<MyResource?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        // Implementation for retrieving single resource
    }

    public async Task<MyResource> CreateAsync(MyResource resource, CancellationToken cancellationToken = default)
    {
        // Implementation for creating resource
    }

    public async Task<MyResource> UpdateAsync(string id, MyResource resource, CancellationToken cancellationToken = default)
    {
        // Implementation for updating resource
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        // Implementation for deleting resource
    }

    private async Task<(List<MyResource> Resources, int TotalCount)> ExecuteStoredProcedureWithCount(
        SqlCommand command, CancellationToken cancellationToken)
    {
        var resources = new List<MyResource>();
        int totalCount = 0;

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        // First result set: paginated data
        while (await reader.ReadAsync(cancellationToken))
        {
            var resource = MapReaderToResource(reader);
            resources.Add(resource);
        }

        // Second result set: total count
        if (await reader.NextResultAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                totalCount = reader.GetInt32("total");
            }
        }

        return (resources, totalCount);
    }

    private MyResource MapReaderToResource(IDataReader reader)
    {
        var resource = new MyResource();
        
        resource.Id = reader["uuid"].ToString();
        
        if (reader["external_id"] != DBNull.Value)
            resource.ExternalId = reader["external_id"].ToString();
        
        resource.Name = ScimTypeConverter.ParseString(reader["name"]);
        resource.Active = ScimTypeConverter.ParseBoolean(reader["active"]);
        resource.Status = ScimTypeConverter.ParseInteger(reader["status"]);
        resource.Schemas = new[] { "urn:looplex:params:scim:schemas:myapp:2.0:MyResource" };
        
        // Create Meta using Foundation method
        resource.Meta = Looplex.Foundation.SCIMv2.SCIMv2.CreateResourceMeta(reader, resource.Id, "MyResource", _httpContextAccessor);
        
        return resource;
    }
}
```

### Step 4: Create Stored Procedures

#### 4.1 Paginated Query Procedure
```sql
CREATE PROCEDURE USP_myresources_pquery
    @page INT = 1,
    @page_size INT = 10,
    @do_count BIT = 0,
    @order_by NVARCHAR(100) = 'updated_at DESC',
    @__dangerouslySetPredicate NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @offset INT = (@page - 1) * @page_size;
    
    -- Main query with pagination
    SELECT 
        mr.uuid,
        mr.external_id,
        mr.name,
        mr.active,
        mr.status,
        mr.created_at,
        mr.updated_at
    FROM myresources mr
    WHERE (@__dangerouslySetPredicate IS NULL OR 
           (@__dangerouslySetPredicate IS NOT NULL AND 
            EXISTS (SELECT 1 WHERE @__dangerouslySetPredicate LIKE '%' + CAST(mr.uuid AS NVARCHAR(36)) + '%')))
    ORDER BY 
        CASE WHEN @order_by LIKE '%created_at%' AND @order_by LIKE '%ASC%' THEN mr.created_at END ASC,
        CASE WHEN @order_by LIKE '%created_at%' AND @order_by LIKE '%DESC%' THEN mr.created_at END DESC,
        CASE WHEN @order_by LIKE '%updated_at%' AND @order_by LIKE '%ASC%' THEN mr.updated_at END ASC,
        CASE WHEN @order_by LIKE '%updated_at%' AND @order_by LIKE '%DESC%' THEN mr.updated_at END DESC
    OFFSET @offset ROWS
    FETCH NEXT @page_size ROWS ONLY;
    
    -- Count query if requested
    IF @do_count = 1
    BEGIN
        SELECT COUNT(*) as total
        FROM myresources mr
        WHERE (@__dangerouslySetPredicate IS NULL OR 
               (@__dangerouslySetPredicate IS NOT NULL AND 
                EXISTS (SELECT 1 WHERE @__dangerouslySetPredicate LIKE '%' + CAST(mr.uuid AS NVARCHAR(36)) + '%')));
    END
END
```

#### 4.2 Other Required Procedures
```sql
-- Retrieve single resource
CREATE PROCEDURE USP_myresources_retrieve
    @filter_uuid UNIQUEIDENTIFIER
AS
BEGIN
    SELECT 
        mr.uuid,
        mr.external_id,
        mr.name,
        mr.active,
        mr.status,
        mr.created_at,
        mr.updated_at
    FROM myresources mr
    WHERE mr.uuid = @filter_uuid;
END

-- Create new resource
CREATE PROCEDURE USP_myresources_create
    @name NVARCHAR(255),
    @active BIT = 1,
    @status INT = 1,
    @external_id NVARCHAR(255) = NULL,
    @created_by NVARCHAR(100) = 'system'
AS
BEGIN
    DECLARE @new_uuid UNIQUEIDENTIFIER = NEWID();
    
    INSERT INTO myresources (uuid, external_id, name, active, status, created_at, updated_at, created_by)
    VALUES (@new_uuid, @external_id, @name, @active, @status, GETUTCDATE(), GETUTCDATE(), @created_by);
    
    SELECT @new_uuid as uuid;
END

-- Update existing resource
CREATE PROCEDURE USP_myresources_update
    @resource_guid UNIQUEIDENTIFIER,
    @name NVARCHAR(255),
    @active BIT,
    @status INT
AS
BEGIN
    UPDATE myresources 
    SET 
        name = @name,
        active = @active,
        status = @status,
        updated_at = GETUTCDATE()
    WHERE uuid = @resource_guid;
    
    SELECT @@ROWCOUNT as rows_affected;
END
```

### Step 5: Create Resource Service

#### 5.1 Implement Resource Service
```csharp
public class SCIMv2MyResourceService : BaseResourceService<MyResource>
{
    private readonly ILogger<SCIMv2MyResourceService> _logger;

    public SCIMv2MyResourceService(
        IResourceRepository<MyResource> repository, 
        ILogger<SCIMv2MyResourceService> logger) : base(repository)
    {
        _logger = logger;
    }

    public override string CollectionName => "myresources";

    public override async Task<Guid> Create(MyResource resource, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating MyResource: {Name}", resource.Name);
        
        try
        {
            var result = await base.Create(resource, cancellationToken);
            _logger.LogInformation("MyResource created successfully with ID: {Id}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create MyResource: {Name}", resource.Name);
            throw;
        }
    }

    // Implement other required methods...
}
```

### Step 6: Register Services

#### 6.1 Update Program.cs
```csharp
// Register repositories
builder.Services.AddSingleton<IMyResourceRepository, MyResourceRepositoryStoredProcedure>();
builder.Services.AddSingleton<IResourceRepository<MyResource>, MyResourceRepositoryStoredProcedure>();

// Register resource services
builder.Services.AddSingleton<IResourceService<MyResource>, SCIMv2MyResourceService>();

// Register SCIMv2 resources
using (var scope = app.Services.CreateScope())
{
    var scimService = scope.ServiceProvider.GetRequiredService<ISCIMv2>();
    var myResourceService = scope.ServiceProvider.GetRequiredService<IResourceService<MyResource>>();
    
    scimService.Register<MyResource>(myResourceService, "myresources");
}

// Use SCIMv2 middleware
app.UseSCIMv2("myresources", authorize: false);
```

---

## ⚙️ Configuration Guide

### SCIMv2 Attribute Configuration

#### Allowed Attributes
```csharp
// Define which SCIM attributes can be used in filters
var allowedAttributes = new HashSet<string> { 
    "id",           // Resource identifier
    "externalId",   // External system identifier
    "name",         // Resource name
    "active",       // Active status
    "status",       // Status code
    "meta.created", // Creation timestamp
    "meta.lastModified" // Last modification timestamp
};
```

#### Attribute Mapping
```csharp
// Map SCIM attributes to database columns
var attributeMapper = new Dictionary<string, string> {
    { "meta.created", "mr.created_at" },        // SCIM → Database
    { "meta.lastModified", "mr.updated_at" },    // SCIM → Database
    { "active", "mr.active" },                   // SCIM → Database
    { "name", "mr.name" },                       // SCIM → Database
    { "status", "mr.status" },                   // SCIM → Database
    { "id", "mr.id" },                           // SCIM → Database
    { "externalId", "mr.external_id" }          // SCIM → Database
};
```

### Database Schema

#### Required Table Structure
```sql
CREATE TABLE myresources (
    id INT IDENTITY(1,1) PRIMARY KEY,
    uuid UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    external_id NVARCHAR(255) NULL,
    name NVARCHAR(255) NOT NULL,
    active BIT NOT NULL DEFAULT 1,
    status INT NOT NULL DEFAULT 1,
    created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    updated_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    created_by NVARCHAR(100) NOT NULL DEFAULT 'system'
);

CREATE UNIQUE INDEX IX_myresources_uuid ON myresources(uuid);
CREATE INDEX IX_myresources_external_id ON myresources(external_id);
CREATE INDEX IX_myresources_active ON myresources(active);
CREATE INDEX IX_myresources_status ON myresources(status);
```

---

## 🧪 Testing & Validation

### Manual Testing

#### 1. Basic CRUD Operations
```bash
# Create resource
curl -X POST http://localhost:5000/myresources \
  -H "Content-Type: application/scim+json" \
  -d '{
    "schemas": ["urn:looplex:params:scim:schemas:myapp:2.0:MyResource"],
    "name": "Test Resource",
    "active": true,
    "status": 1
  }'

# Get all resources
curl -X GET http://localhost:5000/myresources \
  -H "Accept: application/scim+json"

# Get specific resource
curl -X GET http://localhost:5000/myresources/{id} \
  -H "Accept: application/scim+json"

# Update resource
curl -X PUT http://localhost:5000/myresources/{id} \
  -H "Content-Type: application/scim+json" \
  -d '{
    "schemas": ["urn:looplex:params:scim:schemas:myapp:2.0:MyResource"],
    "id": "{id}",
    "name": "Updated Resource",
    "active": true,
    "status": 2
  }'

# Delete resource
curl -X DELETE http://localhost:5000/myresources/{id} \
  -H "Accept: application/scim+json"
```

#### 2. Filter Operations
```bash
# Simple filters
curl -X GET "http://localhost:5000/myresources?filter=active eq true" \
  -H "Accept: application/scim+json"

curl -X GET "http://localhost:5000/myresources?filter=status gt 0" \
  -H "Accept: application/scim+json"

# Complex filters
curl -X GET "http://localhost:5000/myresources?filter=(active eq true) and (status gt 0)" \
  -H "Accept: application/scim+json"

# Pagination
curl -X GET "http://localhost:5000/myresources?startIndex=1&count=5" \
  -H "Accept: application/scim+json"

# Sorting
curl -X GET "http://localhost:5000/myresources?sortBy=meta.lastModified&sortOrder=descending" \
  -H "Accept: application/scim+json"
```

#### 3. Discovery Endpoints
```bash
# Service provider configuration
curl -X GET http://localhost:5000/ServiceProviderConfig \
  -H "Accept: application/scim+json"

# Resource types
curl -X GET http://localhost:5000/ResourceTypes \
  -H "Accept: application/scim+json"

# Schemas
curl -X GET http://localhost:5000/Schemas \
  -H "Accept: application/scim+json"
```

### Automated Testing

#### PowerShell Test Script
```powershell
# comprehensive_scimv2_tests.ps1
$baseUrl = "http://localhost:5000"
$headers = @{"Accept"="application/scim+json"}

Write-Host "=== SCIMv2 Testing Suite ===" -ForegroundColor Cyan

# Test basic operations
Write-Host "Testing GET /myresources" -ForegroundColor Yellow
try { 
    $response = Invoke-WebRequest -Uri "$baseUrl/myresources" -Headers $headers -UseBasicParsing
    Write-Host "Status: $($response.StatusCode)" -ForegroundColor Green
} catch { 
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Test resource creation
Write-Host "Testing POST /myresources" -ForegroundColor Yellow
$body = @{
    schemas = @("urn:looplex:params:scim:schemas:myapp:2.0:MyResource")
    name = "Test Resource"
    active = $true
    status = 1
} | ConvertTo-Json

try { 
    $response = Invoke-WebRequest -Uri "$baseUrl/myresources" -Method POST -Body $body -ContentType "application/scim+json" -Headers $headers -UseBasicParsing
    Write-Host "Status: $($response.StatusCode)" -ForegroundColor Green
    $content = [System.Text.Encoding]::UTF8.GetString($response.Content)
    $resource = $content | ConvertFrom-Json
    Write-Host "Created Resource ID: $($resource.id)" -ForegroundColor Cyan
} catch { 
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}
```

---

## 🔧 Troubleshooting

### Common Issues

#### 1. Configuration Errors
**Problem:** SCIMv2 attributes not working
**Solution:** Ensure configuration happens BEFORE service registration
```csharp
// ❌ Wrong order
builder.Services.AddSingleton<ISCIMv2, SCIMv2>();
ConfigureSCIMv2(); // Too late!

// ✅ Correct order
ConfigureSCIMv2(); // First
builder.Services.AddSingleton<ISCIMv2, SCIMv2>(); // Then
```

#### 2. Filter Parsing Errors
**Problem:** `Cannot filter by attribute` error
**Solution:** Add attribute to allowed attributes
```csharp
// Add missing attribute
var allowedAttributes = new HashSet<string> { 
    "id", "externalId", "name", "active", "status",
    "meta.created", "meta.lastModified",
    "customField" // Add this if needed
};
```

#### 3. Database Connection Issues
**Problem:** `Cannot connect to database`
**Solution:** Check connection string and database setup
```csharp
// Ensure proper connection string
builder.Services.AddSingleton<IDbConnections, DbConnections>();
```

#### 4. Stored Procedure Errors
**Problem:** `Procedure or function not found`
**Solution:** Ensure all required procedures exist
```sql
-- Required procedures:
-- USP_myresources_pquery (paginated query)
-- USP_myresources_retrieve (get by ID)
-- USP_myresources_create (create new)
-- USP_myresources_update (update existing)
-- USP_myresources_delete (delete)
```

#### 5. Delete Operations
**Problem:** `DELETE` operations not working
**Solution:** Implement proper delete stored procedures

**Soft Delete Implementation (Notes):**
```sql
-- Soft delete - mark as inactive
CREATE PROCEDURE USP_notes_delete
    @note_guid UNIQUEIDENTIFIER
AS
BEGIN
    UPDATE notes 
    SET active = 0, updated_at = GETUTCDATE()
    WHERE uuid = @note_guid;
    
    SELECT @@ROWCOUNT as rows_affected;
END
```

**Hard Delete Implementation (Pads):**
```sql
-- Hard delete - remove from database
CREATE PROCEDURE USP_pads_delete
    @pad_guid UNIQUEIDENTIFIER
AS
BEGIN
    DELETE FROM pads WHERE uuid = @pad_guid;
    
    SELECT @@ROWCOUNT as rows_affected;
END
```

**Repository Implementation:**
```csharp
public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
{
    try
    {
        await using var dbCommand = await _connections.CommandConnection();
        await using var command = dbCommand.CreateCommand();

        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = "USP_myresources_delete";
        
        // Convert string ID to GUID for stored procedure
        var resourceGuid = Guid.Parse(id);
        command.Parameters.Add(Dbs.CreateParameter(command, "@resource_guid", resourceGuid, DbType.Guid));

        var result = await command.ExecuteNonQueryAsync(cancellationToken);
        return result > 0;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error deleting resource: {Id}", id);
        throw new InvalidOperationException($"Failed to delete resource: {ex.Message}", ex);
    }
}
```

### Debug Tips

#### 1. Enable Detailed Logging
```csharp
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);
```

#### 2. Check SCIMv2 Configuration
```csharp
// Add this to verify configuration
Console.WriteLine("🔧 SCIMv2 Configuration:");
Console.WriteLine($"Allowed attributes: {string.Join(", ", allowedAttributes)}");
Console.WriteLine($"Attribute mappings: {string.Join(", ", attributeMapper.Select(kvp => $"{kvp.Key}→{kvp.Value}"))}");
```

#### 3. Test Individual Components
```csharp
// Test repository directly
var repository = serviceProvider.GetRequiredService<IMyResourceRepository>();
var (resources, count) = await repository.QueryAsync(1, 10, null, CancellationToken.None);
Console.WriteLine($"Found {count} resources");
```

---

## 🏆 Best Practices

### 1. Repository Pattern
- **Use stored procedures** for all database operations
- **Implement proper error handling** with try-catch blocks
- **Use parameterized queries** to prevent SQL injection
- **Map database results** to domain entities properly

### 2. SCIMv2 Configuration
- **Configure attributes early** in application startup
- **Use descriptive attribute names** that match your domain
- **Map SCIM attributes** to database columns consistently
- **Validate all configurations** before deployment

### 3. Error Handling
- **Log all errors** with appropriate detail levels
- **Return proper HTTP status codes** (200, 201, 400, 404, 500)
- **Provide meaningful error messages** for debugging
- **Handle edge cases** gracefully

### 4. Performance
- **Use pagination** for large result sets
- **Create proper database indexes** for filtered columns
- **Optimize stored procedures** for common queries
- **Cache frequently accessed data** when appropriate

### 5. Security
- **Validate all inputs** before processing
- **Use parameterized queries** to prevent SQL injection
- **Implement proper authentication** and authorization
- **Log security-related events** for monitoring

---

## 📚 Examples

### Complete Working Example

See the `samples/Notejam.WebApp` project for a complete working example that demonstrates:

- ✅ Full SCIMv2 implementation
- ✅ Repository pattern with stored procedures
- ✅ Filter processing
- ✅ CRUD operations
- ✅ Error handling
- ✅ Testing suite

### Key Files to Study

1. **`Program.cs`** - Application configuration
2. **`NoteRepositoryStoredProcedure.cs`** - Repository implementation
3. **`SCIMv2NoteService.cs`** - Resource service
4. **`comprehensive_scimv2_tests.ps1`** - Testing examples

### Next Steps

1. **Study the Notejam example** thoroughly
2. **Implement your own resource** following the pattern
3. **Create your stored procedures** based on the examples
4. **Test your implementation** using the provided test suite
5. **Deploy and monitor** your SCIMv2 application

---

## 📞 Support

For additional help:

- **Documentation:** Check Looplex.Foundation documentation
- **Examples:** Study the Notejam sample application
- **Testing:** Use the comprehensive test suite
- **Debugging:** Enable detailed logging and check error messages

---

**Happy SCIMv2 Development! 🚀**

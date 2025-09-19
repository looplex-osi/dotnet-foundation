# SCIMv2 Service Architecture

This directory contains the core SCIMv2 service implementation that provides centralized SCIMv2 protocol operations for resource management.

## Architecture Overview

The new architecture separates concerns between:
- **Looplex.Foundation.SCIMv2**: Core business logic (reusable)
- **Looplex.Foundation.Protocols.Http**: HTTP adapters and middleware

## Key Components

### Core Implementation

#### `SCIMv2`
Main consolidated service implementation
```csharp
public class SCIMv2 : ISCIMv2, IJsonSchemaProvider
{
    // Implements all SCIMv2 operations and schema management
    // Combines SCIMv2Service + SCIMv2SchemaProvider in a single file
}
```

### Core Interfaces

#### `IResource`
Base interface for all SCIMv2 resources
```csharp
public interface IResource
{
    string? Id { get; set; }
    string? ExternalId { get; set; }
    ResourceMeta Meta { get; set; }
    string[] Schemas { get; set; }
}
```

#### `IResourceService<T>`
Interface for resource-specific services
```csharp
public interface IResourceService<T> where T : IResource
{
    string CollectionName { get; }
    Task<(IList<T> Resources, int TotalCount)> QueryAsync(int startIndex, int count, 
        string? filter, string? sortBy, string? sortOrder, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(T resource, CancellationToken cancellationToken = default);
    Task<T?> RetrieveAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ReplaceAsync(Guid id, T resource, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Guid id, T resource, PatchOperation[] patches, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
```

#### `ISCIMv2`
Main service interface for SCIMv2 operations
```csharp
public interface ISCIMv2
{
    void Register<T>(IResourceService<T> service, string collectionName) where T : IResource;
    Task<SCIMv2Response> QueryAsync(string collection, int startIndex, int count, 
        string? filter, string? sortBy, string? sortOrder, CancellationToken cancellationToken = default);
    Task<SCIMv2Response> CreateAsync(string collection, IResource resource, CancellationToken cancellationToken = default);
    Task<SCIMv2Response> RetrieveAsync(string collection, string id, CancellationToken cancellationToken = default);
    Task<SCIMv2Response> ModifyAsync(string collection, string id, PatchOperation[] patches, CancellationToken cancellationToken = default);
    Task<SCIMv2Response> ReplaceAsync(string collection, string id, IResource resource, CancellationToken cancellationToken = default);
    Task<SCIMv2Response> DeleteAsync(string collection, string id, CancellationToken cancellationToken = default);
}
```

## Usage Examples

### 1. Register Services

```csharp
// In your application startup
services.AddSCIMv2Service();

// Register resource services
var scimService = serviceProvider.GetRequiredService<ISCIMv2>();
scimService.Register(new UserResourceService(), "Users");
scimService.Register(new GroupResourceService(), "Groups");
```

### 2. Implement Resource Service

```csharp
public class UserResourceService : BaseResourceService<User>
{
    public override string CollectionName => "Users";

    protected override Task<IList<User>> GetAllResourcesAsync(CancellationToken cancellationToken = default)
    {
        // Your implementation to get all users
        return Task.FromResult<IList<User>>(users);
    }

    protected override Task SaveResourceAsync(User resource, CancellationToken cancellationToken = default)
    {
        // Your implementation to save user
        return Task.CompletedTask;
    }

    protected override Task DeleteResourceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Your implementation to delete user
        return Task.CompletedTask;
    }
}
```

### 3. Use in Applications

```csharp
// In any application (Protocols.Http, Notejam, Case Management, etc.)
public class MyController
{
    private readonly ISCIMv2 _scimService;

    public MyController(ISCIMv2 scimService)
    {
        _scimService = scimService;
    }

    public async Task<IActionResult> GetUsers()
    {
        var response = await _scimService.QueryAsync("Users", 1, 10, null, null, null);
        return Ok(response);
    }
}
```

## Benefits

### Reusability
- Core logic can be used in any application
- No dependency on ASP.NET Core for business logic
- Easy to test and maintain

### Separation of Concerns
- Business logic in `Looplex.Foundation.SCIMv2`
- HTTP concerns in `Looplex.Foundation.Protocols.Http`
- Clear boundaries between layers

### Extensibility
- Easy to add new resource types
- Simple registration pattern
- Consistent API across applications

### Maintainability
- Centralized SCIMv2 logic
- Single source of truth for protocol implementation
- Easier to update and fix issues

### Validation and Error Handling
- Centralized validation logic
- Consistent error responses
- RFC-compliant error handling

### PATCH Operations Support
- Full RFC 6902 JSON Patch support
- Centralized patch operation parsing
- Validation and error handling for patch operations

## Migration Path

### Phase 1: New Applications
- Use the new `ISCIMv2` directly
- Implement `IResourceService<T>` for your resources
- Register services using the new pattern

### Phase 2: Existing Applications
- Gradually migrate existing middleware
- Keep backward compatibility during transition
- Update middleware to use new service

### Phase 3: Full Migration
- Remove old middleware logic
- All applications use the new service
- Clean up deprecated code

## Files Structure

```
Looplex.Foundation.SCIMv2/
├── Entities/
│   ├── IResource.cs              // Base resource interface
│   ├── Resource.cs               // Updated to implement IResource
│   ├── SCIMv2Response.cs         // Response wrapper
│   ├── PatchOperation.cs         // PATCH operations support
│   ├── ServiceNameProvider.cs    // Service name provider
│   └── ServiceProviderConfiguration.cs  // Service provider configuration
├── Modules/
│   ├── IResourceService.cs       // Resource service interface
│   ├── BaseResourceService.cs   // Base implementation
│   ├── UserService.cs            // User-specific service
│   └── GroupService.cs           // Group-specific service
├── ISCIMv2.cs                    // Main service interface
├── ISCIMv2Validation.cs          // Validation interface
├── SCIMv2.cs                     // Main consolidated service
└── Examples/
    └── SCIMv2ServiceExample.cs   // Usage examples
```

## Additional Interfaces

### `ISCIMv2Validation`
Interface for SCIMv2 validation operations
```csharp
public interface ISCIMv2Validation
{
    (bool IsValid, string? Error) ValidateJsonRequest(string json);
    (bool IsValid, string? Error) ValidateCollection(string collection);
    (bool IsValid, PatchOperation[] Operations, string? Error) ParsePatchOperations(string json);
    IResource CreateMockResource(string collectionName, string id);
}
```

### `PatchOperation`
Entity for PATCH operations support
```csharp
public class PatchOperation
{
    public string Op { get; set; }
    public string Path { get; set; }
    public object? Value { get; set; }
}
```

## Next Steps

1. Test the new service with existing applications
2. Update middleware to use the new service
3. Create migration guide for existing implementations
4. Add comprehensive tests for the new architecture
5. Update documentation with complete examples
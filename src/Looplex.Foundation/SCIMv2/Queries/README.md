# SCIM Filter Processing with Configurable SQL Dialect

This module provides flexible SCIM filter processing with support for multiple SQL dialects and configuration options.

## Features

- **Multiple SQL Dialects**: Support for SqlServer, PostgreSQL, MySQL, Oracle, SQLite, and Standard SQL
- **Configurable Defaults**: Set default dialect and field mappings via DI
- **Backward Compatibility**: Existing code continues to work without changes
- **Security**: SQL injection prevention and validation
- **Flexible Configuration**: Multiple ways to configure dialect preferences

## Usage Examples

### 1. Basic Usage (Default SqlServer)

```csharp
// Uses SqlServer dialect by default
var filter = "userName eq \"john\"";
var (sql, parameters) = filter.ToSqlPredicateWithParameters();
// Result: SqlServer compatible SQL
```

### 2. Explicit Dialect

```csharp
// Explicitly specify dialect
var filter = "userName eq \"john\"";
var (sql, parameters) = filter.ToSqlPredicateWithParameters(
    schemaMapping: null, 
    dialect: SqlDialect.PostgreSql
);
// Result: PostgreSQL compatible SQL
```

### 3. With Schema Mapping

```csharp
var schemaMapping = new Dictionary<string, string>
{
    { "userName", "dsNome" },
    { "email", "dsEmail" }
};

var filter = "userName eq \"john\" and email co \"example.com\"";
var (sql, parameters) = filter.ToSqlPredicateWithParameters(schemaMapping);
// Result: Maps userName -> dsNome, email -> dsEmail
```

### 4. Dependency Injection Configuration

```csharp
// In Program.cs or Startup.cs
services.Configure<ScimFilterConfiguration>(config =>
{
    config.DefaultDialect = SqlDialect.PostgreSql;
    config.DefaultFieldMapping = new Dictionary<string, string>
    {
        { "userName", "dsNome" },
        { "email", "dsEmail" }
    };
});

services.AddScoped<IScimFilterService, ScimFilterService>();

// In your service
public class MyService
{
    private readonly IScimFilterService _scimFilterService;

    public MyService(IScimFilterService scimFilterService)
    {
        _scimFilterService = scimFilterService;
    }

    public void ProcessFilter(string filter)
    {
        // Uses DI configuration (PostgreSql + field mapping)
        var (sql, parameters) = _scimFilterService.ToSqlPredicateWithParameters(filter);
        
        // Or override for specific case
        var (sql2, parameters2) = _scimFilterService.ToSqlPredicateWithParameters(
            filter, 
            dialect: SqlDialect.MySql
        );
    }
}
```

### 5. appsettings.json Configuration

```json
{
  "ScimFilter": {
    "DefaultDialect": "PostgreSql",
    "DefaultFieldMapping": {
      "userName": "dsNome",
      "email": "dsEmail",
      "active": "isAtivo"
    }
  }
}
```

```csharp
// In Program.cs
services.Configure<ScimFilterConfiguration>(
    configuration.GetSection("ScimFilter")
);
```

## Priority Order

The dialect selection follows this priority order:

1. **Explicit Parameter**: `dialect` parameter in method call
2. **DI Configuration**: `ScimFilterConfiguration.DefaultDialect`
3. **Default Fallback**: `SqlDialect.SqlServer`

## Supported Dialects

- `SqlDialect.SqlServer` - Microsoft SQL Server
- `SqlDialect.PostgreSql` - PostgreSQL
- `SqlDialect.MySql` - MySQL
- `SqlDialect.Oracle` - Oracle Database
- `SqlDialect.SQLite` - SQLite
- `SqlDialect.Standard` - ANSI SQL

## Testing Examples

### Unit Tests

```csharp
[TestMethod]
public void Test_DifferentDialects_ShouldGenerateCorrectSQL()
{
    // Arrange
    var filter = "userName eq \"john\" and email co \"example.com\"";
    var schemaMapping = new Dictionary<string, string>
    {
        { "userName", "dsNome" },
        { "email", "dsEmail" }
    };

    // Act - Test different dialects
    var sqlServerResult = filter.ToSqlPredicateWithParameters(schemaMapping, SqlDialect.SqlServer);
    var postgreResult = filter.ToSqlPredicateWithParameters(schemaMapping, SqlDialect.PostgreSql);
    var mySqlResult = filter.ToSqlPredicateWithParameters(schemaMapping, SqlDialect.MySql);

    // Assert
    Assert.IsTrue(sqlServerResult.Sql.Contains("dsNome"));
    Assert.IsTrue(postgreResult.Sql.Contains("dsNome"));
    Assert.IsTrue(mySqlResult.Sql.Contains("dsNome"));
}
```

### Integration Tests

```csharp
[TestMethod]
public void Test_DI_Configuration_ShouldWorkCorrectly()
{
    // Arrange
    var services = new ServiceCollection();
    services.Configure<ScimFilterConfiguration>(config =>
    {
        config.DefaultDialect = SqlDialect.PostgreSql;
        config.DefaultFieldMapping = new Dictionary<string, string>
        {
            { "userName", "dsNome" }
        };
    });
    services.AddScoped<IScimFilterService, ScimFilterService>();
    
    var provider = services.BuildServiceProvider();
    var service = provider.GetService<IScimFilterService>();

    // Act
    var result = service!.ToSqlPredicateWithParameters("userName eq \"john\"");

    // Assert
    Assert.IsNotNull(result);
    Assert.IsTrue(result.Sql.Contains("dsNome"));
}
```

## Performance Considerations

### Best Practices

1. **Reuse Service Instances**: The `IScimFilterService` is designed to be reused across requests
2. **Cache Field Mappings**: Store schema mappings in memory for frequently used filters
3. **Parameterized Queries**: Use `UseParameters = true` for better performance and security
4. **Avoid Complex Filters**: Very complex SCIM filters may impact parsing performance

### Performance Tips

```csharp
// Good: Reuse service instance
private readonly IScimFilterService _scimService;

public MyController(IScimFilterService scimService)
{
    _scimService = scimService; // Reuse across requests
}

// Good: Cache field mappings
private static readonly Dictionary<string, string> _fieldMapping = new()
{
    { "userName", "dsNome" },
    { "email", "dsEmail" }
};

// Avoid: Creating new service for each request
public void BadExample()
{
    var service = new SearchContentService(); // Don't do this
    var result = service.ConvertToSql(filter);
}
```

## Migration Guide

### From Old Implementation

**Before:**
```csharp
// Old hardcoded SqlServer
var (sql, parameters) = filter.ToSqlPredicateWithParameters(schemaMapping);
```

**After:**
```csharp
// New - same behavior (SqlServer default)
var (sql, parameters) = filter.ToSqlPredicateWithParameters(schemaMapping);

// Or explicit dialect
var (sql, parameters) = filter.ToSqlPredicateWithParameters(
    schemaMapping, 
    SqlDialect.PostgreSql
);
```

### Backward Compatibility

All existing code continues to work without changes:
- `ToSqlPredicate()` - No changes needed
- `ToSqlPredicate(schemaMapping)` - No changes needed
- `ToSqlPredicateWithParameters()` - No changes needed
- `ToSqlPredicateWithParameters(schemaMapping)` - No changes needed

## Security Features

- **SQL Injection Prevention**: Parameterized queries
- **Input Validation**: SCIM filter syntax validation
- **Dangerous Keywords**: Rejection of DROP, DELETE, etc.
- **Multiple Statements**: Prevention of batch execution
- **Sanitization**: SQL comment removal and normalization

## Error Handling

### Basic Error Handling

```csharp
try
{
    var (sql, parameters) = filter.ToSqlPredicateWithParameters(schemaMapping);
    // Use SQL and parameters
}
catch (InvalidOperationException ex)
{
    // Handle invalid SCIM filter
    Console.WriteLine($"Invalid filter: {ex.Message}");
}
catch (SecurityException ex)
{
    // Handle dangerous SQL detected
    Console.WriteLine($"Security violation: {ex.Message}");
}
```

### Advanced Error Handling

```csharp
public class ScimFilterProcessor
{
    private readonly IScimFilterService _scimService;
    private readonly ILogger<ScimFilterProcessor> _logger;

    public ScimFilterProcessor(IScimFilterService scimService, ILogger<ScimFilterProcessor> logger)
    {
        _scimService = scimService;
        _logger = logger;
    }

    public (string Sql, Dictionary<string, object> Parameters)? ProcessFilter(string filter)
    {
        try
        {
            return _scimService.ToSqlPredicateWithParameters(filter);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid SCIM filter: {Filter}", filter);
            return null;
        }
        catch (SecurityException ex)
        {
            _logger.LogError(ex, "Security violation in filter: {Filter}", filter);
            throw; // Re-throw security violations
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing filter: {Filter}", filter);
            throw;
        }
    }
}
```

## Troubleshooting

### Common Issues

#### 1. Dialect Not Applied

**Problem:** Dialect configuration not being used

**Solution:** Verify DI configuration
```csharp
// Check if configuration is registered
services.Configure<ScimFilterConfiguration>(config =>
{
    config.DefaultDialect = SqlDialect.PostgreSql;
});

// Ensure service is registered
services.AddScoped<IScimFilterService, ScimFilterService>();
```

#### 2. Field Mapping Not Working

**Problem:** Schema mapping not applied

**Solution:** Check mapping configuration
```csharp
// Verify mapping is correct
var mapping = new Dictionary<string, string>
{
    { "userName", "dsNome" } // SCIM attribute -> Database column
};

// Use explicit mapping
var result = filter.ToSqlPredicateWithParameters(mapping);
```

#### 3. SQL Generation Fails

**Problem:** Invalid SCIM filter syntax

**Solution:** Validate filter before processing
```csharp
if (_scimService.IsValidFilter(filter))
{
    var result = _scimService.ToSqlPredicateWithParameters(filter);
}
else
{
    // Handle invalid filter
    throw new ArgumentException("Invalid SCIM filter syntax");
}
```

#### 4. Performance Issues

**Problem:** Slow filter processing

**Solution:** Optimize usage patterns
```csharp
// Good: Reuse service and mappings
private static readonly Dictionary<string, string> _cachedMapping = new()
{
    { "userName", "dsNome" }
};

// Avoid: Creating new instances frequently
var result = filter.ToSqlPredicateWithParameters(_cachedMapping);
```

### Debug Information

Enable detailed logging to troubleshoot issues:

```csharp
// In appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Looplex.Foundation.SearchContent": "Debug"
    }
  }
}
```

## API Reference

### Extension Methods

- `ToSqlPredicate(string? filter)` - Basic SQL generation
- `ToSqlPredicate(string? filter, Dictionary<string, string> schemaMapping)` - With field mapping
- `ToSqlPredicateWithParameters(string? filter)` - With parameters
- `ToSqlPredicateWithParameters(string? filter, Dictionary<string, string>? schemaMapping, SqlDialect? dialect)` - Full control

### Service Interface

- `IScimFilterService.ToSqlPredicateWithParameters(string? filter, Dictionary<string, string>? schemaMapping, SqlDialect? dialect)` - DI-based service

### Configuration

- `ScimFilterConfiguration.DefaultDialect` - Default SQL dialect
- `ScimFilterConfiguration.DefaultFieldMapping` - Default field mappings

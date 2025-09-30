# 🚀 SCIMv2 Quick Start Guide

## ⚡ 5-Minute Setup

### Prerequisites
- .NET 8 SDK
- SQL Server (LocalDB or full instance)
- Visual Studio 2022 or VS Code

### Step 1: Clone and Setup
```bash
git clone <repository-url>
cd dotnet-foundation-checker/samples/Notejam.WebApp
dotnet restore
```

### Step 2: Configure Database
```sql
-- Run this in SQL Server Management Studio
CREATE DATABASE NotejamDB;
USE NotejamDB;

-- Create tables
CREATE TABLE notes (
    id INT IDENTITY(1,1) PRIMARY KEY,
    uuid UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    external_id INT NULL,
    markdown NVARCHAR(MAX) NOT NULL,
    active BIT NOT NULL DEFAULT 1,
    status TINYINT NOT NULL DEFAULT 1,
    created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    updated_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    created_by NVARCHAR(100) NOT NULL DEFAULT 'system',
    custom_fields NVARCHAR(MAX) NULL
);

CREATE TABLE pads (
    id INT IDENTITY(1,1) PRIMARY KEY,
    uuid UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    external_id INT NULL,
    name NVARCHAR(255) NOT NULL,
    active BIT NOT NULL DEFAULT 1,
    status TINYINT NOT NULL DEFAULT 1,
    created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    updated_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    created_by NVARCHAR(100) NOT NULL DEFAULT 'system',
    custom_fields NVARCHAR(MAX) NULL
);

-- Create indexes
CREATE UNIQUE INDEX IX_notes_uuid ON notes(uuid);
CREATE UNIQUE INDEX IX_pads_uuid ON pads(uuid);
```

### Step 3: Update Connection String
```json
// In appsettings.json or config.env
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=NotejamDB;Trusted_Connection=true;"
  }
}
```

### Step 4: Run Application
```bash
dotnet run --urls "http://localhost:7065"
```

### Step 5: Test SCIMv2 Endpoints
```bash
# Test basic endpoints
curl http://localhost:7065/notes
curl http://localhost:7065/ServiceProviderConfig
curl http://localhost:7065/ResourceTypes

# Create a note
curl -X POST http://localhost:7065/notes \
  -H "Content-Type: application/scim+json" \
  -d '{
    "schemas": ["urn:looplex:params:scim:schemas:notejam:2.0:Note"],
    "text": "Hello SCIMv2!",
    "active": true,
    "status": 1
  }'
```

## 🎯 What You Get

### SCIMv2 Endpoints
- ✅ `GET /notes` - List notes with filtering
- ✅ `GET /notes/{id}` - Get specific note
- ✅ `POST /notes` - Create new note
- ✅ `PUT /notes/{id}` - Update note
- ✅ `PATCH /notes/{id}` - Partial update
- ✅ `DELETE /notes/{id}` - Delete note
- ✅ `GET /ServiceProviderConfig` - Service configuration
- ✅ `GET /ResourceTypes` - Available resource types
- ✅ `GET /Schemas` - Schema definitions
- ✅ `POST /Bulk` - Bulk operations

### Filtering Examples
```bash
# Simple filters
curl "http://localhost:7065/notes?filter=active eq true"
curl "http://localhost:7065/notes?filter=status gt 0"
curl "http://localhost:7065/notes?filter=text co 'test'"

# Complex filters
curl "http://localhost:7065/notes?filter=(active eq true) and (status gt 0)"
curl "http://localhost:7065/notes?filter=(text co 'hello') or (status eq 1)"

# Pagination
curl "http://localhost:7065/notes?startIndex=1&count=5"

# Sorting
curl "http://localhost:7065/notes?sortBy=meta.lastModified&sortOrder=descending"
```

## 🔧 Troubleshooting

### Common Issues

**Application won't start:**
- Check if port 7065 is available
- Verify database connection string
- Ensure SQL Server is running

**Database errors:**
- Run the SQL setup scripts
- Check connection string
- Verify database permissions

**SCIMv2 errors:**
- Check if all stored procedures exist
- Verify SCIMv2 configuration
- Enable detailed logging

### Enable Debug Logging
```csharp
// In Program.cs
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);
```

## 📚 Next Steps

1. **Study the code** in `samples/Notejam.WebApp`
2. **Read the full guide** in `docs/SCIMv2_DEVELOPMENT_GUIDE.md`
3. **Run the test suite** in `reference/comprehensive_scimv2_tests.ps1`
4. **Create your own resource** following the pattern

## 🎉 Success!

You now have a fully functional SCIMv2 application! 

- **Base URL:** http://localhost:7065
- **Documentation:** See `docs/` folder
- **Examples:** See `reference/` folder
- **Testing:** Run `reference/comprehensive_scimv2_tests.ps1`

**Happy SCIMv2 Development! 🚀**


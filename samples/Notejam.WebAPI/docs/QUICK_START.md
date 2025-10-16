# 🚀 SCIMv2 Quick Start Guide

## ⚡ 5-Minute Setup

### Prerequisites
- .NET 8 SDK
- Visual Studio 2022 or VS Code
- Git (optional, for cloning)

### Step 1: Clone and Setup
```bash
git clone <repository-url>
cd dotnet-foundation-checker/samples/Notejam.WebAPI
dotnet restore
```

### Step 2: Database Configuration
**Notejam uses SQLite database by default** - no setup required!

The application automatically:
- Creates SQLite database file
- Sets up tables and indexes
- Seeds with sample data
- Handles all database operations

### Step 3: Run Application
```bash
dotnet run --project samples/Notejam.WebAPI/Notejam.WebAPI.csproj
```

The application will start on `http://localhost:7065` with:
- ✅ Auto-configured SCIMv2 services
- ✅ SQLite database
- ✅ Sample data loaded
- ✅ All endpoints ready

### Step 4: Test SCIMv2 Endpoints

#### Option A: Using HTTP Files (Recommended)
Open `WebApp.http` in VS Code and click "Send Request" on any endpoint.

#### Option B: Using Postman
Import `docs/Notejam.postman_collection` into Postman.

#### Option C: Using curl
```bash
# Test basic endpoints
curl http://localhost:7065/notes
curl http://localhost:7065/ServiceProviderConfig
curl http://localhost:7065/Schemas

# Create a note
curl -X POST http://localhost:7065/notes \
  -H "Content-Type: application/scim+json" \
  -d '{
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
- Ensure .NET 8 SDK is installed
- Run `dotnet restore` if needed

**Database errors:**
- SQLite database, no setup required
- Data resets on each restart
- Check application logs for details

**SCIMv2 errors:**
- Auto-configuration handles most setup
- Check application logs for details
- Verify all services are registered

### Enable Debug Logging
```csharp
// In Program.cs
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);
```

## 📚 Next Steps

1. **Study the code** in `samples/Notejam.WebAPI`
2. **Read the full guide** in `docs/SCIMv2_DEVELOPMENT_GUIDE.md`
3. **Test with Postman** using `docs/Notejam.postman_collection`
4. **Use HTTP files** with `WebApp.http` for quick testing
5. **Create your own resource** following the pattern

## 🎉 Success!

You now have a fully functional SCIMv2 application! 

- **Base URL:** http://localhost:7065
- **Documentation:** See `docs/` folder
- **Postman Collection:** `docs/Notejam.postman_collection`
- **HTTP Test Files:** `WebApp.http`
- **Testing:** Use Postman or HTTP files for testing

**Happy SCIMv2 Development! 🚀**


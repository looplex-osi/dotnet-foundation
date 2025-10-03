# 📚 SCIMv2 API Reference

## 🌐 Base URL
```
http://localhost:7065
```

## 📋 Endpoints Overview

| Method | Endpoint | Description | Status |
|--------|----------|-------------|--------|
| GET | `/notes` | List notes with filtering | ✅ |
| GET | `/notes/{id}` | Get specific note | ✅ |
| POST | `/notes` | Create new note | ✅ |
| PUT | `/notes/{id}` | Replace note | ✅ |
| PATCH | `/notes/{id}` | Modify note | ✅ |
| DELETE | `/notes/{id}` | Delete note | ✅ |
| GET | `/pads` | List pads with filtering | ✅ |
| GET | `/pads/{id}` | Get specific pad | ✅ |
| POST | `/pads` | Create new pad | ✅ |
| PUT | `/pads/{id}` | Replace pad | ✅ |
| PATCH | `/pads/{id}` | Modify pad | ✅ |
| DELETE | `/pads/{id}` | Delete pad | ✅ |
| GET | `/ServiceProviderConfig` | Service configuration | ✅ |
| GET | `/ResourceTypes` | Available resource types | ✅ |
| GET | `/Schemas` | Schema definitions | ✅ |
| POST | `/Bulk` | Bulk operations | ✅ |

---

## 📝 Notes Resource

### GET /notes
List notes with optional filtering and pagination.

**Query Parameters:**
- `filter` (string, optional) - SCIMv2 filter expression
- `startIndex` (int, optional) - Starting index (1-based, default: 1)
- `count` (int, optional) - Number of results per page (default: 100)

**Supported Filter Operators:**
- `eq` - Equals: `active eq "true"`
- `ne` - Not equals: `status ne 0`
- `co` - Contains: `text co "test"`
- `sw` - Starts with: `text sw "Hello"`
- `ew` - Ends with: `text ew "world"`
- `pr` - Present (not null): `externalId pr`
- `gt` - Greater than: `status gt 0`
- `ge` - Greater than or equal: `status ge 1`
- `lt` - Less than: `status lt 10`
- `le` - Less than or equal: `status le 5`

**Example:**
```bash
curl "http://localhost:7065/notes?filter=active eq \"true\"&startIndex=1&count=10"
```

**Response:**
```json
{
  "totalResults": 258,
  "itemsPerPage": 10,
  "startIndex": 1,
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:ListResponse"],
  "Resources": [
    {
      "id": "58b6463e-f36b-1410-85cc-0046c3744233",
      "text": "# Título da Nota\n\nConteúdo em **markdownv2 30092500**",
      "active": true,
      "status": 1,
      "customFields": "{\"priority\": \"high\"}",
      "schemas": ["urn:looplex:params:scim:schemas:notejam:2.0:Note"],
      "meta": {
        "resourceType": "Note",
        "created": "2025-01-30T18:58:17.792Z",
        "lastModified": "2025-01-30T18:58:17.792Z",
        "location": "/notes/58b6463e-f36b-1410-85cc-0046c3744233",
        "version": "87ef17e90a31355bd54bc8ee28b69cccbf769496b7ab6852599e4fd74d2c1e0b"
      }
    }
  ]
}
```

### GET /notes/{id}
Retrieve a specific note by ID.

**Example:**
```bash
curl "http://localhost:7065/notes/123e4567-e89b-12d3-a456-426614174000"
```

**Response:**
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "externalId": "12345",
  "text": "Sample note content",
  "active": true,
  "status": 1,
  "schemas": ["urn:looplex:params:scim:schemas:notejam:2.0:Note"],
  "meta": {
    "resourceType": "Note",
    "created": "2023-01-01T00:00:00Z",
    "lastModified": "2023-01-01T00:00:00Z",
    "location": "/notes/123e4567-e89b-12d3-a456-426614174000"
  }
}
```

### POST /notes
Create a new note.

**Request Body:**
```json
{
  "text": "New note content",
  "active": true,
  "status": 1,
  "customFields": "{\"priority\": \"high\"}"
}
```

**Example:**
```bash
curl -X POST "http://localhost:7065/notes" \
  -H "Content-Type: application/scim+json" \
  -d '{
    "text": "New note content",
    "active": true,
    "status": 1,
    "customFields": "{\"priority\": \"high\"}"
  }'
```

**Response:**
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "externalId": null,
  "text": "New note content",
  "active": true,
  "status": 1,
  "schemas": ["urn:looplex:params:scim:schemas:notejam:2.0:Note"],
  "meta": {
    "resourceType": "Note",
    "created": "2023-01-01T00:00:00Z",
    "lastModified": "2023-01-01T00:00:00Z",
    "location": "/notes/123e4567-e89b-12d3-a456-426614174000"
  }
}
```

### PUT /notes/{id}
Replace an existing note.

**Request Body:**
```json
{
  "text": "Updated note content",
  "active": true,
  "status": 2,
  "customFields": "{\"priority\": \"low\", \"updated\": true}"
}
```

**Example:**
```bash
curl -X PUT "http://localhost:7065/notes/58b6463e-f36b-1410-85cc-0046c3744233" \
  -H "Content-Type: application/scim+json" \
  -d '{
    "text": "Updated note content",
    "active": true,
    "status": 2,
    "customFields": "{\"priority\": \"low\", \"updated\": true}"
  }'
```

### PATCH /notes/{id}
Partially update a note.

**Request Body:**
```json
{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"],
  "Operations": [
    {
      "op": "replace",
      "path": "text",
      "value": "PATCH Updated Note Content - Working!"
    },
    {
      "op": "replace",
      "path": "active",
      "value": true
    }
  ]
}
```

**Example:**
```bash
curl -X PATCH "http://localhost:7065/notes/58b6463e-f36b-1410-85cc-0046c3744233" \
  -H "Content-Type: application/scim+json" \
  -d '{
    "schemas": ["urn:ietf:params:scim:api:messages:2.0:PatchOp"],
    "Operations": [
      {
        "op": "replace",
        "path": "text",
        "value": "PATCH Updated Note Content - Working!"
      },
      {
        "op": "replace",
        "path": "active",
        "value": true
      }
    ]
  }'
```

### DELETE /notes/{id}
Delete a note (soft delete - resource remains accessible but marked as inactive).

**Example:**
```bash
curl -X DELETE "http://localhost:7065/notes/58b6463e-f36b-1410-85cc-0046c3744233"
```

**Response:** `204 No Content`

**Note:** This is a soft delete operation. The note is marked as inactive (`active: false`) but remains accessible via GET requests.

---

## 📁 Pads Resource

### GET /pads
List pads with optional filtering and pagination.

**Query Parameters:** Same as `/notes`

**Example:**
```bash
curl "http://localhost:7065/pads?filter=active eq true"
```

### GET /pads/{id}
Retrieve a specific pad by ID.

**Example:**
```bash
curl "http://localhost:7065/pads/123e4567-e89b-12d3-a456-426614174000"
```

### POST /pads
Create a new pad.

**Request Body:**
```json
{
  "name": "New pad name",
  "active": true,
  "customFields": "{\"priority\": \"high\"}"
}
```

### PUT /pads/{id}
Replace an existing pad.

### PATCH /pads/{id}
Partially update a pad.

### DELETE /pads/{id}
Delete a pad (hard delete - resource is permanently removed).

**Example:**
```bash
curl -X DELETE "http://localhost:7065/pads/eeb2463e-f36b-1410-85cc-0046c3744233"
```

**Response:** `204 No Content`

**Note:** This is a hard delete operation. The pad is permanently removed from the database and will return 404 on subsequent GET requests.

---

## 🗑️ Delete Operations

### Soft Delete vs Hard Delete

The application implements two different delete strategies:

#### Notes - Soft Delete
- **Behavior**: Resource is marked as inactive but remains accessible
- **Status**: `active: false` after deletion
- **Accessibility**: Resource can still be retrieved via GET requests
- **Use Case**: Audit trails, data recovery, compliance requirements

#### Pads - Hard Delete  
- **Behavior**: Resource is permanently removed from database
- **Status**: Resource no longer exists
- **Accessibility**: GET requests return 404 Not Found
- **Use Case**: Complete data removal, privacy requirements

### Delete Examples

**Soft Delete (Notes):**
```bash
# Delete a note (soft delete)
curl -X DELETE "http://localhost:7065/notes/58b6463e-f36b-1410-85cc-0046c3744233"

# Note is still accessible but marked inactive
curl "http://localhost:7065/notes/58b6463e-f36b-1410-85cc-0046c3744233"
# Returns: {"active": false, ...}
```

**Hard Delete (Pads):**
```bash
# Delete a pad (hard delete)
curl -X DELETE "http://localhost:7065/pads/eeb2463e-f36b-1410-85cc-0046c3744233"

# Pad is no longer accessible
curl "http://localhost:7065/pads/eeb2463e-f36b-1410-85cc-0046c3744233"
# Returns: 404 Not Found
```

---

## 🔍 Filtering

### Supported Operators

| Operator | Description | Example |
|----------|-------------|---------|
| `eq` | Equals | `active eq true` |
| `ne` | Not equals | `status ne 0` |
| `co` | Contains | `text co 'test'` |
| `sw` | Starts with | `text sw 'Hello'` |
| `ew` | Ends with | `text ew 'world'` |
| `pr` | Present (not null) | `externalId pr` |
| `gt` | Greater than | `status gt 0` |
| `ge` | Greater than or equal | `status ge 1` |
| `lt` | Less than | `status lt 10` |
| `le` | Less than or equal | `status le 5` |

### Complex Filters

**AND Operations:**
```bash
curl "http://localhost:7065/notes?filter=(active eq \"true\") and (status gt 0)"
```

**OR Operations:**
```bash
curl "http://localhost:7065/notes?filter=(text co \"hello\") or (status eq 1)"
```

**Nested Operations:**
```bash
curl "http://localhost:7065/notes?filter=((active eq \"true\") and (status gt 0)) or (text co \"urgent\")"
```

### Date Filtering

**Date Comparisons:**
```bash
curl "http://localhost:7065/notes?filter=meta.lastModified ge \"2025-01-01T00:00:00Z\""
curl "http://localhost:7065/notes?filter=meta.created lt \"2025-12-31T23:59:59Z\""
```

---

## 🔍 Discovery Endpoints

### GET /ServiceProviderConfig
Get service provider configuration.

**Example:**
```bash
curl "http://localhost:7065/ServiceProviderConfig"
```

**Response:**
```json
{
  "schemas": ["urn:ietf:params:scim:schemas:core:2.0:ServiceProviderConfig"],
  "patch": {
    "supported": true
  },
  "bulk": {
    "supported": true,
    "maxOperations": 100,
    "maxPayloadSize": 1048576
  },
  "filter": {
    "supported": true,
    "maxResults": 200
  },
  "changePassword": {
    "supported": false
  },
  "sort": {
    "supported": true
  },
  "etag": {
    "supported": false
  },
  "authenticationSchemes": [
    {
      "name": "OAuth Bearer Token",
      "description": "Authentication scheme using the OAuth Bearer Token Standard",
      "specUri": "http://www.rfc-editor.org/info/rfc6750",
      "type": "oauthbearertoken",
      "primary": true
    }
  ],
  "meta": {
    "location": "/ServiceProviderConfig",
    "resourceType": "ServiceProviderConfig"
  }
}
```

### GET /ResourceTypes
Get available resource types.

**Example:**
```bash
curl "http://localhost:7065/ResourceTypes"
```

**Response:**
```json
{
  "totalResults": 2,
  "itemsPerPage": 2,
  "startIndex": 1,
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:ListResponse"],
  "Resources": [
    {
      "id": "notes",
      "name": "notes",
      "endpoint": "/notes",
      "description": "SCIMv2 resource type for notes",
      "schema": "urn:looplex:params:scim:schemas:notejam:2.0:Note",
      "schemaExtensions": [],
      "meta": {
        "resourceType": "ResourceType",
        "location": "/ResourceTypes/notes"
      }
    },
    {
      "id": "pads",
      "name": "pads",
      "endpoint": "/pads",
      "description": "SCIMv2 resource type for pads",
      "schema": "urn:looplex:params:scim:schemas:notejam:2.0:Pad",
      "schemaExtensions": [],
      "meta": {
        "resourceType": "ResourceType",
        "location": "/ResourceTypes/pads"
      }
    }
  ]
}
```

### GET /Schemas
Get available schemas.

**Example:**
```bash
curl "http://localhost:7065/Schemas"
```

**Response:**
```json
{
  "totalResults": 4,
  "itemsPerPage": 4,
  "startIndex": 1,
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:ListResponse"],
  "Resources": [
    {
      "id": "urn:ietf:params:scim:schemas:core:2.0:User",
      "schemas": ["urn:ietf:params:scim:schemas:core:2.0:User"],
      "name": "User",
      "description": "User Account"
    },
    {
      "id": "urn:ietf:params:scim:schemas:core:2.0:Group",
      "schemas": ["urn:ietf:params:scim:schemas:core:2.0:Group"],
      "name": "Group",
      "description": "Group"
    },
    {
      "id": "urn:looplex:params:scim:schemas:notejam:2.0:Note",
      "schemas": ["urn:looplex:params:scim:schemas:notejam:2.0:Note"],
      "name": "Note",
      "description": "Note resource"
    },
    {
      "id": "urn:looplex:params:scim:schemas:notejam:2.0:Pad",
      "schemas": ["urn:looplex:params:scim:schemas:notejam:2.0:Pad"],
      "name": "Pad",
      "description": "Pad resource"
    }
  ]
}
```

---

## 📦 Bulk Operations

### POST /Bulk
Perform bulk operations.

**Request Body:**
```json
{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:BulkRequest"],
  "Operations": [
    {
      "method": "POST",
      "path": "/notes",
      "bulkId": "bulk-1",
      "data": {
        "text": "Bulk Note 1",
        "active": true,
        "status": 1
      }
    },
    {
      "method": "POST",
      "path": "/pads",
      "bulkId": "bulk-2",
      "data": {
        "name": "Bulk Pad 1",
        "active": true
      }
    }
  ]
}
```

**Example:**
```bash
curl -X POST "http://localhost:7065/Bulk" \
  -H "Content-Type: application/scim+json" \
  -d '{
    "schemas": ["urn:ietf:params:scim:api:messages:2.0:BulkRequest"],
    "Operations": [
      {
        "method": "POST",
        "path": "/notes",
        "bulkId": "bulk-1",
        "data": {
          "text": "Bulk Note 1",
          "active": true,
          "status": 1
        }
      }
    ]
  }'
```

---

## 📊 HTTP Status Codes

| Code | Description | Usage |
|------|-------------|-------|
| 200 | OK | Successful GET, PUT, PATCH |
| 201 | Created | Successful POST |
| 204 | No Content | Successful DELETE |
| 400 | Bad Request | Invalid request body or parameters |
| 404 | Not Found | Resource not found |
| 409 | Conflict | Resource already exists |
| 500 | Internal Server Error | Server error |

---

## 🔧 Error Responses

### Standard Error Format
```json
{
  "schemas": ["urn:ietf:params:scim:api:messages:2.0:Error"],
  "status": "400",
  "scimType": "invalidSyntax",
  "detail": "Invalid filter syntax"
}
```

### Common Error Types
- `invalidSyntax` - Invalid request syntax
- `invalidFilter` - Invalid filter expression
- `invalidPath` - Invalid resource path
- `invalidValue` - Invalid attribute value
- `invalidVers` - Invalid schema version
- `tooMany` - Too many results
- `uniqueness` - Attribute value must be unique
- `mutability` - Attribute is read-only
- `sensitive` - Attribute value is sensitive

---

## 📚 Examples

### Complete CRUD Example
```bash
# 1. Create a note
curl -X POST "http://localhost:7065/notes" \
  -H "Content-Type: application/scim+json" \
  -d '{
    "text": "Hello SCIMv2!",
    "active": true,
    "status": 1
  }'

# 2. Get all notes
curl "http://localhost:7065/notes"

# 3. Get specific note
curl "http://localhost:7065/notes/58b6463e-f36b-1410-85cc-0046c3744233"

# 4. Update note
curl -X PUT "http://localhost:7065/notes/58b6463e-f36b-1410-85cc-0046c3744233" \
  -H "Content-Type: application/scim+json" \
  -d '{
    "text": "Updated content",
    "active": true,
    "status": 2
  }'

# 5. Delete note
curl -X DELETE "http://localhost:7065/notes/58b6463e-f36b-1410-85cc-0046c3744233"
```

### Filtering Examples
```bash
# Simple filters
curl "http://localhost:7065/notes?filter=active eq \"true\""
curl "http://localhost:7065/notes?filter=status gt 0"
curl "http://localhost:7065/notes?filter=text co \"test\""

# Complex filters
curl "http://localhost:7065/notes?filter=(active eq \"true\") and (status gt 0)"
curl "http://localhost:7065/notes?filter=(text co \"hello\") or (status eq 1)"

# Pagination
curl "http://localhost:7065/notes?startIndex=1&count=5"

# Sorting
curl "http://localhost:7065/notes?sortBy=meta.lastModified&sortOrder=descending"
```

---

## 🎯 Testing

### Manual Testing

#### Option 1: HTTP Files (Recommended)
Open `WebApp.http` in VS Code and click "Send Request" on any endpoint.

#### Option 2: Postman Collection
Import `docs/Notejam.postman_collection` into Postman.

#### Option 3: PowerShell Testing
```powershell
# Test specific endpoints
$baseUrl = "http://localhost:7065"
$headers = @{"Accept"="application/scim+json"}

# Test basic operations
Invoke-WebRequest -Uri "$baseUrl/notes" -Headers $headers
Invoke-WebRequest -Uri "$baseUrl/ServiceProviderConfig" -Headers $headers
Invoke-WebRequest -Uri "$baseUrl/Schemas" -Headers $headers
```

---

**📚 For more detailed information, see the [SCIMv2 Development Guide](SCIMv2_DEVELOPMENT_GUIDE.md)**

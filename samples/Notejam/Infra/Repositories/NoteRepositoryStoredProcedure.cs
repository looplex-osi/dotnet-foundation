using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Looplex.Foundation.Ports;
using Looplex.Foundation.Helpers;
using Looplex.Foundation.SCIMv2.Queries;
using Looplex.Foundation.SCIMv2.Entities;
using Looplex.Foundation.SCIMv2.Modules;
using Looplex.Samples.Domain.Entities;
using Looplex.Samples.Application;
using Microsoft.Extensions.Logging;

namespace Looplex.Samples.Infra.Repositories;

/// <summary>
/// Repository implementation for Note entities using stored procedures.
/// 
/// This repository implements the Case-Management pattern with stored procedures:
/// - USP_notes_cquery - Collection query (paginação)
/// - USP_notes_pquery - Paginated query (com contagem)
/// - USP_notes_retrieve - Retrieve single note
/// - USP_notes_create - Create new note
/// - USP_notes_update - Update existing note
/// 
/// Maintains full compatibility with IResourceRepository<Note> interface from Looplex.Foundation.
/// Supports hierarchical filtering by pad (padId filter).
/// </summary>
public class NoteRepositoryStoredProcedure : INoteRepository, IResourceRepository<Note>
{
    private readonly IDbConnections _connections;
    private readonly ILogger<NoteRepositoryStoredProcedure> _logger;

    public NoteRepositoryStoredProcedure(IDbConnections connections, ILogger<NoteRepositoryStoredProcedure> logger)
    {
        _connections = connections;
        _logger = logger;
    }

    /// <summary>
    /// Query notes with SCIM v2.0 filtering and pagination using stored procedures.
    /// Converts SCIM parameters to Case-Management stored procedure parameters.
    /// Supports hierarchical filtering by pad (padId filter).
    /// </summary>
    public async Task<(IList<Note> Resources, int TotalCount)> QueryAsync(
        int startIndex, int count, string? filter, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting notes with SCIM parameters: startIndex={StartIndex}, count={Count}, filter={Filter}", 
                startIndex, count, filter);

            // Convert SCIM to Case-Management parameters
            var (page, pageSize) = ConvertScimToPageParameters(startIndex, count);
            var filterParams = ParseFilterToStoredProcedureParams(filter);

            await using var dbCommand = await _connections.CommandConnection();
            await using var command = dbCommand.CreateCommand();

            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "USP_notes_pquery";

            // Add stored procedure parameters
            command.Parameters.Add(Dbs.CreateParameter(command, "@page", page, DbType.Int32));
            command.Parameters.Add(Dbs.CreateParameter(command, "@page_size", pageSize, DbType.Int32));
            command.Parameters.Add(Dbs.CreateParameter(command, "@do_count", true, DbType.Boolean));
            command.Parameters.Add(Dbs.CreateParameter(command, "@order_by", "updated_at DESC", DbType.String));
            
            // Add filter parameters
            AddFilterParameters(command, filterParams);

            var (notes, totalCount) = await ExecuteStoredProcedureWithCount((SqlCommand)command, cancellationToken);

            _logger.LogInformation("Retrieved {Count} notes, total: {TotalCount}", notes.Count, totalCount);
            return (notes, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notes with stored procedure");
            throw new InvalidOperationException($"Failed to get notes: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Legacy method for backward compatibility
    /// </summary>
    public async Task<(IList<Note> Notes, int TotalCount)> GetNotesAsync(
        string? filter = null, 
        int page = 1, 
        int pageSize = 10, 
        CancellationToken cancellationToken = default)
    {
        var result = await QueryAsync((page - 1) * pageSize + 1, pageSize, filter, cancellationToken);
        return (result.Resources, result.TotalCount);
    }

    public async Task<Note?> GetNoteByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting note by ID: {NoteId}", id);

            await using var dbCommand = await _connections.CommandConnection();
            await using var command = dbCommand.CreateCommand();

            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "USP_notes_retrieve";
            command.Parameters.Add(Dbs.CreateParameter(command, "@filter_uuid", id, DbType.Guid));

            Note? note = null;
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                note = MapReaderToNote(reader);
                _logger.LogInformation("Note retrieved successfully: {NoteId}", id);
            }
            else
            {
                _logger.LogWarning("Note not found with ID: {NoteId}", id);
            }

            return note;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting note by ID: {NoteId}", id);
            throw new InvalidOperationException($"Failed to get note: {ex.Message}", ex);
        }
    }

    public async Task<Guid> CreateNoteAsync(Note note, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("🚀 CreateNoteAsync started - Note: Name='{Name}', Text='{Text}', Active={Active}, Status={Status}", 
                note.Name, note.Text, note.Active, note.Status);

            await using var dbCommand = await _connections.CommandConnection();
            await using var command = dbCommand.CreateCommand();

            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "USP_notes_create";
            _logger.LogInformation("📋 Using stored procedure: {StoredProcedure}", command.CommandText);

            // Note: pad_guid and created_by will use default values from stored procedure
            _logger.LogInformation("🔍 Using default values for pad_guid and created_by from stored procedure");

            _logger.LogInformation("📝 Adding parameters to command...");
            command.Parameters.Add(Dbs.CreateParameter(command, "@text", note.Text, DbType.String));
            // Note: @pad_guid and @created_by are now optional with default values in stored procedure
            // These parameters are NOT passed - stored procedure will use default values
            command.Parameters.Add(Dbs.CreateParameter(command, "@active", note.Active, DbType.Boolean));
            command.Parameters.Add(Dbs.CreateParameter(command, "@status", note.Status, DbType.Byte));
            command.Parameters.Add(Dbs.CreateParameter(command, "@custom_fields", note.CustomFields, DbType.String));
            
            _logger.LogInformation("🎯 Executing stored procedure with parameters: text='{Text}', active={Active}, status={Status}, custom_fields='{CustomFields}' (pad_guid and created_by will use default values)", 
                note.Text, note.Active, note.Status, note.CustomFields);

            _logger.LogInformation("⚡ Executing stored procedure...");
            var result = await command.ExecuteScalarAsync(cancellationToken);
            var uuid = result != null ? (Guid)result : Guid.Empty;

            _logger.LogInformation("🎉 Note created successfully with UUID: {NoteUuid}", uuid);
            return uuid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error creating note with Text: {NoteText} - Exception: {ExceptionType}: {ExceptionMessage}", 
                note.Text, ex.GetType().Name, ex.Message);
            throw new InvalidOperationException($"Failed to create note: {ex.Message}", ex);
        }
    }

    public async Task<int> UpdateNoteAsync(Guid id, Note note, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"🔍 UpdateNoteAsync called with ID: {id}");
        Console.WriteLine($"🔍 Note: {note?.Name}, Text: {note?.Text}, Active: {note?.Active}, Status: {note?.Status}");
        
        try
        {
            _logger.LogInformation("🚀 UpdateNoteAsync started - ID: {NoteId}, Text: '{Text}', Active: {Active}, Status: {Status}", 
                id, note.Text, note.Active, note.Status);

            Console.WriteLine($"🔍 Getting database connection...");
            var dbCommand = await _connections.CommandConnection();
            Console.WriteLine($"🔍 Database connection obtained: {dbCommand != null}");
            
            await using var command = dbCommand.CreateCommand();
            Console.WriteLine($"🔍 Command created: {command != null}");

            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "USP_notes_update";
            _logger.LogInformation("📋 Using stored procedure: {StoredProcedure}", command.CommandText);
            Console.WriteLine($"🔍 Stored procedure: {command.CommandText}");

            Console.WriteLine($"🔍 Adding parameters...");
            command.Parameters.Add(Dbs.CreateParameter(command, "@note_guid", id, DbType.Guid));
            command.Parameters.Add(Dbs.CreateParameter(command, "@name", note.Name, DbType.String));
            command.Parameters.Add(Dbs.CreateParameter(command, "@text", note.Text, DbType.String));
            command.Parameters.Add(Dbs.CreateParameter(command, "@active", note.Active, DbType.Boolean));
            command.Parameters.Add(Dbs.CreateParameter(command, "@status", (int)note.Status, DbType.Int32));
            Console.WriteLine($"🔍 Parameters added: {command.Parameters.Count}");

            _logger.LogInformation("🎯 Executing stored procedure with parameters: note_guid={NoteGuid}, name='{Name}', text='{Text}', active={Active}, status={Status}", 
                id, note.Name, note.Text, note.Active, note.Status);

            Console.WriteLine($"🔍 Executing stored procedure...");
            int rows = await command.ExecuteNonQueryAsync(cancellationToken);
            Console.WriteLine($"🔍 Stored procedure executed, rows affected: {rows}");

            if (rows == 0)
            {
                _logger.LogWarning("No note found to update with ID: {NoteId}", id);
                Console.WriteLine($"⚠️ No note found to update with ID: {id}");
            }
            else
            {
                _logger.LogInformation("Note updated successfully. Rows affected: {RowsAffected}", rows);
                Console.WriteLine($"✅ Note updated successfully. Rows affected: {rows}");
            }

            return rows;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in UpdateNoteAsync: {ex.Message}");
            Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            _logger.LogError(ex, "Error updating note with ID: {NoteId}", id);
            throw new InvalidOperationException($"Failed to update note: {ex.Message}", ex);
        }
    }

    public async Task<int> DeleteNoteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting note with ID: {NoteId}", id);

            await using var dbCommand = await _connections.CommandConnection();
            await using var command = dbCommand.CreateCommand();

            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "USP_notes_update";

            command.Parameters.Add(Dbs.CreateParameter(command, "@note_guid", id, DbType.Guid));
            command.Parameters.Add(Dbs.CreateParameter(command, "@active", false, DbType.Boolean));

            int rows = await command.ExecuteNonQueryAsync(cancellationToken);

            if (rows == 0)
            {
                _logger.LogWarning("No active note found to delete with ID: {NoteId}", id);
            }
            else
            {
                _logger.LogInformation("Note deleted successfully. Rows affected: {RowsAffected}", rows);
            }

            return rows;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting note with ID: {NoteId}", id);
            throw new InvalidOperationException($"Failed to delete note: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Converts SCIM startIndex/count to page/pageSize parameters
    /// </summary>
    private (int page, int pageSize) ConvertScimToPageParameters(int startIndex, int count)
    {
        // SCIM startIndex is 1-based, convert to page-based
        var page = (startIndex - 1) / count + 1;
        return (page, count);
    }

    // IResourceRepository<Note> implementation
    public async Task<Note?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await GetNoteByIdAsync(Guid.Parse(id), cancellationToken);
    }

    public async Task<Note> CreateAsync(Note resource, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 CreateAsync called with Note: Name='{Name}', Text='{Text}', Active={Active}, Status={Status}", 
            resource.Name, resource.Text, resource.Active, resource.Status);
        
        try
        {
            var id = await CreateNoteAsync(resource, cancellationToken);
            _logger.LogInformation("✅ CreateAsync completed successfully with ID: {Id}", id);
            
            // Set the ID on the resource before returning
            resource.Id = id.ToString();
            _logger.LogInformation("🔧 Set resource.Id to: {ResourceId}", resource.Id);
            
            return resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ CreateAsync failed for Note: Name='{Name}', Text='{Text}'", resource.Name, resource.Text);
            throw;
        }
    }

    public async Task<Note> UpdateAsync(string id, Note resource, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"🔍 NoteRepositoryStoredProcedure.UpdateAsync called with ID: {id}");
        Console.WriteLine($"🔍 Resource: {resource?.Name}, Active: {resource?.Active}, Status: {resource?.Status}");
        
        _logger.LogInformation("🔍 UpdateAsync called with ID: {Id}, Resource: {ResourceName}", id, resource?.Name);
        
        await UpdateNoteAsync(Guid.Parse(id), resource, cancellationToken);
        
        // Set the ID on the resource before returning
        resource.Id = id;
        _logger.LogInformation("🔧 Set resource.Id to: {ResourceId}", resource.Id);
        
        Console.WriteLine($"🔍 UpdateAsync completed successfully for ID: {id}");
        return resource;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var result = await DeleteNoteAsync(Guid.Parse(id), cancellationToken);
        return result > 0;
    }


    /// <summary>
    /// Gets or creates a system user for note creation
    /// </summary>
    private async Task<string> GetOrCreateSystemUserAsync(DbConnection connection, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("🔍 GetOrCreateSystemUserAsync started");
            
            await using var command = connection.CreateCommand();

            // First, try to find an existing user
            command.CommandType = CommandType.Text;
            command.CommandText = "SELECT TOP 1 name FROM users ORDER BY id";
            _logger.LogInformation("🔍 Looking for existing users...");
            
            var existingUser = await command.ExecuteScalarAsync(cancellationToken);
            if (existingUser != null)
            {
                _logger.LogInformation("✅ Found existing user: {ExistingUser}", existingUser.ToString());
                return existingUser.ToString()!;
            }

            _logger.LogInformation("⚠️ No existing user found, creating system user...");
            // If no user exists, create a system user
            command.CommandText = @"
                IF NOT EXISTS (SELECT 1 FROM users WHERE name = 'system')
                BEGIN
                    INSERT INTO users (name, email, active, created_at, updated_at)
                    VALUES ('system', 'system@localhost', 1, GETDATE(), GETDATE())
                END";
            
            await command.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("✅ System user created successfully");
            return "system";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error getting or creating system user: {ExceptionType}: {ExceptionMessage}", 
                ex.GetType().Name, ex.Message);
            // Fallback to a default user name
            return "system";
        }
    }

    /// <summary>
    /// Gets the first available pad ID for creating notes
    /// </summary>
    private async Task<Guid> GetFirstAvailablePadIdAsync(DbConnection connection, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("🔍 GetFirstAvailablePadIdAsync started");
            
            await using var command = connection.CreateCommand();

            command.CommandType = CommandType.Text;
            command.CommandText = "SELECT TOP 1 uuid FROM pads WHERE active = 1 ORDER BY created_at DESC";
            _logger.LogInformation("🔍 Looking for active pads...");

            var result = await command.ExecuteScalarAsync(cancellationToken);
            if (result != null)
            {
                _logger.LogInformation("✅ Found active pad: {PadId}", result);
                return (Guid)result;
            }

            _logger.LogInformation("⚠️ No active pad found, looking for any pad...");
            // If no active pad found, get any pad
            command.CommandText = "SELECT TOP 1 uuid FROM pads ORDER BY created_at DESC";
            result = await command.ExecuteScalarAsync(cancellationToken);
            if (result != null)
            {
                _logger.LogInformation("✅ Found any pad: {PadId}", result);
                return (Guid)result;
            }

            _logger.LogError("💥 No pads found in the database");
            throw new InvalidOperationException("No pads found in the database");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error getting first available pad ID: {ExceptionType}: {ExceptionMessage}", 
                ex.GetType().Name, ex.Message);
            throw new InvalidOperationException($"Failed to get pad ID: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Parse SCIM filter using Foundation and convert to stored procedure parameters
    /// </summary>
    private static NoteFilterParameters ParseFilterToStoredProcedureParams(string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return new NoteFilterParameters();

        try
        {
            // Use Foundation's SCIM parser
            var scimService = new Looplex.Foundation.SCIMv2.SCIMv2();
            var (sqlWhere, parameters) = scimService.ConvertToSqlFilter(filter);

            // Convert to stored procedure parameters
            var filterParams = new NoteFilterParameters();
            
            // Simple mapping for now - can be enhanced
            if (parameters.ContainsKey("@param_0"))
            {
                var value = parameters["@param_0"]?.ToString();
                if (value != null)
                {
                    if (sqlWhere.Contains("markdown"))
                        filterParams.Text = value;
                    else if (sqlWhere.Contains("active"))
                        filterParams.Active = bool.Parse(value);
                    else if (sqlWhere.Contains("status"))
                        filterParams.Status = int.Parse(value);
                }
            }

            return filterParams;
        }
        catch
        {
            // Return empty parameters if parsing fails
            return new NoteFilterParameters();
        }
    }

    /// <summary>
    /// Adds filter parameters to the command
    /// </summary>
    private void AddFilterParameters(IDbCommand command, NoteFilterParameters filterParams)
    {
        command.Parameters.Add(Dbs.CreateParameter(command, "@filter_ids", (object?)filterParams.Ids ?? DBNull.Value, DbType.String));
        command.Parameters.Add(Dbs.CreateParameter(command, "@filter_uuids", (object?)filterParams.Uuids ?? DBNull.Value, DbType.String));
        command.Parameters.Add(Dbs.CreateParameter(command, "@filter_text", (object?)filterParams.Text ?? DBNull.Value, DbType.String));
        command.Parameters.Add(Dbs.CreateParameter(command, "@filter_pad_guids", (object?)filterParams.PadGuids ?? DBNull.Value, DbType.String));
        command.Parameters.Add(Dbs.CreateParameter(command, "@filter_active", (object?)filterParams.Active ?? DBNull.Value, DbType.Boolean));
        command.Parameters.Add(Dbs.CreateParameter(command, "@filter_status", (object?)filterParams.Status ?? DBNull.Value, DbType.Int32));
        command.Parameters.Add(Dbs.CreateParameter(command, "@filter_created_begin", (object?)filterParams.CreatedBegin ?? DBNull.Value, DbType.DateTime));
        command.Parameters.Add(Dbs.CreateParameter(command, "@filter_created_end", (object?)filterParams.CreatedEnd ?? DBNull.Value, DbType.DateTime));
        command.Parameters.Add(Dbs.CreateParameter(command, "@filter_updated_begin", (object?)filterParams.UpdatedBegin ?? DBNull.Value, DbType.DateTime));
        command.Parameters.Add(Dbs.CreateParameter(command, "@filter_updated_end", (object?)filterParams.UpdatedEnd ?? DBNull.Value, DbType.DateTime));
        command.Parameters.Add(Dbs.CreateParameter(command, "@__dangerouslySetPredicate", DBNull.Value, DbType.String));
    }

    /// <summary>
    /// Executes stored procedure and returns results with count
    /// </summary>
    private async Task<(List<Note> Notes, int TotalCount)> ExecuteStoredProcedureWithCount(
        SqlCommand command, CancellationToken cancellationToken)
    {
        var notes = new List<Note>();
        int totalCount = 0;

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        // First result set: paginated data
        while (await reader.ReadAsync(cancellationToken))
        {
            var note = MapReaderToNote(reader);
            notes.Add(note);
        }

        // Second result set: total count (if @do_count = 1)
        if (await reader.NextResultAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                totalCount = reader.GetInt32("total");
            }
        }

        return (notes, totalCount);
    }

    /// <summary>
    /// Maps database reader data to Note domain entity
    /// </summary>
    private static Note MapReaderToNote(IDataReader reader)
    {
        // Handle GUID conversion for SCIM compatibility
        var idValue = reader["uuid"];
        string idString;
        if (idValue is Guid guid)
        {
            idString = guid.ToString();
        }
        else
        {
            idString = idValue?.ToString() ?? string.Empty;
        }

        return new Note
        {
            Id = idString,
            Name = reader["markdown"].ToString() ?? string.Empty, // Use markdown content as name
            Text = reader["markdown"].ToString() ?? string.Empty, // Use markdown content as text
            Active = Convert.ToBoolean(reader["active"]),
            Status = Convert.ToInt32(reader["status"]),
            CustomFields = reader["custom_fields"].ToString() ?? "{}",
            Schemas = new[] { "urn:looplex:params:scim:schemas:notejam:2.0:Note" },
            Meta = CreateResourceMeta(reader, idString)
        };
    }
    
    private static Looplex.Foundation.SCIMv2.Entities.ResourceMeta CreateResourceMeta(IDataReader reader, string resourceId)
    {
        return new Looplex.Foundation.SCIMv2.Entities.ResourceMeta
        {
            ResourceType = "Note",
            Location = $"https://localhost:7065/scim/v2/notes/{resourceId}", // Full URL for SCIM compliance
            // Version will be set by SCIMv2.cs using GenerateResourceVersion
            Created = ParseDateTime(reader["created_at"]),
            LastModified = ParseDateTime(reader["updated_at"])
        };
    }
    
    private static DateTime ParseDateTime(object? value)
    {
        return DateTime.TryParse(value?.ToString(), out var dateTime) ? dateTime : DateTime.UtcNow;
    }


}


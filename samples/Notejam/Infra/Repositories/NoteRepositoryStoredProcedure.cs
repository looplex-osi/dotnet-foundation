using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Looplex.Foundation.Core.Ports;
using Looplex.Foundation.Core.Helpers;
using Looplex.Foundation.Core.SCIMv2.Queries;
using Looplex.Foundation.Core.SCIMv2.Entities;
using Looplex.Foundation.Core.SCIMv2.Modules;
using Looplex.Samples.Domain.Entities;
using Looplex.Samples.Application;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace Looplex.Samples.Infra.Repositories;

    /// <summary>
    /// Filter parameters for Note stored procedures
    /// </summary>
    public class NoteFilterParameters
    {
        public string? Ids { get; set; }
        public string? Uuids { get; set; }
        public string? Text { get; set; }
        public string? PadGuids { get; set; }
        public bool? Active { get; set; }
        public int? Status { get; set; }
        public DateTime? CreatedBegin { get; set; }
        public DateTime? CreatedEnd { get; set; }
        public DateTime? UpdatedBegin { get; set; }
        public DateTime? UpdatedEnd { get; set; }
    }

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
    private readonly IHttpContextAccessor _httpContextAccessor;

    public NoteRepositoryStoredProcedure(IDbConnections connections, ILogger<NoteRepositoryStoredProcedure> logger, IHttpContextAccessor httpContextAccessor)
    {
        _connections = connections;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Query notes with SCIM v2.0 filtering using ELEGANT Foundation approach
    /// </summary>
    public async Task<(IList<Note> Resources, int TotalCount)> QueryAsync(
        int startIndex, int count, string? filter, CancellationToken cancellationToken = default)
    {
        // Use the elegant implementation by default
        return await QueryAsyncElegant(startIndex, count, filter, cancellationToken);
    }

    /// <summary>
    /// Get notes with optional filtering and pagination
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

    /// <summary>
    /// ELEGANT APPROACH - Using Foundation like Case Management
    /// Simple, clean, 1-line filter processing
    /// </summary>
    public async Task<(IList<Note> Notes, int TotalCount)> QueryAsyncElegant(
        int startIndex, 
        int count, 
        string? filter = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("✨ ELEGANT: Getting notes with Foundation approach: startIndex={StartIndex}, count={Count}, filter={Filter}", 
                startIndex, count, filter);

            // Convert SCIM to Case-Management parameters
            var (page, pageSize) = ConvertScimToPageParameters(startIndex, count);
            
        // ELEGANT: Use Foundation's approach like Case Management
        // Pass allowed attributes to enable filtering
        var allowedAttributes = new HashSet<string> { 
            "id", "externalId", "text", "active", "status", 
            "meta.created", "meta.lastModified" 
        };
        
        // ELEGANT: Pass attribute mapping for meta.created -> n.created_at
        var attributeMapper = new Dictionary<string, string> {
            { "meta.created", "n.created_at" },
            { "meta.lastModified", "n.updated_at" },
            { "active", "n.active" },
            { "text", "n.markdown" },
            { "status", "n.status" },
            { "id", "n.id" },
            { "externalId", "n.external_id" }
        };
        
        string? filters = filter?.ToSqlPredicate(attributeMapper, allowedAttributes);

            await using var dbCommand = await _connections.CommandConnection();
            await using var command = dbCommand.CreateCommand();

            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "USP_notes_pquery";

            // Add stored procedure parameters
            command.Parameters.Add(Dbs.CreateParameter(command, "@page", page, DbType.Int32));
            command.Parameters.Add(Dbs.CreateParameter(command, "@page_size", pageSize, DbType.Int32));
            command.Parameters.Add(Dbs.CreateParameter(command, "@do_count", true, DbType.Boolean));
            command.Parameters.Add(Dbs.CreateParameter(command, "@order_by", "updated_at DESC", DbType.String));
            
            // ELEGANT: Add filter using Foundation's approach (like Case Management)
            if (filters != null)
                command.Parameters.Add(Dbs.CreateParameter(command, "@__dangerouslySetPredicate", filters, DbType.String));

            var (notes, totalCount) = await ExecuteStoredProcedureWithCount((SqlCommand)command, cancellationToken);

            _logger.LogInformation("✨ ELEGANT: Retrieved {Count} notes, total: {TotalCount}", notes.Count, totalCount);
            return (notes, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notes with ELEGANT Foundation approach");
            throw new InvalidOperationException($"Failed to get notes: {ex.Message}", ex);
        }
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
        try
        {
            _logger.LogInformation("🚀 UpdateNoteAsync started - ID: {NoteId}, Text: '{Text}', Active: {Active}, Status: {Status}", 
                id, note.Text, note.Active, note.Status);

            await using var dbCommand = await _connections.CommandConnection();
            await using var command = dbCommand.CreateCommand();

            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "USP_notes_update";
            _logger.LogInformation("📋 Using stored procedure: {StoredProcedure}", command.CommandText);

            command.Parameters.Add(Dbs.CreateParameter(command, "@note_guid", id, DbType.Guid));
            command.Parameters.Add(Dbs.CreateParameter(command, "@name", note.Name, DbType.String));
            command.Parameters.Add(Dbs.CreateParameter(command, "@text", note.Text, DbType.String));
            command.Parameters.Add(Dbs.CreateParameter(command, "@active", note.Active, DbType.Boolean));
            command.Parameters.Add(Dbs.CreateParameter(command, "@status", (int)note.Status, DbType.Int32));

            _logger.LogInformation("🎯 Executing stored procedure with parameters: note_guid={NoteGuid}, name='{Name}', text='{Text}', active={Active}, status={Status}", 
                id, note.Name, note.Text, note.Active, note.Status);

            int rows = await command.ExecuteNonQueryAsync(cancellationToken);

            if (rows == 0)
            {
                _logger.LogWarning("No note found to update with ID: {NoteId}", id);
            }
            else
            {
                _logger.LogInformation("Note updated successfully. Rows affected: {RowsAffected}", rows);
            }

            return rows;
        }
        catch (Exception ex)
        {
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
        _logger.LogInformation("🔍 UpdateAsync called with ID: {Id}, Resource: {ResourceName}", id, resource?.Name);
        
        await UpdateNoteAsync(Guid.Parse(id), resource, cancellationToken);
        
        // Set the ID on the resource before returning
        resource.Id = id;
        _logger.LogInformation("🔧 Set resource.Id to: {ResourceId}", resource.Id);
        
        return resource;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var result = await DeleteNoteAsync(Guid.Parse(id), cancellationToken);
        return result > 0;
    }



    /// <summary>
    /// Parse SCIM filter using Foundation generic services and convert to stored procedure parameters.
    /// Implements RFC 7644 Section 3.4.2.2 - Filtering using generic SCIM services.
    /// [RFC 7644 Section 3.4.2.2](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.2.2)
    /// </summary>
    private static NoteFilterParameters ParseFilterToStoredProcedureParams(string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return new NoteFilterParameters();

        try
        {
            // Use generic SCIM filter processor from Foundation
            var (sqlWhere, parameters) = Looplex.Foundation.Core.SCIMv2.SCIMv2.ScimFilterProcessor.ProcessFilter<Note>(filter);

            // Convert to stored procedure parameters using generic services
            var filterParams = new NoteFilterParameters();
            
            // Map parameters to stored procedure format using generic type converter
            if (parameters.ContainsKey("@param_0"))
            {
                var value = parameters["@param_0"]?.ToString();
                if (value != null)
                {
                    if (sqlWhere.Contains("text"))
                        filterParams.Text = Looplex.Foundation.Core.SCIMv2.SCIMv2.ScimTypeConverter.ParseString(value);
                    else if (sqlWhere.Contains("active"))
                        filterParams.Active = Looplex.Foundation.Core.SCIMv2.SCIMv2.ScimTypeConverter.ParseBoolean(value);
                    else if (sqlWhere.Contains("status"))
                        filterParams.Status = Looplex.Foundation.Core.SCIMv2.SCIMv2.ScimTypeConverter.ParseInteger(value);
                }
            }

            return filterParams;
        }
        catch (Exception ex)
        {
            // Log warning and return empty parameters if parsing fails
            Console.WriteLine($"Warning: Failed to parse SCIM filter '{filter}': {ex.Message}");
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
        /// Maps database reader data to Note domain entity using Notejam-specific mapping.
        /// Implements RFC 7643 Section 2.1 - Core Schema mapping with Notejam database structure.
        /// [RFC 7643 Section 2.1](https://datatracker.ietf.org/doc/html/rfc7643#section-2.1)
        /// </summary>
        private Note MapReaderToNote(IDataReader reader)
        {
            var note = new Note();
            
            // Map Id from uuid column (Notejam-specific)
            note.Id = reader["uuid"].ToString();
            
            // Map externalId as optional field following RFC 7643 Section 2.1 - Core Schema
            // RFC 7643 Section 2.1 defines externalId as optional identifier for external system mapping
            // [RFC 7643 Section 2.1](https://datatracker.ietf.org/doc/html/rfc7643#section-2.1)
            if (reader["external_id"] != DBNull.Value)
                note.ExternalId = reader["external_id"].ToString();
            
            // Map Notejam-specific properties
            note.Name = Looplex.Foundation.Core.SCIMv2.SCIMv2.ScimTypeConverter.ParseString(reader["markdown"]);
            note.Text = Looplex.Foundation.Core.SCIMv2.SCIMv2.ScimTypeConverter.ParseString(reader["markdown"]);
            note.Active = Looplex.Foundation.Core.SCIMv2.SCIMv2.ScimTypeConverter.ParseBoolean(reader["active"]);
            note.Status = Looplex.Foundation.Core.SCIMv2.SCIMv2.ScimTypeConverter.ParseInteger(reader["status"]);
            note.CustomFields = Looplex.Foundation.Core.SCIMv2.SCIMv2.ScimTypeConverter.ParseString(reader["custom_fields"]);
            note.Schemas = new[] { "urn:looplex:params:scim:schemas:notejam:2.0:Note" };
            
            // Create Meta using Foundation method (agnostic)
            note.Meta = Looplex.Foundation.Core.SCIMv2.SCIMv2.CreateResourceMeta(reader, note.Id, "Note", _httpContextAccessor);
            
            return note;
        }


}


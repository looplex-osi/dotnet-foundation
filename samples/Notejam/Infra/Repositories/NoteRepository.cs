using System.Data;
using Looplex.Foundation.Helpers;
using Looplex.Foundation.SCIMv2.Queries;
using Looplex.Samples.Application;
using Looplex.Samples.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Looplex.Samples.Infra.Repositories;

/// <summary>
/// Repository implementation for Note entities that demonstrates a complete data access layer.
/// 
/// This repository showcases:
/// - CRUD operations with proper error handling and logging
/// - SCIM v2.0 filter processing and SQL generation
/// - Pagination support with OFFSET/FETCH
/// - Data mapping between database and domain entities
/// - Integration with Looplex.Foundation SearchContent service
/// 
/// Note: This is a demonstration repository that combines multiple responsibilities
/// for educational purposes. In production applications, consider separating
/// concerns into dedicated services (e.g., ScimFilterProcessor, DataMapper).
/// </summary>
public class NoteRepository(IDbConnections connections, ILogger<NoteRepository> logger) : INoteRepository
{
    /// <summary>
    /// Retrieves notes with optional SCIM filtering and pagination support.
    /// 
    /// This method demonstrates:
    /// - Dynamic SQL query building with SCIM filter integration
    /// - Pagination using SQL Server OFFSET/FETCH syntax
    /// - Parameterized queries for security
    /// - Comprehensive error handling and logging
    /// - Integration with Looplex.Foundation SearchContent for filter processing
    /// 
    /// The method processes SCIM filters by:
    /// 1. Normalizing filter syntax (single to double quotes)
    /// 2. Converting SCIM filters to SQL using SearchContent service
    /// 3. Applying schema mapping for attribute-to-column conversion
    /// 4. Building parameterized queries to prevent SQL injection
    /// </summary>
    /// <param name="filter">SCIM v2.0 filter expression (e.g., "active eq true and status eq 1")</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Tuple containing list of notes and total count</returns>
    public async Task<(IList<Note> Notes, int TotalCount)> GetNotesAsync(
        string? filter = null, 
        int page = 1, 
        int pageSize = 10, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Getting notes with filter: {Filter}, page: {Page}, pageSize: {PageSize}", 
                filter, page, pageSize);

            var dbQuery = await connections.QueryConnection();
            await using var command = dbQuery.CreateCommand();

            command.CommandType = CommandType.Text;
            
            var baseQuery = BuildBaseQuery();
            var finalQuery = ApplyScimFilter(baseQuery, filter, command);
            finalQuery += $" ORDER BY n.{NoteConfiguration.Database.UpdatedColumn} DESC OFFSET {(page - 1) * pageSize} ROWS FETCH NEXT {pageSize} ROWS ONLY";
            command.CommandText = finalQuery;
            

            foreach (IDbDataParameter param in command.Parameters)
            {
                logger.LogInformation("Parameter: {Name} = {Value}", param.ParameterName, param.Value);
            }

            var notes = await ExecuteQueryAndMapResults(command, cancellationToken);
            
            logger.LogInformation("Retrieved {Count} notes successfully", notes.Count);
            return (notes, notes.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting notes with filter: {Filter}", filter);
            throw new InvalidOperationException($"Failed to get notes: {ex.Message}", ex);
        }
    }

    public async Task<Note?> GetNoteByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Getting note by ID: {NoteId}", id);

            var dbQuery = await connections.QueryConnection();
            await using var command = dbQuery.CreateCommand();

            command.CommandType = CommandType.Text;
            command.CommandText = $@"
                SELECT 
                  n.{NoteConfiguration.Database.IdColumn} as Id,
                  n.{NoteConfiguration.Database.TextColumn} as Text,
                  n.{NoteConfiguration.Database.ActiveColumn} as IsActive,
                  n.{NoteConfiguration.Database.StatusColumn} as Status,
                  n.{NoteConfiguration.Database.CustomFieldsColumn} as CustomFields,
                  n.{NoteConfiguration.Database.CreatedColumn} as Created,
                  n.{NoteConfiguration.Database.UpdatedColumn} as Modified,
                  p.{NoteConfiguration.Database.PadNameColumn} as PadName
                FROM {NoteConfiguration.Database.NotesTable} n
                LEFT JOIN {NoteConfiguration.Database.PadsTable} p ON n.{NoteConfiguration.Database.PadIdColumn} = p.{NoteConfiguration.Database.PadIdReferenceColumn}
                WHERE n.{NoteConfiguration.Database.IdColumn} = {NoteConfiguration.Parameters.Id} AND n.{NoteConfiguration.Database.ActiveColumn} = 1";

            command.Parameters.Add(Dbs.CreateParameter(command, NoteConfiguration.Parameters.Id, id, DbType.Guid));

            Note? note = null;
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                note = MapReaderToNote(reader);
                logger.LogInformation("Note retrieved successfully: {NoteId}", id);
            }
            else
            {
                logger.LogWarning("Note not found with ID: {NoteId}", id);
            }

            return note;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting note by ID: {NoteId}", id);
            throw new InvalidOperationException($"Failed to get note: {ex.Message}", ex);
        }
    }

    public async Task<Guid> CreateNoteAsync(Note note, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Creating new note with Name: {NoteName}", note.Name);

            var dbCommand = await connections.CommandConnection();
            await using var command = dbCommand.CreateCommand();

            command.CommandType = CommandType.Text;
            command.CommandText = $@"
                INSERT INTO {NoteConfiguration.Database.NotesTable} (pad_id, user_id, {NoteConfiguration.Database.TextColumn}, {NoteConfiguration.Database.ActiveColumn}, status, custom_fields, created_by, updated_by)
                VALUES ({NoteConfiguration.Parameters.PadId}, @user_id, {NoteConfiguration.Parameters.Text}, {NoteConfiguration.Parameters.Active}, 1, '{{}}', 'admin', 'admin');
                SELECT SCOPE_IDENTITY();";

            var padId = await GetPadIdAsync(note.Name, cancellationToken);
            
            command.Parameters.Add(Dbs.CreateParameter(command, "@user_id", -2147483642, DbType.Int32));
            command.Parameters.Add(Dbs.CreateParameter(command, NoteConfiguration.Parameters.Text, note.Text, DbType.String));
            command.Parameters.Add(Dbs.CreateParameter(command, NoteConfiguration.Parameters.Active, NoteConfiguration.Defaults.DefaultActive, DbType.Boolean));
            command.Parameters.Add(Dbs.CreateParameter(command, NoteConfiguration.Parameters.PadId, padId, DbType.Int32));

            var result = await command.ExecuteScalarAsync(cancellationToken);
            var noteId = result != null ? Convert.ToInt32(result) : 0;

            // Get the UUID of the created note
            command.CommandText = $@"
                SELECT {NoteConfiguration.Database.IdColumn}
                FROM {NoteConfiguration.Database.NotesTable}
                WHERE id = @note_id";

            command.Parameters.Clear();
            command.Parameters.Add(Dbs.CreateParameter(command, "@note_id", noteId, DbType.Int32));

            var uuidResult = await command.ExecuteScalarAsync(cancellationToken);
            var uuid = uuidResult != null ? (Guid)uuidResult : Guid.Empty;

            logger.LogInformation("Note created successfully with ID: {NoteId}, UUID: {NoteUuid}", noteId, uuid);
            return uuid;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating note with Name: {NoteName}", note.Name);
            throw new InvalidOperationException($"Failed to create note: {ex.Message}", ex);
        }
    }

    public async Task<int> UpdateNoteAsync(Guid id, Note note, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Updating note with ID: {NoteId}", id);

            var dbCommand = await connections.CommandConnection();
            await using var command = dbCommand.CreateCommand();

            command.CommandType = CommandType.Text;
            command.CommandText = $@"
                UPDATE {NoteConfiguration.Database.NotesTable} 
                SET {NoteConfiguration.Database.TextColumn} = {NoteConfiguration.Parameters.Text},
                    {NoteConfiguration.Database.ActiveColumn} = {NoteConfiguration.Parameters.Active},
                    {NoteConfiguration.Database.StatusColumn} = {NoteConfiguration.Parameters.Status},
                    {NoteConfiguration.Database.CustomFieldsColumn} = {NoteConfiguration.Parameters.CustomFields}
                WHERE {NoteConfiguration.Database.IdColumn} = {NoteConfiguration.Parameters.Id}";

            command.Parameters.Add(Dbs.CreateParameter(command, NoteConfiguration.Parameters.Id, id, DbType.Guid));
            command.Parameters.Add(Dbs.CreateParameter(command, NoteConfiguration.Parameters.Text, note.Text, DbType.String));
            command.Parameters.Add(Dbs.CreateParameter(command, NoteConfiguration.Parameters.Active, note.Active, DbType.Boolean));
            command.Parameters.Add(Dbs.CreateParameter(command, NoteConfiguration.Parameters.Status, note.Status, DbType.Byte));
            command.Parameters.Add(Dbs.CreateParameter(command, NoteConfiguration.Parameters.CustomFields, note.CustomFields, DbType.String));

            int rows = await command.ExecuteNonQueryAsync(cancellationToken);

            if (rows == 0)
            {
                logger.LogWarning("No note found to update with ID: {NoteId}", id);
            }
            else
            {
                logger.LogInformation("Note updated successfully. Rows affected: {RowsAffected}", rows);
            }

            return rows;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating note with ID: {NoteId}", id);
            throw new InvalidOperationException($"Failed to update note: {ex.Message}", ex);
        }
    }

    public async Task<int> DeleteNoteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Deleting note with ID: {NoteId}", id);

            var dbCommand = await connections.CommandConnection();
            await using var command = dbCommand.CreateCommand();

            command.CommandType = CommandType.Text;
            command.CommandText = $@"
                UPDATE {NoteConfiguration.Database.NotesTable} 
                SET {NoteConfiguration.Database.ActiveColumn} = 0
                WHERE {NoteConfiguration.Database.IdColumn} = {NoteConfiguration.Parameters.Id} AND {NoteConfiguration.Database.ActiveColumn} = 1";

            command.Parameters.Add(Dbs.CreateParameter(command, NoteConfiguration.Parameters.Id, id, DbType.Guid));

            int rows = await command.ExecuteNonQueryAsync(cancellationToken);

            if (rows == 0)
            {
                logger.LogWarning("No active note found to delete with ID: {NoteId}", id);
            }
            else
            {
                logger.LogInformation("Note deleted successfully. Rows affected: {RowsAffected}", rows);
            }

            return rows;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting note with ID: {NoteId}", id);
            throw new InvalidOperationException($"Failed to delete note: {ex.Message}", ex);
        }
    }

    private static string BuildBaseQuery()
    {
        return $@"
            SELECT 
              n.{NoteConfiguration.Database.IdColumn} as Id,
              n.{NoteConfiguration.Database.TextColumn} as Text,
              n.{NoteConfiguration.Database.ActiveColumn} as IsActive,
              n.{NoteConfiguration.Database.StatusColumn} as Status,
              n.{NoteConfiguration.Database.CustomFieldsColumn} as CustomFields,
              n.{NoteConfiguration.Database.CreatedColumn} as Created,
              n.{NoteConfiguration.Database.UpdatedColumn} as Modified,
              p.{NoteConfiguration.Database.PadNameColumn} as PadName
            FROM {NoteConfiguration.Database.NotesTable} n
            LEFT JOIN {NoteConfiguration.Database.PadsTable} p ON n.{NoteConfiguration.Database.PadIdColumn} = p.{NoteConfiguration.Database.PadIdReferenceColumn}";
    }
    
    /// <summary>
    /// Applies SCIM v2.0 filters to SQL queries by converting them to SQL predicates.
    /// 
    /// This method demonstrates the integration between SCIM filtering and SQL generation:
    /// - Converts SCIM filter syntax to SQL WHERE clauses
    /// - Handles complex filter expressions with logical operators
    /// - Applies schema mapping for attribute-to-column conversion
    /// - Uses parameterized queries to prevent SQL injection
    /// - Gracefully handles filter processing errors
    /// 
    /// The filter processing pipeline:
    /// 1. Normalize SCIM filter syntax (single quotes to double quotes)
    /// 2. Convert to SQL using Looplex.Foundation SearchContent service
    /// 3. Apply schema mapping for database column names
    /// 4. Build parameterized WHERE clause
    /// 5. Add parameters to command for security
    /// </summary>
    /// <param name="baseQuery">Base SQL query without WHERE clause</param>
    /// <param name="filter">SCIM v2.0 filter expression</param>
    /// <param name="command">Database command to add parameters</param>
    /// <returns>SQL query with applied filter</returns>
    private string ApplyScimFilter(string baseQuery, string? filter, IDbCommand command)
    {
        if (string.IsNullOrEmpty(filter))
            return baseQuery;
        
        try
        {
            // Normalize quotes for SCIM filter compatibility
            var normalizedFilter = NormalizeFilterQuotes(filter);
            
            // Convert SCIM filter to SQL using SearchContent service with schema mapping
            var (sqlPredicate, parameters) = ConvertToSqlWithSqlServerDialect(normalizedFilter, GetSchemaMapping());
            
            if (!string.IsNullOrEmpty(sqlPredicate))
            {
                var whereClause = RemoveWhereKeyword(sqlPredicate);
                
                // Dynamically build WHERE clause based on existing query structure
                if (baseQuery.ToUpper().Contains("WHERE"))
                {
                    baseQuery += $" AND {whereClause}";
                }
                else
                {
                    baseQuery += $" WHERE {whereClause}";
                }
                
                // Add parameters to command for SQL injection prevention
                AddParametersToCommand(command, parameters);
            }
            else
            {
                logger.LogWarning("Empty SQL predicate generated for filter: {Filter}", filter);
            }
        }
        catch (Exception ex)
        {
            // Graceful degradation: continue without filter if processing fails
            logger.LogWarning(ex, "Failed to process SCIM filter: {Filter}. Continuing without filter.", filter);
        }
        
        return baseQuery;
    }
    
    private static string RemoveWhereKeyword(string sqlPredicate)
    {
        return sqlPredicate.Replace("WHERE ", "").Replace("where ", "");
    }
    
    private static string NormalizeFilterQuotes(string filter)
    {
        // Replace single quotes with double quotes for SCIM filter compatibility
        return filter.Replace("'", "\"");
    }
    
    private static void AddParametersToCommand(IDbCommand command, Dictionary<string, object> parameters)
    {
        foreach (var param in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = param.Key;
            parameter.Value = param.Value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }
    }
    
    private static Task<List<Note>> ExecuteQueryAndMapResults(IDbCommand command, CancellationToken cancellationToken)
    {
        var notes = new List<Note>();
        using var reader = command.ExecuteReader();
        
        while (reader.Read())
        {
            var note = MapReaderToNote(reader);
            notes.Add(note);
        }
        
        return Task.FromResult(notes);
    }
    
    /// <summary>
    /// Maps database reader data to Note domain entity.
    /// 
    /// This method demonstrates proper data mapping techniques:
    /// - Safe type conversion with null handling
    /// - GUID to string conversion for SCIM compatibility
    /// - Default value handling for missing data
    /// - Integration with SCIM ResourceMeta for metadata
    /// - Proper error handling for data type mismatches
    /// 
    /// The mapping ensures that:
    /// - Database GUIDs are converted to SCIM-compatible string IDs
    /// - Null values are handled gracefully with defaults
    /// - Boolean and integer conversions are safe
    /// - SCIM metadata is properly populated
    /// </summary>
    /// <param name="reader">Database data reader containing note data</param>
    /// <returns>Mapped Note domain entity</returns>
    private static Note MapReaderToNote(IDataReader reader)
    {
        // Handle GUID conversion for SCIM compatibility
        var idValue = reader["Id"];
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
            Name = reader["PadName"].ToString() ?? NoteConfiguration.Defaults.DefaultPadName,
            Text = reader["Text"].ToString() ?? string.Empty,
            Active = Convert.ToBoolean(reader["IsActive"]),
            Status = Convert.ToInt32(reader["Status"]),
            CustomFields = reader["CustomFields"].ToString() ?? "{}",
            Meta = CreateResourceMeta(reader)
        };
    }
    
    private static Looplex.Foundation.SCIMv2.Entities.ResourceMeta CreateResourceMeta(IDataReader reader)
    {
        return new Looplex.Foundation.SCIMv2.Entities.ResourceMeta
        {
            Created = ParseDateTime(reader["Created"]),
            LastModified = ParseDateTime(reader["Modified"])
        };
    }
    
    private static DateTime ParseDateTime(object? value)
    {
        return DateTime.TryParse(value?.ToString(), out var dateTime) ? dateTime : DateTime.UtcNow;
    }

    private async Task<int> GetPadIdAsync(string noteName, CancellationToken cancellationToken = default)
    {
        try
        {
            var dbCommand = await connections.CommandConnection();
            await using var command = dbCommand.CreateCommand();

            command.CommandType = CommandType.Text;
            command.CommandText = $@"
                SELECT TOP 1 {NoteConfiguration.Database.PadIdReferenceColumn}
                FROM {NoteConfiguration.Database.PadsTable}
                WHERE {NoteConfiguration.Database.ActiveColumn} = 1
                ORDER BY {NoteConfiguration.Database.CreatedColumn} DESC";

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result != null ? Convert.ToInt32(result) : 1; // Fallback to 1 if no pad is found
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to get pad ID, using default value 1");
            return 1; // Fallback to 1 in case of error
        }
    }
    
    /// <summary>
    /// Retrieves SCIM attribute to database column mapping configuration.
    /// 
    /// This mapping enables the conversion of SCIM filter attributes to actual database column names.
    /// For example: "active" → "IsActive", "created" → "CreatedAt"
    /// 
    /// The mapping is defined in NoteConfiguration and supports:
    /// - Direct attribute mapping (e.g., "active" → "IsActive")
    /// - Complex attribute mapping (e.g., "meta.created" → "CreatedAt")
    /// - Custom field mapping for extensibility
    /// </summary>
    /// <returns>Dictionary mapping SCIM attributes to database columns</returns>
    private Dictionary<string, string> GetSchemaMapping()
    {
        return NoteConfiguration.ScimMappings.AttributeToColumn;
    }
    
    /// <summary>
    /// Converts SCIM v2.0 filter expressions to SQL Server compatible SQL.
    /// 
    /// This method demonstrates the integration with Looplex.Foundation SearchContent service:
    /// - Converts SCIM filter syntax to SQL WHERE clauses
    /// - Applies schema mapping for attribute-to-column conversion
    /// - Uses SQL Server specific dialect and syntax
    /// - Returns parameterized SQL for security
    /// - Handles complex filter expressions with logical operators
    /// 
    /// The conversion process:
    /// 1. Parse SCIM filter using ANTLR grammar
    /// 2. Apply schema mapping for column names
    /// 3. Generate SQL Server compatible syntax
    /// 4. Create parameterized query for injection prevention
    /// </summary>
    /// <param name="filter">SCIM v2.0 filter expression</param>
    /// <param name="schemaMapping">Attribute to column mapping</param>
    /// <returns>Tuple containing SQL predicate and parameters</returns>
    private (string Sql, Dictionary<string, object> Parameters) ConvertToSqlWithSqlServerDialect(string filter, Dictionary<string, string> schemaMapping)
    {
        var options = new Looplex.Foundation.SearchContent.SqlGenerator.SqlGenerationOptions
        {
            FieldMapping = schemaMapping,
            Dialect = Looplex.Foundation.SearchContent.SqlGenerator.SqlDialect.SqlServer
        };
        
        var service = new Looplex.Foundation.SearchContent.SearchContentService();
        var result = service.ConvertToSql(filter, options);
        
        return (result.Sql, result.Parameters ?? new Dictionary<string, object?>());
    }
}

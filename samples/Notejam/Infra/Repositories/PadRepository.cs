using System.Data;
using Looplex.Foundation.Ports;
using Looplex.Foundation.Helpers;
using Looplex.Foundation.SCIMv2.Queries;
using Looplex.Samples.Application;
using Looplex.Samples.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Looplex.Samples.Infra.Repositories;

/// <summary>
/// Repository implementation for Pad entities that demonstrates a complete data access layer.
/// 
/// This repository showcases:
/// - CRUD operations with proper error handling and logging
/// - SCIM v2.0 filter processing and SQL generation
/// - Pagination support with OFFSET/FETCH
/// - Data mapping between database and domain entities
/// - Integration with Looplex.Foundation SearchContent service
/// - Multiple query overloads for different use cases
/// 
/// Note: This is a demonstration repository that combines multiple responsibilities
/// for educational purposes. In production applications, consider separating
/// concerns into dedicated services (e.g., ScimFilterProcessor, DataMapper).
/// </summary>
public class PadRepository : IPadRepository
{
    private readonly IDbConnections _connections;
    private readonly ILogger<PadRepository> _logger;

    public PadRepository(IDbConnections connections, ILogger<PadRepository> logger)
    {
        _connections = connections;
        _logger = logger;
    }

    public async Task<List<Pad>> GetPadsAsync(CancellationToken cancellationToken = default)
    {
        return await GetPadsAsync(null, cancellationToken);
    }

    public async Task<List<Pad>> GetPadsAsync(string? filter, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting pads with filter: {Filter}", filter);

            var dbCommand = await _connections.CommandConnection();
            await using var command = dbCommand.CreateCommand();

            command.CommandType = CommandType.Text;
            
            var baseQuery = BuildBaseQuery();
            var finalQuery = ApplyScimFilter(baseQuery, filter, command);
            command.CommandText = finalQuery;

            var pads = await ExecuteQueryAndMapResults(command, cancellationToken);

            _logger.LogInformation("Retrieved {Count} pads", pads.Count);
            return pads;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pads");
            throw new InvalidOperationException($"Failed to get pads: {ex.Message}", ex);
        }
    }

    public async Task<List<Pad>> GetPadsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await GetPadsAsync(null, page, pageSize, cancellationToken);
    }

    public async Task<List<Pad>> GetPadsAsync(string? filter, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting pads with filter: '{Filter}', page: {Page}, size: {PageSize}", filter, page, pageSize);

            var dbCommand = await _connections.CommandConnection();
            await using var command = dbCommand.CreateCommand();

            command.CommandType = CommandType.Text;
            
            var baseQuery = BuildBaseQuery();
            var finalQuery = ApplyScimFilter(baseQuery, filter, command);
            finalQuery += $" ORDER BY p.{PadConfiguration.Database.UpdatedColumn} DESC OFFSET {(page - 1) * pageSize} ROWS FETCH NEXT {pageSize} ROWS ONLY";
            command.CommandText = finalQuery;
            


            var pads = await ExecuteQueryAndMapResults(command, cancellationToken);

            _logger.LogInformation("Retrieved {Count} pads for page {Page}", pads.Count, page);
            return pads;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pads with pagination");
            throw new InvalidOperationException($"Failed to get pads: {ex.Message}", ex);
        }
    }

    public async Task<Pad?> GetPadByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting pad by ID: {PadId}", id);

            var dbCommand = await _connections.CommandConnection();
            await using var command = dbCommand.CreateCommand();

            command.CommandType = CommandType.Text;
            command.CommandText = $@"
                SELECT 
                  p.{PadConfiguration.Database.IdColumn} as Id,
                  p.{PadConfiguration.Database.NameColumn} as Name,
                  p.{PadConfiguration.Database.ActiveColumn} as IsActive,
                  p.{PadConfiguration.Database.StatusColumn} as Status,
                  p.{PadConfiguration.Database.CustomFieldsColumn} as CustomFields,
                  p.{PadConfiguration.Database.CreatedColumn} as Created,
                  p.{PadConfiguration.Database.UpdatedColumn} as Modified
                FROM {PadConfiguration.Database.PadsTable} p
                WHERE p.{PadConfiguration.Database.IdColumn} = {PadConfiguration.Parameters.Id}";

            command.Parameters.Add(Dbs.CreateParameter(command, PadConfiguration.Parameters.Id, id, DbType.Guid));

            Pad? pad = null;
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                pad = MapReaderToPad(reader);
                _logger.LogInformation("Pad retrieved successfully: {PadId}", id);
            }
            else
            {
                _logger.LogWarning("Pad not found with ID: {PadId}", id);
            }

            return pad;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pad by ID: {PadId}", id);
            throw new InvalidOperationException($"Failed to get pad: {ex.Message}", ex);
        }
    }

    public async Task<Pad?> GetPadByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting pad by ID for update: {PadId}", id);

            var dbCommand = await _connections.CommandConnection();
            await using var command = dbCommand.CreateCommand();

            command.CommandType = CommandType.Text;
            command.CommandText = $@"
                SELECT 
                  p.{PadConfiguration.Database.IdColumn} as Id,
                  p.{PadConfiguration.Database.NameColumn} as Name,
                  p.{PadConfiguration.Database.ActiveColumn} as IsActive,
                  p.{PadConfiguration.Database.StatusColumn} as Status,
                  p.{PadConfiguration.Database.CustomFieldsColumn} as CustomFields,
                  p.{PadConfiguration.Database.CreatedColumn} as Created,
                  p.{PadConfiguration.Database.UpdatedColumn} as Modified
                FROM {PadConfiguration.Database.PadsTable} p
                WHERE p.{PadConfiguration.Database.IdColumn} = {PadConfiguration.Parameters.Id}";

            command.Parameters.Add(Dbs.CreateParameter(command, PadConfiguration.Parameters.Id, id, DbType.Guid));

            Pad? pad = null;
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                pad = MapReaderToPad(reader);
                _logger.LogInformation("Pad retrieved successfully for update: {PadId}", id);
            }
            else
            {
                _logger.LogWarning("Pad not found with ID for update: {PadId}", id);
            }

            return pad;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pad by ID for update: {PadId}", id);
            throw new InvalidOperationException($"Failed to get pad for update: {ex.Message}", ex);
        }
    }

    public async Task<Guid> CreatePadAsync(Pad pad, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating new pad with Name: {PadName}", pad.Name);

            var dbCommand = await _connections.CommandConnection();
            await using var command = dbCommand.CreateCommand();

            command.CommandType = CommandType.Text;
            command.CommandText = $@"
                INSERT INTO {PadConfiguration.Database.PadsTable} (user_id, {PadConfiguration.Database.NameColumn}, {PadConfiguration.Database.ActiveColumn}, status, custom_fields, created_by, updated_by)
                VALUES (@user_id, {PadConfiguration.Parameters.Name}, {PadConfiguration.Parameters.Active}, {PadConfiguration.Parameters.Status}, {PadConfiguration.Parameters.CustomFields}, 'admin', 'admin');
                SELECT SCOPE_IDENTITY();";

            command.Parameters.Add(Dbs.CreateParameter(command, "@user_id", -2147483642, DbType.Int32));
            command.Parameters.Add(Dbs.CreateParameter(command, PadConfiguration.Parameters.Name, pad.Name, DbType.String));
            command.Parameters.Add(Dbs.CreateParameter(command, PadConfiguration.Parameters.Active, pad.Active, DbType.Boolean));
            command.Parameters.Add(Dbs.CreateParameter(command, PadConfiguration.Parameters.Status, pad.Status, DbType.Byte));
            command.Parameters.Add(Dbs.CreateParameter(command, PadConfiguration.Parameters.CustomFields, pad.CustomFields, DbType.String));

            var result = await command.ExecuteScalarAsync(cancellationToken);
            var padId = result != null ? Convert.ToInt32(result) : 0;

            // Get the UUID of the created pad
            command.CommandText = $@"
                SELECT {PadConfiguration.Database.IdColumn}
                FROM {PadConfiguration.Database.PadsTable}
                WHERE id = @pad_id";

            command.Parameters.Clear();
            command.Parameters.Add(Dbs.CreateParameter(command, "@pad_id", padId, DbType.Int32));

            var uuidResult = await command.ExecuteScalarAsync(cancellationToken);
            var uuid = uuidResult != null ? (Guid)uuidResult : Guid.Empty;

            _logger.LogInformation("Pad created successfully with ID: {PadId}, UUID: {PadUuid}", padId, uuid);
            return uuid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating pad with Name: {PadName}", pad.Name);
            throw new InvalidOperationException($"Failed to create pad: {ex.Message}", ex);
        }
    }

    public async Task<int> UpdatePadAsync(Guid id, Pad pad, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating pad with ID: {PadId}", id);

            var dbCommand = await _connections.CommandConnection();
            await using var command = dbCommand.CreateCommand();

            command.CommandType = CommandType.Text;
            command.CommandText = $@"
                UPDATE {PadConfiguration.Database.PadsTable} 
                SET {PadConfiguration.Database.NameColumn} = {PadConfiguration.Parameters.Name},
                    {PadConfiguration.Database.ActiveColumn} = {PadConfiguration.Parameters.Active},
                    {PadConfiguration.Database.StatusColumn} = {PadConfiguration.Parameters.Status},
                    {PadConfiguration.Database.CustomFieldsColumn} = {PadConfiguration.Parameters.CustomFields}
                WHERE {PadConfiguration.Database.IdColumn} = {PadConfiguration.Parameters.Id}";

            command.Parameters.Add(Dbs.CreateParameter(command, PadConfiguration.Parameters.Id, id, DbType.Guid));
            command.Parameters.Add(Dbs.CreateParameter(command, PadConfiguration.Parameters.Name, pad.Name, DbType.String));
            command.Parameters.Add(Dbs.CreateParameter(command, PadConfiguration.Parameters.Active, pad.Active, DbType.Boolean));
            command.Parameters.Add(Dbs.CreateParameter(command, PadConfiguration.Parameters.Status, pad.Status, DbType.Byte));
            command.Parameters.Add(Dbs.CreateParameter(command, PadConfiguration.Parameters.CustomFields, pad.CustomFields, DbType.String));

            int rows = await command.ExecuteNonQueryAsync(cancellationToken);

            if (rows == 0)
            {
                _logger.LogWarning("No pad found to update with ID: {PadId}", id);
            }
            else
            {
                _logger.LogInformation("Pad updated successfully. Rows affected: {RowsAffected}", rows);
            }

            return rows;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating pad with ID: {PadId}", id);
            throw new InvalidOperationException($"Failed to update pad: {ex.Message}", ex);
        }
    }

    public async Task<int> DeletePadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting pad with ID: {PadId}", id);

            var dbCommand = await _connections.CommandConnection();
            await using var command = dbCommand.CreateCommand();

            command.CommandType = CommandType.Text;
            command.CommandText = $@"
                UPDATE {PadConfiguration.Database.PadsTable} 
                SET {PadConfiguration.Database.ActiveColumn} = 0
                WHERE {PadConfiguration.Database.IdColumn} = {PadConfiguration.Parameters.Id} AND {PadConfiguration.Database.ActiveColumn} = 1";

            command.Parameters.Add(Dbs.CreateParameter(command, PadConfiguration.Parameters.Id, id, DbType.Guid));

            int rows = await command.ExecuteNonQueryAsync(cancellationToken);

            if (rows == 0)
            {
                _logger.LogWarning("No active pad found to delete with ID: {PadId}", id);
            }
            else
            {
                _logger.LogInformation("Pad deleted successfully. Rows affected: {RowsAffected}", rows);
            }

            return rows;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting pad with ID: {PadId}", id);
            throw new InvalidOperationException($"Failed to delete pad: {ex.Message}", ex);
        }
    }

    private static string BuildBaseQuery()
    {
        return $@"
            SELECT 
              p.{PadConfiguration.Database.IdColumn} as Id,
              p.{PadConfiguration.Database.NameColumn} as Name,
              p.{PadConfiguration.Database.ActiveColumn} as IsActive,
              p.{PadConfiguration.Database.StatusColumn} as Status,
              p.{PadConfiguration.Database.CustomFieldsColumn} as CustomFields,
              p.{PadConfiguration.Database.CreatedColumn} as Created,
              p.{PadConfiguration.Database.UpdatedColumn} as Modified
            FROM {PadConfiguration.Database.PadsTable} p";
    }
    
    private static string RemoveWhereKeyword(string sqlPredicate)
    {
        return sqlPredicate.Replace("WHERE ", "").Replace("where ", "");
    }
    
    /// <summary>
    /// Normalizes SCIM filter quotes to use double quotes instead of single quotes
    /// </summary>
    private static string NormalizeFilterQuotes(string? filter)
    {
        if (string.IsNullOrEmpty(filter))
            return filter ?? string.Empty;
            
        // Replace single quotes with double quotes for string values
        // This handles cases like: name eq 'Test Pad' -> name eq "Test Pad"
        var normalized = filter.Replace("'", "\"");
        
        // Handle edge cases where we might have double quotes already
        // This is a simple approach - in production you might want more sophisticated parsing
        return normalized;
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
    
    private static Pad MapReaderToPad(IDataReader reader)
    {
        // Handle GUID conversion properly
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

        return new Pad
        {
            Id = idString,
            Name = reader["Name"].ToString() ?? PadConfiguration.Defaults.DefaultName,
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
    
    private string ApplyScimFilter(string baseQuery, string? filter, IDbCommand command)
    {
        if (string.IsNullOrEmpty(filter))
            return baseQuery;
        
        try
        {
            var schemaMapping = GetSchemaMapping();
            
            // Normalize filter to use double quotes instead of single quotes
            var normalizedFilter = NormalizeFilterQuotes(filter);
            var (sqlPredicate, parameters) = ConvertToSqlWithSqlServerDialect(normalizedFilter, schemaMapping);
            
            if (!string.IsNullOrEmpty(sqlPredicate))
            {
                var whereClause = RemoveWhereKeyword(sqlPredicate);
                
                // Check if baseQuery already has WHERE clause
                if (baseQuery.ToUpper().Contains("WHERE"))
                {
                    baseQuery += $" AND {whereClause}";
                }
                else
                {
                    baseQuery += $" WHERE {whereClause}";
                }
                
                AddParametersToCommand(command, parameters);
            }
            else
            {
                _logger.LogWarning("Empty SQL predicate generated for filter: {Filter}", filter);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to process SCIM filter: {Filter}. Continuing without filter.", filter);
        }
        
        return baseQuery;
    }
    
    private static Task<List<Pad>> ExecuteQueryAndMapResults(IDbCommand command, CancellationToken cancellationToken)
    {
        var pads = new List<Pad>();
        using var reader = command.ExecuteReader();
        
        while (reader.Read())
        {
            var pad = MapReaderToPad(reader);
            pads.Add(pad);
        }
        
        return Task.FromResult(pads);
    }
    
    /// <summary>
    /// SCIM attribute mapping to database columns
    /// </summary>
    private Dictionary<string, string> GetSchemaMapping()
    {
        return PadConfiguration.ScimMappings.AttributeToColumn;
    }
    
    /// <summary>
    /// Converts SCIM filter to SQL using SQL Server dialect
    /// </summary>
    private (string Sql, Dictionary<string, object> Parameters) ConvertToSqlWithSqlServerDialect(string filter, Dictionary<string, string> schemaMapping)
    {
        var options = new Looplex.Foundation.SearchContent.SqlGenerator.SqlGenerationOptions
        {
            FieldMapping = schemaMapping,
            Dialect = Looplex.Foundation.SearchContent.SqlGenerator.SqlDialect.SqlServer
        };
        
        var service = new Looplex.Foundation.SearchContent.SearchContentService();
        var result = service.ConvertToSql(filter, options);
        
        var parameters = new Dictionary<string, object>();
        if (result.Parameters != null)
        {
            foreach (var kvp in result.Parameters)
            {
                parameters[kvp.Key] = kvp.Value ?? string.Empty;
            }
        }
        return (result.Sql, parameters);
    }
}

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
/// Repository implementation for Pad entities using stored procedures.
/// 
/// This repository implements the Case-Management pattern with stored procedures:
/// - USP_pads_cquery - Collection query (paginação)
/// - USP_pads_pquery - Paginated query (com contagem)
/// - USP_pads_retrieve - Retrieve single pad
/// - USP_pads_create - Create new pad
/// - USP_pads_update - Update existing pad
/// 
/// Maintains full compatibility with IResourceRepository<Pad> interface from Looplex.Foundation.
/// </summary>
public class PadRepositoryStoredProcedure : IPadRepository, IResourceRepository<Pad>
{
    private readonly IDbConnections _connections;
    private readonly ILogger<PadRepositoryStoredProcedure> _logger;

    public PadRepositoryStoredProcedure(IDbConnections connections, ILogger<PadRepositoryStoredProcedure> logger)
    {
        _connections = connections;
        _logger = logger;
    }

    /// <summary>
    /// Query pads with SCIM v2.0 filtering and pagination using stored procedures.
    /// Converts SCIM parameters to Case-Management stored procedure parameters.
    /// </summary>
    public async Task<(IList<Pad> Resources, int TotalCount)> QueryAsync(
        int startIndex, int count, string? filter, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting pads with SCIM parameters: startIndex={StartIndex}, count={Count}, filter={Filter}", 
                startIndex, count, filter);

            // Convert SCIM to Case-Management parameters
            var (page, pageSize) = ConvertScimToPageParameters(startIndex, count);
            var filterParams = ParseFilterToStoredProcedureParams(filter);

            await using var dbCommand = await _connections.CommandConnection();
            await using var command = dbCommand.CreateCommand();

            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "USP_pads_pquery";

            // Add stored procedure parameters
            command.Parameters.Add(Dbs.CreateParameter(command, "@page", page, DbType.Int32));
            command.Parameters.Add(Dbs.CreateParameter(command, "@page_size", pageSize, DbType.Int32));
            command.Parameters.Add(Dbs.CreateParameter(command, "@do_count", true, DbType.Boolean));
            command.Parameters.Add(Dbs.CreateParameter(command, "@order_by", "updated_at DESC", DbType.String));
            
            // Add filter parameters
            AddFilterParameters(command, filterParams);

            var (pads, totalCount) = await ExecuteStoredProcedureWithCount((SqlCommand)command, cancellationToken);

            _logger.LogInformation("Retrieved {Count} pads, total: {TotalCount}", pads.Count, totalCount);
            return (pads, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pads with stored procedure");
            throw new InvalidOperationException($"Failed to get pads: {ex.Message}", ex);
        }
    }

    public async Task<List<Pad>> GetPadsAsync(CancellationToken cancellationToken = default)
    {
        var result = await QueryAsync(1, 100, null, cancellationToken);
        return result.Resources.ToList();
    }

    public async Task<List<Pad>> GetPadsAsync(string? filter, CancellationToken cancellationToken = default)
    {
        var result = await QueryAsync(1, 100, filter, cancellationToken);
        return result.Resources.ToList();
    }

    public async Task<List<Pad>> GetPadsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var result = await QueryAsync((page - 1) * pageSize + 1, pageSize, null, cancellationToken);
        return result.Resources.ToList();
    }

    public async Task<List<Pad>> GetPadsAsync(string? filter, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var result = await QueryAsync((page - 1) * pageSize + 1, pageSize, filter, cancellationToken);
        return result.Resources.ToList();
    }

    public async Task<Pad?> GetPadByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting pad by ID: {PadId}", id);

            await using var dbCommand = await _connections.CommandConnection();
            await using var command = dbCommand.CreateCommand();

            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "USP_pads_retrieve";
            command.Parameters.Add(Dbs.CreateParameter(command, "@filter_uuid", id, DbType.Guid));

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
        return await GetPadByIdAsync(id, cancellationToken);
    }

    public async Task<Guid> CreatePadAsync(Pad pad, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating new pad with Name: {PadName}", pad.Name);

            await using var dbCommand = await _connections.CommandConnection();
            await using var command = dbCommand.CreateCommand();

            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "USP_pads_create";

            command.Parameters.Add(Dbs.CreateParameter(command, "@name", pad.Name, DbType.String));
            command.Parameters.Add(Dbs.CreateParameter(command, "@active", pad.Active, DbType.Boolean));
            command.Parameters.Add(Dbs.CreateParameter(command, "@status", pad.Status, DbType.Byte));
            command.Parameters.Add(Dbs.CreateParameter(command, "@created_by", "admin", DbType.String));
            command.Parameters.Add(Dbs.CreateParameter(command, "@custom_fields", pad.CustomFields, DbType.String));

            var result = await command.ExecuteScalarAsync(cancellationToken);
            var uuid = result != null ? (Guid)result : Guid.Empty;

            _logger.LogInformation("Pad created successfully with UUID: {PadUuid}", uuid);
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

            await using var dbCommand = await _connections.CommandConnection();
            await using var command = dbCommand.CreateCommand();

            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "USP_pads_update";

            command.Parameters.Add(Dbs.CreateParameter(command, "@pad_guid", id, DbType.Guid));
            command.Parameters.Add(Dbs.CreateParameter(command, "@name", pad.Name, DbType.String));
            command.Parameters.Add(Dbs.CreateParameter(command, "@active", pad.Active, DbType.Boolean));
            command.Parameters.Add(Dbs.CreateParameter(command, "@status", (int)pad.Status, DbType.Int32));
            command.Parameters.Add(Dbs.CreateParameter(command, "@custom_fields", pad.CustomFields ?? "{}", DbType.String));

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

            await using var dbCommand = await _connections.CommandConnection();
            await using var command = dbCommand.CreateCommand();

            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "USP_pads_delete";

            command.Parameters.Add(Dbs.CreateParameter(command, "@pad_guid", id, DbType.Guid));

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

    /// <summary>
    /// Converts SCIM startIndex/count to page/pageSize parameters
    /// </summary>
    private (int page, int pageSize) ConvertScimToPageParameters(int startIndex, int count)
    {
        // SCIM startIndex is 1-based, convert to page-based
        var page = (startIndex - 1) / count + 1;
        return (page, count);
    }

    // IResourceRepository<Pad> implementation
    public async Task<Pad?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await GetPadByIdAsync(Guid.Parse(id), cancellationToken);
    }

    public async Task<Pad> CreateAsync(Pad resource, CancellationToken cancellationToken = default)
    {
        var id = await CreatePadAsync(resource, cancellationToken);
        
        // Set the ID on the resource before returning
        resource.Id = id.ToString();
        _logger.LogInformation("🔧 Set resource.Id to: {ResourceId}", resource.Id);
        
        return resource;
    }

    public async Task<Pad> UpdateAsync(string id, Pad resource, CancellationToken cancellationToken = default)
    {
        await UpdatePadAsync(Guid.Parse(id), resource, cancellationToken);
        
        // Set the ID on the resource before returning
        resource.Id = id;
        _logger.LogInformation("🔧 Set resource.Id to: {ResourceId}", resource.Id);
        
        return resource;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var result = await DeletePadAsync(Guid.Parse(id), cancellationToken);
        return result > 0;
    }


    /// <summary>
    /// Parse SCIM filter using Foundation and convert to stored procedure parameters
    /// </summary>
    private static PadFilterParameters ParseFilterToStoredProcedureParams(string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return new PadFilterParameters();

        try
        {
            // Use Foundation's SCIM parser
            var scimService = new Looplex.Foundation.SCIMv2.SCIMv2();
            var (sqlWhere, parameters) = scimService.ConvertToSqlFilter(filter);

            // Convert to stored procedure parameters
            var filterParams = new PadFilterParameters();
            
            // Simple mapping for now - can be enhanced
            if (parameters.ContainsKey("@param_0"))
            {
                var value = parameters["@param_0"]?.ToString();
                if (value != null)
                {
                    if (sqlWhere.Contains("name"))
                        filterParams.Name = value;
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
            return new PadFilterParameters();
        }
    }

    /// <summary>
    /// Adds filter parameters to the command
    /// </summary>
    private void AddFilterParameters(IDbCommand command, PadFilterParameters filterParams)
    {
        command.Parameters.Add(Dbs.CreateParameter(command, "@filter_ids", (object?)filterParams.Ids ?? DBNull.Value, DbType.String));
        command.Parameters.Add(Dbs.CreateParameter(command, "@filter_uuids", (object?)filterParams.Uuids ?? DBNull.Value, DbType.String));
        command.Parameters.Add(Dbs.CreateParameter(command, "@filter_name", (object?)filterParams.Name ?? DBNull.Value, DbType.String));
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
    private async Task<(List<Pad> Pads, int TotalCount)> ExecuteStoredProcedureWithCount(
        SqlCommand command, CancellationToken cancellationToken)
    {
        var pads = new List<Pad>();
        int totalCount = 0;

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        // First result set: paginated data
        while (await reader.ReadAsync(cancellationToken))
        {
            var pad = MapReaderToPad(reader);
            pads.Add(pad);
        }

        // Second result set: total count (if @do_count = 1)
        if (await reader.NextResultAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                totalCount = reader.GetInt32("total");
            }
        }

        return (pads, totalCount);
    }

    /// <summary>
    /// Maps database reader data to Pad domain entity
    /// </summary>
    private static Pad MapReaderToPad(IDataReader reader)
    {
        // Handle GUID conversion properly
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

        return new Pad
        {
            Id = idString,
            Name = reader["name"].ToString() ?? PadConfiguration.Defaults.DefaultName,
            Active = Convert.ToBoolean(reader["active"]),
            Status = Convert.ToInt32(reader["status"]),
            CustomFields = reader["custom_fields"].ToString() ?? "{}",
            Schemas = new[] { "urn:looplex:params:scim:schemas:notejam:2.0:Pad" },
            Meta = CreateResourceMeta(reader, idString)
        };
    }
    
    private static Looplex.Foundation.SCIMv2.Entities.ResourceMeta CreateResourceMeta(IDataReader reader, string resourceId)
    {
        return new Looplex.Foundation.SCIMv2.Entities.ResourceMeta
        {
            ResourceType = "Pad",
            Location = $"https://localhost:7065/scim/v2/pads/{resourceId}", // Full URL for SCIM compliance
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


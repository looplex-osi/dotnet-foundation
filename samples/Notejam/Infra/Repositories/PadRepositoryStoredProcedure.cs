using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Looplex.Foundation.Ports;
using Looplex.Foundation.Helpers;
using Looplex.SCIMv2.Queries;
using Looplex.SCIMv2.Entities;
using Looplex.SCIMv2.Modules;
using Looplex.SCIMv2.Helpers;
using Looplex.Samples.Domain.Entities;
using Looplex.Samples.Application;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace Looplex.Samples.Infra.Repositories;

/// <summary>
/// Filter parameters for Pad stored procedures
/// </summary>
public class PadFilterParameters
{
    public string? Ids { get; set; }
    public string? Uuids { get; set; }
    public string? Name { get; set; }
    public bool? Active { get; set; }
    public int? Status { get; set; }
    public DateTime? CreatedBegin { get; set; }
    public DateTime? CreatedEnd { get; set; }
    public DateTime? UpdatedBegin { get; set; }
    public DateTime? UpdatedEnd { get; set; }
}

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
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PadRepositoryStoredProcedure(
        IDbConnections connections,
        ILogger<PadRepositoryStoredProcedure> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _connections = connections;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Query pads with SCIM v2.0 filtering using Foundation approach
    /// </summary>
    public async Task<(IList<Pad> Resources, int TotalCount)> QueryAsync(
        int startIndex,
        int count,
        string? filter = null,
        CancellationToken cancellationToken = default)
    {
        return await QueryAsync(startIndex, count, filter, null, null, cancellationToken);
    }

    /// <summary>
    /// Query pads with SCIM v2.0 filtering and sorting using Foundation approach
    /// </summary>
    public async Task<(IList<Pad> Resources, int TotalCount)> QueryAsync(
        int startIndex,
        int count,
        string? filter = null,
        string? sortBy = null,
        string? sortOrder = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("🔍 Getting pads with Foundation approach: startIndex={StartIndex}, count={Count}, filter={Filter}, sortBy={SortBy}, sortOrder={SortOrder}",
                startIndex, count, filter, sortBy, sortOrder);

            var page = CalculatePage(startIndex, count);
            var pageSize = count;
            // Use Foundation's approach like Case Management
            // Pass allowed attributes to enable filtering
            var allowedAttributes = new HashSet<string> {
                "id", "externalId", "name", "active", "status",
                "meta.created", "meta.lastModified"
            };

            // Pass attribute mapping for meta.created -> p.created_at (with table alias for stored procedure)
            var attributeMapper = new Dictionary<string, string> {
                { "meta.created", "p.created_at" },
                { "meta.lastModified", "p.updated_at" },
                { "active", "p.active" },
                { "name", "p.name" },
                { "status", "p.status" },
                { "id", "p.uuid" },
                { "externalId", "p.external_id" }
            };
            
            string? filters = filter?.ToSqlPredicate(attributeMapper, allowedAttributes);

            await using var dbCommand = await _connections.CommandConnection();
            await using var command = dbCommand.CreateCommand();

            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "USP_pads_pquery";

            // Build ORDER BY clause based on sorting parameters
            var orderByClause = BuildOrderByClause(sortBy, sortOrder);
            
            command.Parameters.Add(Dbs.CreateParameter(command, "@page", page, DbType.Int32));
            command.Parameters.Add(Dbs.CreateParameter(command, "@page_size", pageSize, DbType.Int32));
            command.Parameters.Add(Dbs.CreateParameter(command, "@do_count", true, DbType.Boolean));
            command.Parameters.Add(Dbs.CreateParameter(command, "@order_by", orderByClause, DbType.String));

            // Add filter using Foundation's approach (like Case Management)
            if (filters != null)
                command.Parameters.Add(Dbs.CreateParameter(command, "@__dangerouslySetPredicate", filters, DbType.String));

            var (pads, totalCount) = await ExecuteStoredProcedureWithCount((SqlCommand)command, cancellationToken);

            _logger.LogInformation("✅ Retrieved {Count} pads, total: {TotalCount}", pads.Count, totalCount);
            return (pads, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pads with Foundation approach");
            throw new InvalidOperationException($"Failed to get pads: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Builds ORDER BY clause based on sorting parameters
    /// </summary>
    private static string BuildOrderByClause(string? sortBy, string? sortOrder)
    {
        // Default sorting if no sortBy specified
        if (string.IsNullOrWhiteSpace(sortBy))
            return "updated_at DESC";

        // Map SCIM field names to database column names (without table alias for ORDER BY)
        var fieldMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "id", "uuid" },
            { "externalId", "external_id" },
            { "name", "name" },
            { "active", "active" },
            { "status", "status" },
            { "meta.created", "created_at" },
            { "meta.lastModified", "updated_at" },
            { "created", "created_at" },
            { "updated", "updated_at" }
        };

        // Get database column name, fallback to sortBy if not found
        var dbColumn = fieldMapping.TryGetValue(sortBy, out var mappedColumn) ? mappedColumn : $"p.{sortBy}";

        // Determine sort direction
        var isDescending = string.Equals(sortOrder, "descending", StringComparison.OrdinalIgnoreCase);
        var direction = isDescending ? "DESC" : "ASC";

        return $"{dbColumn} {direction}";
    }

    /// <summary>
    /// Execute stored procedure and return results with count
    /// </summary>
    private async Task<(List<Pad> Pads, int TotalCount)> ExecuteStoredProcedureWithCount(SqlCommand command, CancellationToken cancellationToken)
    {
        var pads = new List<Pad>();
        int totalCount = 0;

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        while (await reader.ReadAsync(cancellationToken))
        {
            var pad = MapReaderToPad(reader);
            pads.Add(pad);
        }

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
    /// Calculate page number from SCIM startIndex and count parameters
    /// </summary>
    private static int CalculatePage(int startIndex, int count)
    {
        return (int)Math.Ceiling((double)startIndex / count);
    }

    /// <summary>
    /// Map database reader to Pad entity
    /// </summary>
    private static Pad MapReaderToPad(IDataReader reader)
    {
        var pad = new Pad
        {
            Id = reader.GetGuid(reader.GetOrdinal("uuid")).ToString(),
            ExternalId = reader.IsDBNull(reader.GetOrdinal("external_id")) ? null : reader.GetInt32(reader.GetOrdinal("external_id")).ToString(),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Active = reader.GetBoolean(reader.GetOrdinal("active")),
            Status = reader.GetByte(reader.GetOrdinal("status")),
            CustomFields = reader.IsDBNull(reader.GetOrdinal("custom_fields")) ? null : reader.GetString(reader.GetOrdinal("custom_fields")),
            Meta = new Looplex.SCIMv2.Entities.ResourceMeta
            {
                ResourceType = "Pad",
                Location = $"/Pads/{reader.GetGuid(reader.GetOrdinal("uuid"))}",
                Created = reader.GetDateTime(reader.GetOrdinal("created_at")),
                LastModified = reader.GetDateTime(reader.GetOrdinal("updated_at")),
                Version = "W/\"" + reader.GetDateTime(reader.GetOrdinal("updated_at")).ToString("yyyy-MM-ddTHH:mm:ss.fffZ") + "\""
            },
            Schemas = new[] { "urn:looplex:params:scim:schemas:notejam:2.0:Pad" }
        };

        return pad;
    }

    #region IResourceRepository<Pad> Implementation

    public async Task<Pad?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var dbCommand = await _connections.CommandConnection();
            await using var command = dbCommand.CreateCommand();

            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "USP_pads_retrieve";
            command.Parameters.Add(Dbs.CreateParameter(command, "@filter_uuid", id, DbType.String));

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return MapReaderToPad(reader);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pad by id: {Id}", id);
            throw new InvalidOperationException($"Failed to get pad: {ex.Message}", ex);
        }
    }

    public async Task<Pad> CreateAsync(Pad pad, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var dbCommand = await _connections.CommandConnection();
            await using var command = dbCommand.CreateCommand();

            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "USP_pads_create";
            
            command.Parameters.Add(Dbs.CreateParameter(command, "@name", pad.Name, DbType.String));
            command.Parameters.Add(Dbs.CreateParameter(command, "@active", pad.Active, DbType.Boolean));
            command.Parameters.Add(Dbs.CreateParameter(command, "@status", pad.Status, DbType.Int32));
            command.Parameters.Add(Dbs.CreateParameter(command, "@created_by", "admin", DbType.String));
            command.Parameters.Add(Dbs.CreateParameter(command, "@custom_fields", pad.CustomFields ?? (object)DBNull.Value, DbType.String));

            // Execute stored procedure and get the returned UUID
            var result = await command.ExecuteScalarAsync(cancellationToken);
            var uuid = result != null ? (Guid)result : Guid.Empty;
            
            // Set the ID from the stored procedure result
            pad.Id = uuid.ToString();
            
            return pad;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating pad: {Id}", pad.Id);
            throw new InvalidOperationException($"Failed to create pad: {ex.Message}", ex);
        }
    }

    public async Task<Pad> UpdateAsync(Pad pad, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var dbCommand = await _connections.CommandConnection();
            await using var command = dbCommand.CreateCommand();

            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "USP_pads_update";
            
            // Convert string ID to GUID for the stored procedure
            var padGuid = Guid.Parse(pad.Id);
            command.Parameters.Add(Dbs.CreateParameter(command, "@pad_guid", padGuid, DbType.Guid));
            command.Parameters.Add(Dbs.CreateParameter(command, "@name", pad.Name, DbType.String));
            command.Parameters.Add(Dbs.CreateParameter(command, "@active", pad.Active, DbType.Boolean));
            command.Parameters.Add(Dbs.CreateParameter(command, "@status", pad.Status, DbType.Int32));
            command.Parameters.Add(Dbs.CreateParameter(command, "@custom_fields", pad.CustomFields ?? (object)DBNull.Value, DbType.String));

            await command.ExecuteNonQueryAsync(cancellationToken);
            return pad;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating pad: {Id}", pad.Id);
            throw new InvalidOperationException($"Failed to update pad: {ex.Message}", ex);
        }
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var dbCommand = await _connections.CommandConnection();
            await using var command = dbCommand.CreateCommand();

            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "USP_pads_delete";
            
            // Convert string ID to GUID for the stored procedure
            var padGuid = Guid.Parse(id);
            command.Parameters.Add(Dbs.CreateParameter(command, "@pad_guid", padGuid, DbType.Guid));

            var result = await command.ExecuteNonQueryAsync(cancellationToken);
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting pad: {Id}", id);
            throw new InvalidOperationException($"Failed to delete pad: {ex.Message}", ex);
        }
    }

    #endregion

    #region IPadRepository Implementation

    public async Task<List<Pad>> GetPadsAsync(CancellationToken cancellationToken = default)
    {
        var (pads, _) = await QueryAsync(1, int.MaxValue, null, cancellationToken);
        return pads.ToList();
    }

    public async Task<List<Pad>> GetPadsAsync(string? filter, CancellationToken cancellationToken = default)
    {
        var (pads, _) = await QueryAsync(1, int.MaxValue, filter, cancellationToken);
        return pads.ToList();
    }

    public async Task<List<Pad>> GetPadsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var startIndex = (page - 1) * pageSize + 1;
        var (pads, _) = await QueryAsync(startIndex, pageSize, null, cancellationToken);
        return pads.ToList();
    }

    public async Task<List<Pad>> GetPadsAsync(string? filter, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var startIndex = (page - 1) * pageSize + 1;
        var (pads, _) = await QueryAsync(startIndex, pageSize, filter, cancellationToken);
        return pads.ToList();
    }


    public async Task<Guid> CreatePadAsync(Pad pad, CancellationToken cancellationToken = default)
    {
        var result = await CreateAsync(pad, cancellationToken);
        return Guid.Parse(result.Id);
    }

    public async Task<int> UpdatePadAsync(Guid id, Pad pad, CancellationToken cancellationToken = default)
    {
        await UpdateAsync(pad, cancellationToken);
        return 1; // Return 1 to indicate success
    }

    public async Task<int> DeletePadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await DeleteAsync(id.ToString(), cancellationToken);
        return result ? 1 : 0; // Return 1 for success, 0 for failure
    }

    #endregion

    #region IResourceRepository<Pad> Additional Methods

    public async Task<Pad> UpdateAsync(string id, Pad resource, CancellationToken cancellationToken = default)
    {
        // Set the ID from the parameter
        resource.Id = id;
        return await UpdateAsync(resource, cancellationToken);
    }


    #endregion
}

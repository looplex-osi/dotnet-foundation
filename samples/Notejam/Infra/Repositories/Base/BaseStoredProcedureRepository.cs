using Looplex.Samples.Domain.Entities;
using Looplex.Samples.Application.Abstraction;
using Looplex.SCIMv2;
using Microsoft.Extensions.Logging;

namespace Looplex.Samples.Infra.Repositories.Base;

/// <summary>
/// Base repository implementation for stored procedure-based repositories.
/// Provides common CRUD operations and query functionality.
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public abstract class BaseStoredProcedureRepository<T> : IResourceRepository<T> 
    where T : class
{
    protected readonly IDbConnections _connections;
    protected readonly ILogger _logger;
    protected readonly IStoredProcedureExecutor _executor;
    protected readonly IEntityMapping<T> _mapping;
    protected readonly IDataMapper<T> _dataMapper;

    protected BaseStoredProcedureRepository(
        IDbConnections connections,
        ILogger logger,
        IStoredProcedureExecutor executor,
        IEntityMapping<T> mapping,
        IDataMapper<T> dataMapper)
    {
        _connections = connections;
        _logger = logger;
        _executor = executor;
        _mapping = mapping;
        _dataMapper = dataMapper;
    }

    public virtual async Task<(IList<T> Resources, int TotalCount)> QueryAsync(
        int startIndex, int count, string? filter, CancellationToken cancellationToken = default)
    {
        return await QueryAsync(startIndex, count, filter, null, null, null, null, cancellationToken);
    }

    public virtual async Task<(IList<T> Resources, int TotalCount)> QueryAsync(
        int startIndex, int count, string? filter, string? sortBy, string? sortOrder, 
        string? attributes, string? excludedAttributes, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("🔍 Getting {EntityName} with Foundation approach: startIndex={StartIndex}, count={Count}, filter={Filter}, sortBy={SortBy}, sortOrder={SortOrder}, attributes={Attributes}, excludedAttributes={ExcludedAttributes}", 
                _mapping.EntityName, startIndex, count, filter, sortBy, sortOrder, attributes, excludedAttributes);

            var queryParams = new StoredProcedureQueryParams
            {
                StartIndex = startIndex,
                Count = count,
                Filter = filter,
                SortBy = sortBy,
                SortOrder = sortOrder,
                Mapping = _mapping
            };

            var result = await _executor.ExecuteQueryAsync<T>(
                _mapping.GetQueryProcedureName(), 
                queryParams, 
                _dataMapper, 
                cancellationToken);

            _logger.LogInformation("✅ Retrieved {Count} {EntityName}, total: {TotalCount}", 
                result.Resources.Count, _mapping.EntityName, result.TotalCount);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting {EntityName} with Foundation approach", _mapping.EntityName);
            throw new InvalidOperationException($"Failed to get {_mapping.EntityName.ToLower()}: {ex.Message}", ex);
        }
    }

    public virtual async Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting {EntityName} by ID: {Id}", _mapping.EntityName, id);

            var parameters = _dataMapper.MapToRetrieveParameters(id);
            var result = await _executor.ExecuteReaderAsync<T>(
                _mapping.GetRetrieveProcedureName(), 
                parameters, 
                _dataMapper, 
                cancellationToken);

            if (result != null)
            {
                _logger.LogInformation("{EntityName} retrieved successfully: {Id}", _mapping.EntityName, id);
            }
            else
            {
                _logger.LogWarning("{EntityName} not found with ID: {Id}", _mapping.EntityName, id);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting {EntityName} by ID: {Id}", _mapping.EntityName, id);
            throw new InvalidOperationException($"Failed to get {_mapping.EntityName.ToLower()}: {ex.Message}", ex);
        }
    }

    public virtual async Task<T> CreateAsync(T resource, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("🔍 CreateAsync called with {EntityName}: {ResourceName}", 
                _mapping.EntityName, GetResourceName(resource));
            
            var parameters = _dataMapper.MapToCreateParameters(resource);
            var result = await _executor.ExecuteScalarAsync(
                _mapping.GetCreateProcedureName(), 
                parameters, 
                cancellationToken);
            
            var id = result?.ToString() ?? Guid.Empty.ToString();
            SetResourceId(resource, id);
            
            _logger.LogInformation("✅ CreateAsync completed successfully with ID: {Id}", id);
            return resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ CreateAsync failed for {EntityName}: {ResourceName}", 
                _mapping.EntityName, GetResourceName(resource));
            throw;
        }
    }

    public virtual async Task<T> UpdateAsync(string id, T resource, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("🔍 UpdateAsync called with ID: {Id}, {EntityName}: {ResourceName}", 
                id, _mapping.EntityName, GetResourceName(resource));
            
            var parameters = _dataMapper.MapToUpdateParameters(id, resource);
            await _executor.ExecuteNonQueryAsync(
                _mapping.GetUpdateProcedureName(), 
                parameters, 
                cancellationToken);
            
            SetResourceId(resource, id);
            _logger.LogInformation("✅ UpdateAsync completed successfully");
            return resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ UpdateAsync failed for {EntityName}: {Id}", _mapping.EntityName, id);
            throw;
        }
    }

    public virtual async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting {EntityName} with ID: {Id}", _mapping.EntityName, id);
            
            var parameters = _dataMapper.MapToDeleteParameters(id);
            var result = await _executor.ExecuteNonQueryAsync(
                _mapping.GetDeleteProcedureName(), 
                parameters, 
                cancellationToken);
            
            var success = result > 0;
            if (success)
            {
                _logger.LogInformation("{EntityName} deleted successfully. Rows affected: {RowsAffected}", 
                    _mapping.EntityName, result);
            }
            else
            {
                _logger.LogWarning("No {EntityName} found to delete with ID: {Id}", _mapping.EntityName, id);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting {EntityName} with ID: {Id}", _mapping.EntityName, id);
            throw new InvalidOperationException($"Failed to delete {_mapping.EntityName.ToLower()}: {ex.Message}", ex);
        }
    }

    protected abstract string GetResourceName(T resource);
    protected abstract void SetResourceId(T resource, string id);
}

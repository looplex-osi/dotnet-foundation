using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Looplex.Foundation.Helpers;
using Looplex.SCIMv2.Helpers;
using Looplex.Samples.Application.Abstraction;
using Microsoft.Extensions.Logging;

namespace Looplex.Samples.Infra.Repositories.Base;

/// <summary>
/// Implementation of stored procedure executor using SQL Server.
/// Handles all database operations with proper error handling and logging.
/// </summary>
public class StoredProcedureExecutor : IStoredProcedureExecutor
{
    private readonly IDbConnections _connections;
    private readonly ILogger<StoredProcedureExecutor> _logger;

    public StoredProcedureExecutor(IDbConnections connections, ILogger<StoredProcedureExecutor> logger)
    {
        _connections = connections;
        _logger = logger;
    }

    public async Task<(IList<T> Resources, int TotalCount)> ExecuteQueryAsync<T>(
        string procedureName,
        StoredProcedureQueryParams queryParams,
        IDataMapper<T> dataMapper,
        CancellationToken cancellationToken = default) where T : class
    {
        await using var dbCommand = await _connections.CommandConnection();
        await using var command = dbCommand.CreateCommand();
        
        if (dbCommand.State != ConnectionState.Open)
        {
            await dbCommand.OpenAsync(cancellationToken);
        }

        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = procedureName;

        var page = CalculatePage(queryParams.StartIndex, queryParams.Count);
        var pageSize = queryParams.Count > 0 ? queryParams.Count : 10;
        var orderByClause = BuildOrderByClause(queryParams.SortBy, queryParams.SortOrder, queryParams.Mapping);
        var mapping = queryParams.Mapping;
        var attributeMapper = ((dynamic)mapping).AttributeMapper;
        var allowedAttributes = ((dynamic)mapping).AllowedAttributes;
        
        string? filters = null;
        if (!string.IsNullOrEmpty(queryParams.Filter))
        {
            // Call ToSqlPredicate extension method using reflection to avoid dynamic binding issues
            var filterString = queryParams.Filter;
            var toSqlPredicateMethod = typeof(Looplex.SCIMv2.Helpers.Strings).GetMethod("ToSqlPredicate", 
                new[] { typeof(string), typeof(Dictionary<string, string>), typeof(HashSet<string>) });
            
            if (toSqlPredicateMethod != null)
            {
                filters = (string?)toSqlPredicateMethod.Invoke(null, new object[] { filterString, attributeMapper, allowedAttributes });
            }
        }

        command.Parameters.Add(Dbs.CreateParameter(command, "@page", page, DbType.Int32));
        command.Parameters.Add(Dbs.CreateParameter(command, "@page_size", pageSize, DbType.Int32));
        command.Parameters.Add(Dbs.CreateParameter(command, "@do_count", true, DbType.Boolean));
        command.Parameters.Add(Dbs.CreateParameter(command, "@order_by", orderByClause, DbType.String));

        if (filters != null)
        {
            command.Parameters.Add(Dbs.CreateParameter(command, "@__dangerouslySetPredicate", filters, DbType.String));
        }

        return await ExecuteWithCountAsync((SqlCommand)command, dataMapper, cancellationToken);
    }

    public async Task<T?> ExecuteReaderAsync<T>(
        string procedureName,
        Dictionary<string, object> parameters,
        IDataMapper<T> dataMapper,
        CancellationToken cancellationToken = default) where T : class
    {
        await using var dbCommand = await _connections.CommandConnection();
        await using var command = dbCommand.CreateCommand();
        
        if (dbCommand.State != ConnectionState.Open)
        {
            await dbCommand.OpenAsync(cancellationToken);
        }

        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = procedureName;

        foreach (var param in parameters)
        {
            command.Parameters.Add(Dbs.CreateParameter(command, param.Key, param.Value, GetDbType(param.Value)));
        }

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return dataMapper.MapFromReader(reader);
        }

        return null;
    }

    public async Task<object?> ExecuteScalarAsync(
        string procedureName,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        await using var dbCommand = await _connections.CommandConnection();
        await using var command = dbCommand.CreateCommand();
        
        if (dbCommand.State != ConnectionState.Open)
        {
            await dbCommand.OpenAsync(cancellationToken);
        }

        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = procedureName;

        foreach (var param in parameters)
        {
            command.Parameters.Add(Dbs.CreateParameter(command, param.Key, param.Value, GetDbType(param.Value)));
        }

        return await command.ExecuteScalarAsync(cancellationToken);
    }

    public async Task<int> ExecuteNonQueryAsync(
        string procedureName,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        await using var dbCommand = await _connections.CommandConnection();
        await using var command = dbCommand.CreateCommand();
        
        if (dbCommand.State != ConnectionState.Open)
        {
            await dbCommand.OpenAsync(cancellationToken);
        }

        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = procedureName;

        foreach (var param in parameters)
        {
            command.Parameters.Add(Dbs.CreateParameter(command, param.Key, param.Value, GetDbType(param.Value)));
        }

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<(IList<T> Resources, int TotalCount)> ExecuteWithCountAsync<T>(
        SqlCommand command, IDataMapper<T> dataMapper, CancellationToken cancellationToken) where T : class
    {
        var resources = new List<T>();
        int totalCount = 0;

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        // First result set: paginated data
        while (await reader.ReadAsync(cancellationToken))
        {
            var resource = dataMapper.MapFromReader(reader);
            resources.Add(resource);
        }

        // Second result set: total count (if @do_count = 1)
        if (await reader.NextResultAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                totalCount = reader.GetInt32("total");
            }
        }

        return (resources, totalCount);
    }

    private static string BuildOrderByClause(string? sortBy, string? sortOrder, object mapping)
    {
        var dynamicMapping = (dynamic)mapping;
        
        if (string.IsNullOrWhiteSpace(sortBy))
            return dynamicMapping.DefaultSortField;

        if (!dynamicMapping.SortFieldMapping.TryGetValue(sortBy, out string mappedColumn))
            throw new ArgumentException($"Invalid sortBy field: {sortBy}", nameof(sortBy));
        var dbColumn = mappedColumn;

        var isDescending = string.Equals(sortOrder, "descending", StringComparison.OrdinalIgnoreCase);
        var direction = isDescending ? "DESC" : "ASC";

        var orderBy = $"{dbColumn} {direction}";
        
        // Para evitar duplicação, se não for updated_at, adicionamos updated_at DESC
        // Mas só se o DefaultSortField não já contém updated_at
        var defaultSortField = dynamicMapping.DefaultSortField;
        if (!dbColumn.Contains("updated_at") && 
            !orderBy.Contains("updated_at") && 
            !defaultSortField.Contains("updated_at"))
        {
            orderBy += ", updated_at DESC";  // Sem alias para que a stored procedure reconheça
        }

        return orderBy;
    }

    private static int CalculatePage(int startIndex, int count)
    {
        var safeCount = count > 0 ? count : 10;
        var safeStart = startIndex > 0 ? startIndex : 1;
        return ((safeStart - 1) / safeCount) + 1;
    }

    private static DbType GetDbType(object value)
    {
        return value switch
        {
            Guid => DbType.Guid,
            int => DbType.Int32,
            long => DbType.Int64,
            byte => DbType.Byte,
            short => DbType.Int16,
            bool => DbType.Boolean,
            DateTime => DbType.DateTime,
            string => DbType.String,
            _ => DbType.String
        };
    }
}

using System.Data;

namespace Looplex.Samples.Infra.Repositories.Base;

/// <summary>
/// Interface for executing stored procedures with different return types.
/// Provides abstraction for database operations.
/// </summary>
public interface IStoredProcedureExecutor
{
    /// <summary>
    /// Executes a stored procedure that returns a query result with count
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="procedureName">Stored procedure name</param>
    /// <param name="queryParams">Query parameters</param>
    /// <param name="dataMapper">Data mapper for the entity</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Query result with resources and total count</returns>
    Task<(IList<T> Resources, int TotalCount)> ExecuteQueryAsync<T>(
        string procedureName,
        StoredProcedureQueryParams queryParams,
        IDataMapper<T> dataMapper,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Executes a stored procedure that returns a single entity
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="procedureName">Stored procedure name</param>
    /// <param name="parameters">Procedure parameters</param>
    /// <param name="dataMapper">Data mapper for the entity</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Single entity or null</returns>
    Task<T?> ExecuteReaderAsync<T>(
        string procedureName,
        Dictionary<string, object> parameters,
        IDataMapper<T> dataMapper,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Executes a stored procedure that returns a scalar value
    /// </summary>
    /// <param name="procedureName">Stored procedure name</param>
    /// <param name="parameters">Procedure parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Scalar result</returns>
    Task<object?> ExecuteScalarAsync(
        string procedureName,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a stored procedure that returns number of affected rows
    /// </summary>
    /// <param name="procedureName">Stored procedure name</param>
    /// <param name="parameters">Procedure parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of affected rows</returns>
    Task<int> ExecuteNonQueryAsync(
        string procedureName,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Parameters for stored procedure queries
/// </summary>
public class StoredProcedureQueryParams
{
    public int StartIndex { get; set; }
    public int Count { get; set; }
    public string? Filter { get; set; }
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; }
    public object Mapping { get; set; } = null!;
}

using System.Data;

namespace Looplex.Samples.Infra.Repositories.Base;

/// <summary>
/// Interface for data mapping between database and domain entities.
/// Handles conversion between IDataReader and domain objects, and parameter mapping.
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public interface IDataMapper<T> where T : class
{
    /// <summary>
    /// Maps database reader data to domain entity
    /// </summary>
    /// <param name="reader">Database reader</param>
    /// <returns>Mapped domain entity</returns>
    T MapFromReader(IDataReader reader);
    
    /// <summary>
    /// Maps entity to stored procedure parameters for create operations
    /// </summary>
    /// <param name="entity">Domain entity</param>
    /// <returns>Parameter dictionary</returns>
    Dictionary<string, object> MapToCreateParameters(T entity);
    
    /// <summary>
    /// Maps entity to stored procedure parameters for update operations
    /// </summary>
    /// <param name="id">Entity ID</param>
    /// <param name="entity">Domain entity</param>
    /// <returns>Parameter dictionary</returns>
    Dictionary<string, object> MapToUpdateParameters(string id, T entity);
    
    /// <summary>
    /// Maps entity ID to stored procedure parameters for delete operations
    /// </summary>
    /// <param name="id">Entity ID</param>
    /// <returns>Parameter dictionary</returns>
    Dictionary<string, object> MapToDeleteParameters(string id);
    
    /// <summary>
    /// Maps entity ID to stored procedure parameters for retrieve operations
    /// </summary>
    /// <param name="id">Entity ID</param>
    /// <returns>Parameter dictionary</returns>
    Dictionary<string, object> MapToRetrieveParameters(string id);
}

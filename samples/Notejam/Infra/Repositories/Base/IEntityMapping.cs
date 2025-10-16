using Looplex.Samples.Domain.Entities;

namespace Looplex.Samples.Infra.Repositories.Base;

/// <summary>
/// Interface for entity mapping configuration in stored procedure repositories.
/// Provides mapping between SCIM attributes and database columns.
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public interface IEntityMapping<T> where T : class
{
    /// <summary>
    /// Entity name for logging and error messages
    /// </summary>
    string EntityName { get; }
    
    /// <summary>
    /// Database table alias (e.g., "n" for notes, "p" for pads)
    /// </summary>
    string TableAlias { get; }
    
    /// <summary>
    /// ID column name with table alias
    /// </summary>
    string IdColumn { get; }
    
    /// <summary>
    /// SCIM schema URI for the entity
    /// </summary>
    string SchemaUri { get; }
    
    /// <summary>
    /// Allowed attributes for SCIM filtering
    /// </summary>
    HashSet<string> AllowedAttributes { get; }
    
    /// <summary>
    /// Mapping from SCIM attributes to database columns
    /// </summary>
    Dictionary<string, string> AttributeMapper { get; }
    
    /// <summary>
    /// Mapping from SCIM sort fields to database columns
    /// </summary>
    Dictionary<string, string> SortFieldMapping { get; }
    
    /// <summary>
    /// Default sort field for queries
    /// </summary>
    string DefaultSortField { get; }
    
    /// <summary>
    /// Stored procedure names for CRUD operations
    /// </summary>
    string GetQueryProcedureName();
    string GetCreateProcedureName();
    string GetUpdateProcedureName();
    string GetDeleteProcedureName();
    string GetRetrieveProcedureName();
}

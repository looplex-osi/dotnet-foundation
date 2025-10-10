using Looplex.Samples.Domain.Entities;
using Looplex.Samples.Infra.Repositories.Base;

namespace Looplex.Samples.Infra.Repositories.Mappings;

/// <summary>
/// Entity mapping configuration for Note entities.
/// Defines mapping between SCIM attributes and database columns for Note stored procedures.
/// </summary>
public class NoteEntityMapping : IEntityMapping<Note>
{
    public string EntityName => "Note";
    public string TableAlias => "n";
    public string IdColumn => "n.id";
    public string SchemaUri => "urn:looplex:params:scim:schemas:notejam:2.0:Note";
    
    public HashSet<string> AllowedAttributes => new()
    {
        "id", "externalId", "name", "text", "active", "status", 
        "meta.created", "meta.lastModified"
    };

    public Dictionary<string, string> AttributeMapper => new()
    {
        { "meta.created", "created_at" },
        { "meta.lastModified", "updated_at" },
        { "active", "active" },
        { "name", "markdown" },  // Notes usam 'markdown' como conteúdo
        { "text", "markdown" },
        { "status", "status" },
        { "id", "uuid" },
        { "externalId", "external_id" }
    };

    public Dictionary<string, string> SortFieldMapping => new()
    {
        { "id", "uuid" },
        { "externalId", "external_id" },
        { "name", "markdown" },  // Notes usam 'markdown' como conteúdo
        { "text", "markdown" },
        { "active", "active" },
        { "status", "status" },
        { "meta.created", "created_at" },
        { "meta.lastModified", "updated_at" },
        { "created", "created_at" },
        { "updated", "updated_at" }
    };

    public string DefaultSortField => "updated_at DESC";
    
    public string GetQueryProcedureName() => "USP_notes_pquery";
    public string GetCreateProcedureName() => "USP_notes_create";
    public string GetUpdateProcedureName() => "USP_notes_update";
    public string GetDeleteProcedureName() => "USP_notes_update";
    public string GetRetrieveProcedureName() => "USP_notes_retrieve";
}

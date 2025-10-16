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
        { "meta.created", "n.created_at" },  // Qualificado com alias da tabela
        { "meta.lastModified", "n.updated_at" },  // Qualificado com alias da tabela
        { "active", "n.active" },  // Qualificado com alias da tabela
        { "name", "n.markdown" },  // Notes usam 'markdown' como conteúdo, qualificado
        { "text", "n.markdown" },  // Qualificado
        { "status", "n.status" },  // Qualificado
        { "id", "n.uuid" },  // Qualificado
        { "externalId", "n.external_id" }  // Qualificado
    };

    public Dictionary<string, string> SortFieldMapping => new()
    {
        { "id", "uuid" },  // Sem alias para stored procedure
        { "externalId", "external_id" },  // Sem alias
        { "name", "markdown" },  // Notes usam 'markdown' como conteúdo, sem alias
        { "text", "markdown" },  // Sem alias
        { "active", "active" },  // Sem alias
        { "status", "status" },  // Sem alias
        { "meta.created", "created_at" },  // Sem alias para stored procedure
        { "meta.lastModified", "updated_at" },  // Sem alias
        { "created", "created_at" },  // Sem alias
        { "updated", "updated_at" }  // Sem alias
    };

    public string DefaultSortField => "updated_at DESC";  // Sem alias para que a stored procedure reconheça
    
    public string GetQueryProcedureName() => "USP_notes_pquery";
    public string GetCreateProcedureName() => "USP_notes_create";
    public string GetUpdateProcedureName() => "USP_notes_update";
    public string GetDeleteProcedureName() => "USP_notes_update";
    public string GetRetrieveProcedureName() => "USP_notes_retrieve";
}

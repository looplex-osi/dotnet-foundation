using Looplex.Samples.Domain.Entities;
using Looplex.Samples.Infra.Repositories.Base;

namespace Looplex.Samples.Infra.Repositories.Mappings;

/// <summary>
/// Entity mapping configuration for Pad entities.
/// Defines mapping between SCIM attributes and database columns for Pad stored procedures.
/// </summary>
public class PadEntityMapping : IEntityMapping<Pad>
{
    public string EntityName => "Pad";
    public string TableAlias => "p";
    public string IdColumn => "p.uuid";
    public string SchemaUri => "urn:looplex:params:scim:schemas:notejam:2.0:Pad";
    
    public HashSet<string> AllowedAttributes => new()
    {
        "id", "externalId", "name", "active", "status",
        "meta.created", "meta.lastModified"
    };

    public Dictionary<string, string> AttributeMapper => new()
    {
        { "meta.created", "p.created_at" },
        { "meta.lastModified", "p.updated_at" },
        { "active", "p.active" },
        { "name", "p.name" },
        { "status", "p.status" },
        { "id", "p.uuid" },
        { "externalId", "p.external_id" }
    };

    public Dictionary<string, string> SortFieldMapping => new()
    {
        { "id", "p.uuid" },
        { "externalId", "p.external_id" },
        { "name", "p.name" },
        { "active", "p.active" },
        { "status", "p.status" },
        { "meta.created", "p.created_at" },
        { "meta.lastModified", "p.updated_at" },
        { "created", "p.created_at" },
        { "updated", "p.updated_at" }
    };

    public string DefaultSortField => "updated_at DESC";
    
    public string GetQueryProcedureName() => "USP_pads_pquery";
    public string GetCreateProcedureName() => "USP_pads_create";
    public string GetUpdateProcedureName() => "USP_pads_update";
    public string GetDeleteProcedureName() => "USP_pads_delete";
    public string GetRetrieveProcedureName() => "USP_pads_retrieve";
}

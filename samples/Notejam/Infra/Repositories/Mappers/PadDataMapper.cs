using System.Data;
using Looplex.Samples.Domain.Entities;
using Looplex.Samples.Infra.Repositories.Base;
using Looplex.SCIMv2.Entities;

namespace Looplex.Samples.Infra.Repositories.Mappers;

/// <summary>
/// Data mapper for Pad entities.
/// Handles conversion between database records and Pad domain objects.
/// </summary>
public class PadDataMapper : IDataMapper<Pad>
{
    private readonly IEntityMapping<Pad> _mapping;

    public PadDataMapper(IEntityMapping<Pad> mapping)
    {
        _mapping = mapping;
    }

    public Pad MapFromReader(IDataReader reader)
    {
        var pad = new Pad
        {
            Id = reader.GetGuid(reader.GetOrdinal("uuid")).ToString(),
            ExternalId = reader.IsDBNull(reader.GetOrdinal("external_id")) ? null : reader.GetInt32(reader.GetOrdinal("external_id")).ToString(),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Active = reader.GetBoolean(reader.GetOrdinal("active")),
            Status = reader.GetByte(reader.GetOrdinal("status")),
            CustomFields = reader.IsDBNull(reader.GetOrdinal("custom_fields")) ? null : reader.GetString(reader.GetOrdinal("custom_fields")),
            Meta = new ResourceMeta
            {
                ResourceType = _mapping.EntityName,
                Location = $"/Pads/{reader.GetGuid(reader.GetOrdinal("uuid"))}",
                Created = reader.GetDateTime(reader.GetOrdinal("created_at")),
                LastModified = reader.GetDateTime(reader.GetOrdinal("updated_at")),
                Version = "W/\"" + reader.GetDateTime(reader.GetOrdinal("updated_at")).ToString("yyyy-MM-ddTHH:mm:ss.fffZ") + "\""
            },
            Schemas = new[] { _mapping.SchemaUri }
        };

        return pad;
    }

    public Dictionary<string, object> MapToCreateParameters(Pad pad)
    {
        return new Dictionary<string, object>
        {
            { "@name", pad.Name },
            { "@active", pad.Active },
            { "@status", pad.Status },
            { "@created_by", "admin" },
            { "@custom_fields", pad.CustomFields ?? (object)DBNull.Value }
        };
    }

    public Dictionary<string, object> MapToUpdateParameters(string id, Pad pad)
    {
        return new Dictionary<string, object>
        {
            { "@pad_guid", Guid.Parse(id) },
            { "@name", pad.Name },
            { "@active", pad.Active },
            { "@status", pad.Status },
            { "@custom_fields", pad.CustomFields ?? (object)DBNull.Value }
        };
    }

    public Dictionary<string, object> MapToDeleteParameters(string id)
    {
        return new Dictionary<string, object>
        {
            { "@pad_guid", Guid.Parse(id) }
        };
    }

    public Dictionary<string, object> MapToRetrieveParameters(string id)
    {
        return new Dictionary<string, object>
        {
            { "@filter_uuid", id }
        };
    }
}

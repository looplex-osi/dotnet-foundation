using System.Data;
using Looplex.Samples.Domain.Entities;
using Looplex.Samples.Infra.Repositories.Base;
using Looplex.SCIMv2;
using Microsoft.AspNetCore.Http;

namespace Looplex.Samples.Infra.Repositories.Mappers;

/// <summary>
/// Data mapper for Note entities.
/// Handles conversion between database records and Note domain objects.
/// </summary>
public class NoteDataMapper : IDataMapper<Note>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IEntityMapping<Note> _mapping;

    public NoteDataMapper(IHttpContextAccessor httpContextAccessor, IEntityMapping<Note> mapping)
    {
        _httpContextAccessor = httpContextAccessor;
        _mapping = mapping;
    }

    public Note MapFromReader(IDataReader reader)
    {
        var note = new Note();
        
        // Map Id from uuid column (Notejam-specific)
        note.Id = reader["uuid"]?.ToString() ?? string.Empty;
        
        // Map externalId as optional field following RFC 7643 Section 2.1 - Core Schema
        if (reader["external_id"] != DBNull.Value)
            note.ExternalId = reader["external_id"].ToString();
        
        // Map Notejam-specific properties using SCIMv2 type converters
        note.Name = Looplex.SCIMv2.SCIMv2.ScimTypeConverter.ParseString(reader["markdown"]);
        note.Text = Looplex.SCIMv2.SCIMv2.ScimTypeConverter.ParseString(reader["markdown"]);
        note.Active = Looplex.SCIMv2.SCIMv2.ScimTypeConverter.ParseBoolean(reader["active"]);
        note.Status = Looplex.SCIMv2.SCIMv2.ScimTypeConverter.ParseInteger(reader["status"]);
        note.CustomFields = Looplex.SCIMv2.SCIMv2.ScimTypeConverter.ParseString(reader["custom_fields"]);
        note.Schemas = new[] { _mapping.SchemaUri };
        
        // Create Meta using Foundation method (agnostic)
        note.Meta = Looplex.SCIMv2.SCIMv2.CreateResourceMeta(reader, note.Id ?? string.Empty, _mapping.EntityName, _httpContextAccessor);
        
        return note;
    }

    public Dictionary<string, object> MapToCreateParameters(Note note)
    {
        return new Dictionary<string, object>
        {
            { "@text", note.Text },
            { "@active", note.Active },
            { "@status", note.Status },
            { "@custom_fields", note.CustomFields }
        };
    }

    public Dictionary<string, object> MapToUpdateParameters(string id, Note note)
    {
        return new Dictionary<string, object>
        {
            { "@note_guid", Guid.Parse(id) },
            { "@name", note.Name },
            { "@text", note.Text },
            { "@active", note.Active },
            { "@status", (int)note.Status }
        };
    }

    public Dictionary<string, object> MapToDeleteParameters(string id)
    {
        return new Dictionary<string, object>
        {
            { "@note_guid", Guid.Parse(id) },
            { "@active", false }
        };
    }

    public Dictionary<string, object> MapToRetrieveParameters(string id)
    {
        return new Dictionary<string, object>
        {
            { "@filter_uuid", Guid.Parse(id) }
        };
    }
}

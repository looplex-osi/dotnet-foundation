namespace Looplex.Samples.Application;

/// <summary>
/// Configuration constants for Note entity
/// </summary>
public static class NoteConfiguration
{
    /// <summary>
    /// Database table and column names
    /// </summary>
    public static class Database
    {
        public const string NotesTable = "notes";
        public const string PadsTable = "pads";
        
        public const string IdColumn = "uuid";
        public const string TextColumn = "markdown";
        public const string ActiveColumn = "active";
        public const string StatusColumn = "status";
        public const string CustomFieldsColumn = "custom_fields";
        public const string CreatedColumn = "created_at";
        public const string UpdatedColumn = "updated_at";
        public const string PadIdColumn = "pad_id";
        public const string PadNameColumn = "name";
        public const string PadIdReferenceColumn = "id"; // Coluna id da tabela pads (IDENTITY)
        public const string NotesIdReferenceColumn = "id"; // Coluna id da tabela notes (IDENTITY)
    }
    
    /// <summary>
    /// SQL parameter names
    /// </summary>
    public static class Parameters
    {
        public const string Id = "@uuid";
        public const string Text = "@text";
        public const string Active = "@active";
        public const string Status = "@status";
        public const string CustomFields = "@customFields";
        public const string Created = "@created";
        public const string Updated = "@updated";
        public const string PadId = "@padId";
    }
    
    /// <summary>
    /// Default values
    /// </summary>
    public static class Defaults
    {
        public const string DefaultPadName = "Sem Pad";
        public const string DefaultPadId = "11111111-1111-1111-1111-111111111111";
        public const bool DefaultActive = true;
        public const int DefaultStatus = 1;
        public const string DefaultCustomFields = "{}";
    }
    
    /// <summary>
    /// SCIM attribute mappings
    /// </summary>
    public static class ScimMappings
    {
        public static readonly Dictionary<string, string> AttributeToColumn = new()
        {
            // Atributos da entidade Note (usando aliases da query)
            { "id", $"n.{Database.IdColumn}" },
            { "text", $"n.{Database.TextColumn}" },
            { "name", $"p.{Database.PadNameColumn}" },
            { "active", $"n.{Database.ActiveColumn}" },
            { "status", $"n.{Database.StatusColumn}" },
            { "customFields", $"n.{Database.CustomFieldsColumn}" },
            { "custom_fields", $"n.{Database.CustomFieldsColumn}" },
            { "customFields.test", $"JSON_VALUE(n.{Database.CustomFieldsColumn}, '$.test')" },
            { "custom_fields.test", $"JSON_VALUE(n.{Database.CustomFieldsColumn}, '$.test')" },
            { "customFields.updated", $"JSON_VALUE(n.{Database.CustomFieldsColumn}, '$.updated')" },
            { "custom_fields.updated", $"JSON_VALUE(n.{Database.CustomFieldsColumn}, '$.updated')" },
            { "created", $"n.{Database.CreatedColumn}" },
            { "modified", $"n.{Database.UpdatedColumn}" },
            
            // Atributos do Pad (relacionamento)
            { "pad.name", $"p.{Database.PadNameColumn}" },
            { "pad.id", $"p.{Database.PadIdReferenceColumn}" },
            
            // Atributos adicionais para evitar conflitos
            { "uuid", $"n.{Database.IdColumn}" },
            { "markdown", $"n.{Database.TextColumn}" }
        };
    }
}

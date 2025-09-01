namespace Looplex.Samples.Application;

/// <summary>
/// Centralizes configuration constants for the Pad entity including database mappings, 
/// SCIM attribute mappings, and default values. Used throughout the application for consistency.
/// </summary>
public static class PadConfiguration
{
    /// <summary>
    /// Database table and column names
    /// </summary>
    public static class Database
    {
        public const string PadsTable = "pads";
        
        public const string IdColumn = "uuid";
        public const string NameColumn = "name";
        public const string ActiveColumn = "active";
        public const string StatusColumn = "status";
        public const string CustomFieldsColumn = "custom_fields";
        public const string CreatedColumn = "created_at";
        public const string UpdatedColumn = "updated_at";
        public const string IdReferenceColumn = "id"; // Column id from pads table (IDENTITY)
    }
    
    /// <summary>
    /// SQL parameter names
    /// </summary>
    public static class Parameters
    {
        public const string Id = "@uuid";
        public const string Name = "@name";
        public const string Active = "@active";
        public const string Status = "@status";
        public const string CustomFields = "@custom_fields";
        public const string Created = "@created";
        public const string Updated = "@updated";
    }
    
    /// <summary>
    /// Default values
    /// </summary>
    public static class Defaults
    {
        public const string DefaultName = "Unnamed";
        public const bool DefaultActive = true;
        public const byte DefaultStatus = 1;
        public const string DefaultCustomFields = "{}";
    }
    
    /// <summary>
    /// SCIM attribute mappings
    /// </summary>
    public static class ScimMappings
    {
        public static readonly Dictionary<string, string> AttributeToColumn = new()
        {
            // Pad entity attributes
            { "id", $"p.{Database.IdColumn}" },
            { "name", $"p.{Database.NameColumn}" },
            { "active", $"p.{Database.ActiveColumn}" },
            { "status", $"p.{Database.StatusColumn}" },
            { "custom_fields", $"p.{Database.CustomFieldsColumn}" },
            { "created", $"p.{Database.CreatedColumn}" },
            { "modified", $"p.{Database.UpdatedColumn}" },
            
            // Additional attributes to avoid conflicts
            { "uuid", $"p.{Database.IdColumn}" }
        };
    }
}

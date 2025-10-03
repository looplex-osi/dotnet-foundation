using Looplex.SCIMv2.Entities;

namespace Looplex.Samples.Domain.Entities;

/// <summary>
/// Domain entity representing a Pad (workspace/container for notes). 
/// Implements SCIM Resource interface and includes business validation rules.
/// </summary>
public class Pad : Resource
{
    private string _name = string.Empty;
    private bool _active = true;
    private int _status = 1;
    private string _customFields = "{}";
    private string _createdBy = string.Empty;
    private string _updatedBy = string.Empty;

    /// <summary>
    /// Name of the pad
    /// </summary>
    public string Name
    {
        get => _name;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Name cannot be null or empty", nameof(value));
            if (value.Length > 255)
                throw new ArgumentException("Name cannot exceed 255 characters", nameof(value));
            _name = value;
        }
    }

    /// <summary>
    /// Whether the pad is active
    /// </summary>
    public bool Active
    {
        get => _active;
        set => _active = value;
    }

    /// <summary>
    /// Status of the pad
    /// </summary>
    public int Status
    {
        get => _status;
        set => _status = value;
    }

    /// <summary>
    /// Custom fields in JSON format
    /// </summary>
    public string CustomFields
    {
        get => _customFields;
        set => _customFields = value ?? "{}";
    }

    /// <summary>
    /// User who created the pad
    /// </summary>
    public string CreatedBy
    {
        get => _createdBy;
        set => _createdBy = value ?? string.Empty;
    }

    /// <summary>
    /// User who last updated the pad
    /// </summary>
    public string UpdatedBy
    {
        get => _updatedBy;
        set => _updatedBy = value ?? string.Empty;
    }

    /// <summary>
    /// Validates the pad entity
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new InvalidOperationException("Pad name is required");
    }

    /// <summary>
    /// Creates a new pad with validation
    /// </summary>
    public static Pad Create(string name, bool active = true)
    {
        var pad = new Pad
        {
            Name = name,
            Active = active,
            Status = 1,
            CustomFields = "{}",
            CreatedBy = "admin",
            UpdatedBy = "admin"
        };

        pad.Validate();
        return pad;
    }
}

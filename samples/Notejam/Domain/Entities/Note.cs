using Looplex.Foundation.SCIMv2.Entities;

using PropertyChanged;

namespace Looplex.Samples.Domain.Entities
{
  [AddINotifyPropertyChangedInterface]
  public class Note : Resource
  {
    #region Reflectivity

    /// <summary>
    /// Default constructor required for:
    /// - Entity Framework Core for database operations
    /// - JSON serialization/deserialization
    /// - Reflection-based frameworks
    /// - Looplex.Foundation SCIM operations
    /// 
    /// This constructor initializes the entity with default values.
    /// </summary>
    public Note() { }

    #endregion

    private string _name = string.Empty;
    private string _text = string.Empty;
    private bool _active = true;
    private int _status = 1;
    private string _customFields = "{}";

    public string Name 
    { 
      get => _name;
      set
      {
        if (string.IsNullOrWhiteSpace(value))
          throw new ArgumentException("Name cannot be null or empty", nameof(value));
        
        if (value.Length > 255)
          throw new ArgumentException("Name cannot exceed 255 characters", nameof(value));
        
        _name = value.Trim();
      }
    }

    public string Text 
    { 
      get => _text;
      set
      {
        if (value == null)
          throw new ArgumentException("Text cannot be null", nameof(value));
        
        if (value.Length > 10000)
          throw new ArgumentException("Text cannot exceed 10,000 characters", nameof(value));
        
        _text = value;
      }
    }

    public bool Active 
    { 
      get => _active;
      set => _active = value;
    }

    public int Status 
    { 
      get => _status;
      set
      {
        if (value < 0 || value > 255)
          throw new ArgumentException("Status must be between 0 and 255", nameof(value));
        
        _status = value;
      }
    }

    public string CustomFields 
    { 
      get => _customFields;
      set
      {
        if (string.IsNullOrWhiteSpace(value))
          _customFields = "{}";
        else
          _customFields = value;
      }
    }

    /// <summary>
    /// Validates the note entity
    /// </summary>
    /// <returns>True if valid, throws exception if invalid</returns>
    public bool Validate()
    {
      if (string.IsNullOrWhiteSpace(Name))
        throw new InvalidOperationException("Note name is required");
      
      if (string.IsNullOrWhiteSpace(Text))
        throw new InvalidOperationException("Note text is required");
      
      return true;
    }

    /// <summary>
    /// Creates a new note with validation
    /// </summary>
    public static Note Create(string name, string text)
    {
      var note = new Note
      {
        Name = name,
        Text = text
      };
      
      note.Validate();
      return note;
    }
  }
}
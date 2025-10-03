using Looplex.SCIMv2.Entities;

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
        // Temporarily disable validation for debugging
        _name = value?.Trim() ?? string.Empty;
      }
    }

    public string Text 
    { 
      get => _text;
      set
      {
        // Temporarily disable validation for debugging
        _text = value ?? string.Empty;
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
        // Temporarily disable validation for debugging
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

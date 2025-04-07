using Looplex.Foundation.SCIMv2.Entities;

using PropertyChanged;

namespace Looplex.Samples.Domain.Entities
{
  [AddINotifyPropertyChangedInterface]
  public class Pad : Resource
  {
    #region Reflectivity
    public Pad() { }
    #endregion

    public string Name { get; set; }
    public List<Note> Notes { get; } = new List<Note>();

    public void AddNote(Note note)
    {
      // validate
      if (note == null)
      {
        throw new ArgumentNullException(nameof(note));
      }

      Notes.Add(note);
      FireEvent("NOTE_ADDED", note);
    }
  }
}
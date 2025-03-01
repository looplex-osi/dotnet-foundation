using Looplex.Foundation.Entities;
using PropertyChanged;

namespace Looplex.Samples.Entities;

[AddINotifyPropertyChangedInterface]
public class Pad : Actor
{
	#region Reflectivity
	public Pad() : base(){ }
	#endregion

	public string? Name { get; set; }
	public List<Note> Notes { get; } = new();

	public void AddNote(Note note)
	{
		// validate
		if (note == null) throw new ArgumentNullException(nameof(note));
		Notes.Add(note);
		FireEvent("NOTE_ADDED", note);
	}
}
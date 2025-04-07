using Looplex.Foundation.SCIMv2.Entities;

using PropertyChanged;

namespace Looplex.Samples.Domain.Entities
{
  [AddINotifyPropertyChangedInterface]
  public class Note : Resource
  {
    #region Reflectivity
    public Note() { }
    #endregion

    public string Name { get; set; }
    public string Text { get; set; }
  }
}
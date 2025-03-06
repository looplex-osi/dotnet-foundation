using Looplex.Foundation.Entities;

using PropertyChanged;

namespace Looplex.Samples.Entities;

[AddINotifyPropertyChangedInterface]
public class Note : Actor
{
  #region Reflectivity

  #endregion

  public string? Name { get; set; }
  public string? Text { get; set; }
}
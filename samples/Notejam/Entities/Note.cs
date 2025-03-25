using Looplex.Foundation.SCIMv2.Entities;

using PropertyChanged;

namespace Looplex.Samples.Entities;

[AddINotifyPropertyChangedInterface]
public class Note : Resource
{
  #region Reflectivity

  #endregion

  public string? Name { get; set; }
  public string? Text { get; set; }
}
using System;
using System.Collections.Generic;

using Looplex.OpenForExtension.Abstractions.Contexts;
using Looplex.OpenForExtension.Abstractions.Plugins;
using Looplex.OpenForExtension.Contexts;

namespace Looplex.Foundation.Entities {
  public abstract class Service
  {
    #region Reflectivity
    /* NOTE: Normally, since abstract classes can’t be instantiated directly,
    ReSharper might flag a public constructor as unnecessary or potentially
    indicative of a design flaw. However, there are cases—like using reflection
    or providing a clear initialization path for derived classes—where a public
    constructor is intentional. By adding this comment, you're telling ReSharper:
    "I know what I'm doing here; don't warn me about this one time."
    */
    // ReSharper disable once PublicConstructorInAbstractClass
    public Service() { }
    #endregion

    #region Micro-Kernel
    protected Service(IList<IPlugin> plugins)
    {
      Plugins = plugins;
    }
    protected IList<IPlugin> Plugins { get; set; } = new List<IPlugin>();

    public virtual IContext NewContext()
    {
      return DefaultContext.New(Plugins);
    }
    #endregion
  }
}

using System;
using System.Collections.Generic;

using Looplex.OpenForExtension.Abstractions.Contexts;
using Looplex.OpenForExtension.Abstractions.Plugins;
using Looplex.OpenForExtension.Contexts;

namespace Looplex.Foundation.Entities;

public abstract class Service : Actor
{
  #region Reflectivity

  protected Service() { }

  #endregion

  #region Micro-Kernel

  protected Service(IList<IPlugin> plugins)
  {
    Plugins = plugins;
  }

  protected IList<IPlugin> Plugins { get; set; }

  public virtual IContext NewContext()
  {
    return DefaultContext.New(Plugins);
  }

  #endregion
}
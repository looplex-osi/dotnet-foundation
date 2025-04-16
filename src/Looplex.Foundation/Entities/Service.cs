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

  #region Helpers

  protected static int Page(int startIndex, int count)
  {
    var page = (int)Math.Ceiling((double)startIndex / count);
    return page;
  }

  #endregion
}
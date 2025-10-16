using System;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;

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
  protected Service(IList<IPlugin> plugins, ILogger<Service> logger)
  {
    Plugins = plugins;
		Logger = logger;
	}

	protected ILogger<Service> Logger { get; set; }
	protected IList<IPlugin> Plugins { get; set; }

  public virtual IContext NewContext()
  {
    return DefaultContext.New(Plugins);
  }
  #endregion
}

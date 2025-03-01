using System.Collections.Generic;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Looplex.OpenForExtension.Abstractions.Plugins;
using Looplex.OpenForExtension.Contexts;

namespace Looplex.Foundation.Entities;

public abstract class Service
{
    #region Reflectivity
    // ReSharper disable once PublicConstructorInAbstractClass
    public Service() { }
    #endregion

    protected Service(IList<IPlugin> plugins)
    {
        Plugins = plugins;
    }
        
    #region Micro-Kernel
    protected IList<IPlugin> Plugins { get; set; } = new List<IPlugin>();
        
    public virtual IContext NewContext()
    {
        return DefaultContext.New(Plugins);
    }
    #endregion
}
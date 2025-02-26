using System.Collections.Generic;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Looplex.OpenForExtension.Contexts;
using Looplex.OpenForExtension.Abstractions.Plugins;

namespace Looplex.Foundation.Services
{
    public abstract class Service
    {
        #region Micro-Kernel
        protected IList<IPlugin> Plugins { get; set; } = new List<IPlugin>();
        
        public virtual IContext NewContext()
        {
            return DefaultContext.New(Plugins);
        }
        #endregion
    }
}
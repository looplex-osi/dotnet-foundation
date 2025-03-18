using System;
using System.Collections.Generic;

using Looplex.Foundation.Entities;
using Looplex.OpenForExtension.Abstractions.Plugins;

namespace Looplex.Foundation.Ports;

public interface IPluginsFactory
{
  IList<IPlugin> GetForService<T>() where T : Service;
  IList<IPlugin> GetForService(Type type);
}
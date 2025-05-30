using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Looplex.Foundation.Entities;
using Looplex.OpenForExtension.Abstractions.Plugins;

using Newtonsoft.Json.Linq;

namespace Looplex.Foundation.SCIMv2.Entities;

public abstract class SCIMv2<Tmeta, Tdata> : Service where Tmeta : Resource, new()
{
  #region Reflectivity

  // ReSharper disable once PublicConstructorInAbstractClass
  public SCIMv2() : base()
  {
  }

  #endregion

  public SCIMv2(IList<IPlugin> plugins) : base(plugins)
  {
  }

  public abstract Task<ListResponse<Tmeta>> Query(int startIndex, int count, string? filter, string? sortBy, string? sortOrder, CancellationToken cancellationToken);

  public abstract Task<Guid> Create(Tdata resource, CancellationToken cancellationToken);

  public abstract Task<Tdata?> Retrieve(Guid id, CancellationToken cancellationToken);

  public abstract Task<bool> Replace(Guid id, Tdata resource, CancellationToken cancellationToken);

  public abstract Task<bool> Update(Guid id, Tdata resource, JArray patches, CancellationToken cancellationToken);

  public abstract Task<bool> Delete(Guid id, CancellationToken cancellationToken);
}
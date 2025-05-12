using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Looplex.Foundation.Entities;
using Looplex.OpenForExtension.Abstractions.Plugins;

namespace Looplex.Foundation.SCIMv2.Entities;

public abstract class SCIMv2<Resource, FullResource> : Service where Resource : Entities.Resource, new()
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

  public abstract Task<ListResponse<Resource>> Query(int startIndex, int count,
    string? filter, string? sortBy, string? sortOrder,
    CancellationToken cancellationToken);

  public abstract Task<Guid> Create(FullResource resource, CancellationToken cancellationToken);

  public abstract Task<FullResource?> Retrieve(Guid id, CancellationToken cancellationToken);

  public abstract Task<bool> Update(Guid id, FullResource resource, string? fields, CancellationToken cancellationToken);

  public abstract Task<bool> Delete(Guid id, CancellationToken cancellationToken);
}
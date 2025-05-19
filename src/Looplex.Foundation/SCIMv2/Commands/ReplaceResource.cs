using System;

using Looplex.Foundation.SCIMv2.Entities;

using MediatR;

namespace Looplex.Foundation.SCIMv2.Commands;

public class ReplaceResource<T> : IRequest<int>
  where T : Resource
{
  public Guid Id { get; }
  public T Resource { get; }

  public ReplaceResource(Guid id, T resource)
  {
    if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty", nameof(id));
    Id = id;
    Resource = resource ?? throw new ArgumentNullException(nameof(resource));
  }
}
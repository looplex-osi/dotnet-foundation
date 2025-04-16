using System;

using Looplex.Foundation.SCIMv2.Entities;

using MediatR;

namespace Looplex.Foundation.SCIMv2.Queries;

public class RetrieveResource<T> : IRequest<T>
  where T : Resource
{
  public Guid Id { get; }

  public RetrieveResource(Guid id)
  {
    if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty", nameof(id));
    Id = id;
  }
}
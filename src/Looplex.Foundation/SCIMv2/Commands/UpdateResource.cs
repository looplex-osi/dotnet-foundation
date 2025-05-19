using System;

using Looplex.Foundation.SCIMv2.Entities;

using MediatR;

using Newtonsoft.Json.Linq;

namespace Looplex.Foundation.SCIMv2.Commands;

public class UpdateResource<T> : IRequest<int>
  where T : Resource
{
  public Guid Id { get; }
  public T Resource { get; }

  public JArray Patches { get; }

  public UpdateResource(Guid id, T resource, JArray patches)
  {
    if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty", nameof(id));
    Id = id;
    Resource = resource ?? throw new ArgumentNullException(nameof(resource));
    Patches = patches ?? throw new ArgumentNullException(nameof(patches));
  }
}
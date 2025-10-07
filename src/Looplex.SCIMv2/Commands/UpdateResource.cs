using System;

using Looplex.SCIMv2.Entities;

using MediatR;
using System.Text.Json;

namespace Looplex.SCIMv2.Commands;

public class UpdateResource<T> : IRequest<int>
  where T : Resource
{
  public Guid Id { get; }
  public T Resource { get; }

  public JsonElement Patches { get; }

  public UpdateResource(Guid id, T resource, JsonElement patches)
  {
    if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty", nameof(id));
    Id = id;
    Resource = resource ?? throw new ArgumentNullException(nameof(resource));
    Patches = patches;
  }
}

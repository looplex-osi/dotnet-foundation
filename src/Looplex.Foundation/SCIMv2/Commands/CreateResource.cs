using System;

using Looplex.Foundation.SCIMv2.Entities;

using MediatR;

namespace Looplex.Foundation.SCIMv2.Commands;

public class CreateResource<T>(T resource) : IRequest<Guid>
  where T : Resource
{
  public T Resource { get; } = resource ?? throw new ArgumentNullException(nameof(resource));
}
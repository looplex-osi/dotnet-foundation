using System;

using Looplex.Foundation.SCIMv2.Entities;

using MediatR;

namespace Looplex.Foundation.SCIMv2.Commands;

public class DeleteResourceCommand<T> : IRequest<int>
  where T : Resource
{
  public Guid Id { get; }

  public DeleteResourceCommand(Guid id)
  {
    if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty", nameof(id));
    Id = id;
  }
}
using System;
using System.Data;

using Looplex.Foundation.SCIMv2.Entities;

using MediatR;

namespace Looplex.Foundation.SCIMv2.Commands;

public class CreateResource<T>(T resource, IDbTransaction? dbTransaction = null) : IRequest<Guid> where T : Resource
{
  public T Resource { get; } = resource ?? throw new ArgumentNullException(nameof(resource));

  public IDbTransaction? DbTransaction { get; set; } = dbTransaction;
}
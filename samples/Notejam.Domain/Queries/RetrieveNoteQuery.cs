using System;
using System.Data.Common;
using System.Threading;

using Looplex.Samples.Domain.Entities;

using MediatR;

namespace Looplex.Samples.Domain.Queries
{
  public class RetrieveNoteQuery : IRequest<Note>
  {
    public DbConnection DbQuery { get; }
    public Guid Id { get; }
    public CancellationToken CancellationToken { get; }

    public RetrieveNoteQuery(DbConnection dbQuery, Guid id,
      CancellationToken cancellationToken)
    {
      DbQuery = dbQuery;
      Id = id;
      CancellationToken = cancellationToken;
    }
  }
}
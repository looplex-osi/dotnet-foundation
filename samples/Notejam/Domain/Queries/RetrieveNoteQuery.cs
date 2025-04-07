using System.Data.Common;

using Looplex.Samples.Domain.Entities;

using MediatR;

namespace Looplex.Samples.Domain.Queries
{
  public class RetrieveNoteQuery : IRequest<Note>
  {
    public DbConnection DbQuery { get; }
    public Guid Id { get; }

    public RetrieveNoteQuery(DbConnection dbQuery, Guid id)
    {
      DbQuery = dbQuery;
      Id = id;
    }
  }
}
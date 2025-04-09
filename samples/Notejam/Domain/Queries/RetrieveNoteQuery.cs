using Looplex.Samples.Domain.Entities;

using MediatR;

namespace Looplex.Samples.Domain.Queries
{
  public class RetrieveNoteQuery : IRequest<Note>
  {
    public Guid Id { get; }

    public RetrieveNoteQuery(Guid id)
    {
      Id = id;
    }
  }
}
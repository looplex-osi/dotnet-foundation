using Looplex.Samples.Domain.Entities;

using MediatR;

namespace Looplex.Samples.Domain.Queries
{
  public class RetrieveNoteQuery : IRequest<Note>
  {
    public Guid Id { get; }

    public RetrieveNoteQuery(Guid id)
    {
      if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty", nameof(id));
      Id = id;
    }
  }
}
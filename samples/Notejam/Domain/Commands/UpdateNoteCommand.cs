using Looplex.Samples.Domain.Entities;

using MediatR;

namespace Looplex.Samples.Domain.Commands
{
  public class UpdateNoteCommand : IRequest<int>
  {
    public Guid Id { get; }
    public Note Note { get; }

    public UpdateNoteCommand(Guid id, Note note)
    {
      if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty", nameof(id));
      Id = id;
      Note = note ?? throw new ArgumentNullException(nameof(note));
    }
  }
}
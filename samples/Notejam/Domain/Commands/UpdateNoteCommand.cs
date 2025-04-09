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
      Id = id;
      Note = note;
    }
  }
}
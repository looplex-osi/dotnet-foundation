using Looplex.Samples.Domain.Entities;

using MediatR;

namespace Looplex.Samples.Domain.Commands
{
  public class CreateNoteCommand : IRequest<Guid>
  {
    public Note Note { get; }

    public CreateNoteCommand(Note note)
    {
      Note = note;
    }
  }
}
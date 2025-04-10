using Looplex.Samples.Domain.Entities;

using MediatR;

namespace Looplex.Samples.Domain.Commands
{
  public class CreateNoteCommand(Note note) : IRequest<Guid>
  {
    public Note Note { get; } = note ?? throw new ArgumentNullException(nameof(note));
  }
}
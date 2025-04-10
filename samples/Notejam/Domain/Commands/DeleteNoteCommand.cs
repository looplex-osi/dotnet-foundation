using MediatR;

namespace Looplex.Samples.Domain.Commands
{
  public class DeleteNoteCommand : IRequest<int>
  {
    public Guid Id { get; }

    public DeleteNoteCommand(Guid id)
    {
      if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty", nameof(id));
      Id = id;
    }
  }
}
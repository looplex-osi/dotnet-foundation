using MediatR;

namespace Looplex.Samples.Domain.Commands
{
  public class DeleteNoteCommand : IRequest<int>
  {
    public Guid Id { get; }

    public DeleteNoteCommand(Guid id)
    {
      Id = id;
    }
  }
}
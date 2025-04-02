using Looplex.Samples.Domain.Commands;

using MediatR;

namespace Looplex.Samples.Infra.CommandHandlers
{
  public class DeleteNoteCommandHandler : IRequestHandler<DeleteNoteCommand, int>
  {
    public async Task<int> Handle(DeleteNoteCommand request, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      
      throw new System.NotImplementedException();
    }
  }
}
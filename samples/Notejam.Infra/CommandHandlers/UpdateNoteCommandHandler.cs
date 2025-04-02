using Looplex.Samples.Domain.Commands;

using MediatR;

namespace Looplex.Samples.Infra.CommandHandlers
{
  public class UpdateNoteCommandHandler : IRequestHandler<UpdateNoteCommand, int>
  {
    public async Task<int> Handle(UpdateNoteCommand request, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      
      throw new System.NotImplementedException();
    }
  }
}
using Looplex.Samples.Domain.Commands;

using MediatR;

namespace Looplex.Samples.Infra.CommandHandlers
{
  public class CreateNoteCommandHandler : IRequestHandler<CreateNoteCommand, Guid>
  {
    public async Task<Guid> Handle(CreateNoteCommand request, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      
      throw new NotImplementedException();
    }
  }
}
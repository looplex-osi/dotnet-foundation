using Looplex.Samples.Domain.Entities;
using Looplex.Samples.Domain.Queries;

using MediatR;

namespace Looplex.Samples.Infra.QueryHandlers
{
  public class RetrieveNoteQueryHandler : IRequestHandler<RetrieveNoteQuery, Note>
  {
    public async Task<Note> Handle(RetrieveNoteQuery request, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      
      throw new System.NotImplementedException();
    }
  }
}
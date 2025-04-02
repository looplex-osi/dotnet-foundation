using System;
using System.Data.Common;

using MediatR;

namespace Looplex.Samples.Domain.Commands
{
  public class DeleteNoteCommand : IRequest<int>
  {
    public DeleteNoteCommand(DbConnection dbCommand, Guid id)
    {
    }
  }
}
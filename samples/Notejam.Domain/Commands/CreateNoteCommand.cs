using System;
using System.Data.Common;

using Looplex.Samples.Domain.Entities;

using MediatR;

namespace Looplex.Samples.Domain.Commands
{
  public class CreateNoteCommand : IRequest<Guid>
  {
    public CreateNoteCommand(DbConnection dbCommand, Note note)
    {
    }
  }
}
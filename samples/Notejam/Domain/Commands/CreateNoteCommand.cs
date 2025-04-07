using System.Data.Common;

using Looplex.Samples.Domain.Entities;

using MediatR;

namespace Looplex.Samples.Domain.Commands
{
  public class CreateNoteCommand : IRequest<Guid>
  {
    public DbConnection DbCommand { get; }
    public Note Note { get; }

    public CreateNoteCommand(DbConnection dbCommand, Note note)
    {
      DbCommand = dbCommand;
      Note = note;
    }
  }
}
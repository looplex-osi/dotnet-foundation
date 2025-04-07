using System.Data.Common;

using MediatR;

namespace Looplex.Samples.Domain.Commands
{
  public class DeleteNoteCommand : IRequest<int>
  {
    public DbConnection DbCommand { get; }
    public Guid Id { get; }

    public DeleteNoteCommand(DbConnection dbCommand, Guid id)
    {
      DbCommand = dbCommand;
      Id = id;
    }
  }
}
using System.Data;

using Looplex.Foundation.Helpers;
using Looplex.Samples.Domain.Commands;
using Looplex.Samples.Domain.Entities;

using MediatR;

namespace Looplex.Samples.Infra.CommandHandlers
{
  public class DeleteNoteCommandHandler : IRequestHandler<DeleteNoteCommand, int>
  {
    public async Task<int> Handle(DeleteNoteCommand request, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      
      string resourceName = nameof(Note).ToLower();
      string procName = $"USP_{resourceName}_delete";
      
      await request.DbCommand.OpenAsync(cancellationToken);
      await using var command = request.DbCommand.CreateCommand();
      
      command.CommandType = CommandType.StoredProcedure;
      command.CommandText = procName;

      command.Parameters.Add(Dbs.CreateParameter(command, "@uuid", request.Id, DbType.Guid));

      // If your stored procedure supported a parameter for hard deletion,
      // you could add it here. For now, we assume the same proc handles deletion.
      int rows = await command.ExecuteNonQueryAsync(cancellationToken);
      
      return rows;
    }
  }
}
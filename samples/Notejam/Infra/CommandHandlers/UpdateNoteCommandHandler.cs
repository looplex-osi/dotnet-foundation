using System.Data;

using Looplex.Foundation.Helpers;
using Looplex.Samples.Domain.Commands;
using Looplex.Samples.Domain.Entities;

using MediatR;

namespace Looplex.Samples.Infra.CommandHandlers
{
  public class UpdateNoteCommandHandler : IRequestHandler<UpdateNoteCommand, int>
  {
    public async Task<int> Handle(UpdateNoteCommand request, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      
      string resourceName = nameof(Note).ToLower();
      string procName = $"USP_{resourceName}_update";
      
      await request.DbCommand.OpenAsync(cancellationToken);
      await using var command = request.DbCommand.CreateCommand();
      
      command.CommandType = CommandType.StoredProcedure;
      command.CommandText = procName;

      command.Parameters.Add(Dbs.CreateParameter(command, "@uuid", request.Id, DbType.Guid));

      // Update mapping: if the resource contains a non-null UserName, update @name.
      string? nameValue = request.Note.GetPropertyValue<string>("UserName");
      if (!string.IsNullOrWhiteSpace(nameValue))
      {
        command.Parameters.Add(Dbs.CreateParameter(command, "@name", nameValue!, DbType.String));
      }

      // Similarly, update email if available.
      string? emailValue = request.Note.GetFirstEmailValue();
      if (!string.IsNullOrWhiteSpace(emailValue))
      {
        command.Parameters.Add(Dbs.CreateParameter(command, "@email", emailValue!, DbType.String));
      }
      // Additional parameters (active, status, custom_fields) can be mapped as needed.

      int rows = await command.ExecuteNonQueryAsync(cancellationToken);

      return rows;
    }
  }
}
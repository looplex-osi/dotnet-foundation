using System.Data;

using Looplex.Foundation.Helpers;
using Looplex.Foundation.SCIMv2.Commands;
using Looplex.Samples.Application.Abstraction;
using Looplex.Samples.Domain.Entities;

using MediatR;

namespace Looplex.Samples.Infra.CommandHandlers
{
  public class UpdateNoteCommandHandler(IDbConnections connections) : IRequestHandler<UpdateResource<Note>, int>
  {
    public async Task<int> Handle(UpdateResource<Note> request, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();

      string resourceName = nameof(Note).ToLower();
      string procName = $"USP_{resourceName}_update";

      var dbCommand = await connections.CommandConnection();
      await dbCommand.OpenAsync(cancellationToken);
      await using var command = dbCommand.CreateCommand();

      command.CommandType = CommandType.StoredProcedure;
      command.CommandText = procName;

      command.Parameters.Add(Dbs.CreateParameter(command, "@uuid", request.Id, DbType.Guid));

      // Update mapping: if the resource contains a non-null UserName, update @name.
      string? nameValue = request.Resource.GetPropertyValue<string>("UserName");
      if (!string.IsNullOrWhiteSpace(nameValue))
      {
        command.Parameters.Add(Dbs.CreateParameter(command, "@name", nameValue!, DbType.String));
      }

      // Similarly, update email if available.
      string? emailValue = request.Resource.GetFirstEmailValue();
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
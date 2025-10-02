using System.Data;
using System.Data.Common;

using Looplex.Foundation.Core.Helpers;
using Looplex.Foundation.Core.SCIMv2.Commands;
using Looplex.Samples.Application;
using Looplex.Samples.Domain.Entities;

using MediatR;

namespace Looplex.Samples.Infra.CommandHandlers
{
  public class ReplaceNoteCommandHandler(IDbConnections connections) : IRequestHandler<ReplaceResource<Note>, int>
  {
    public async Task<int> Handle(ReplaceResource<Note> request, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();

      string resourceName = nameof(Note).ToLower();
      string procName = $"USP_{resourceName}_replace";

      await using var dbCommand = await connections.CommandConnection();
      await dbCommand.OpenAsync(cancellationToken);
      await using var command = dbCommand.CreateCommand();

      command.CommandType = CommandType.StoredProcedure;
      command.CommandText = procName;

      command.Parameters.Add(Dbs.CreateParameter(command, "@uuid", request.Id, DbType.Guid));

      // Replace mapping: update all fields with the resource data
      string? nameValue = request.Resource.GetPropertyValue<string>("UserName");
      if (!string.IsNullOrWhiteSpace(nameValue))
      {
        command.Parameters.Add(Dbs.CreateParameter(command, "@name", nameValue!, DbType.String));
      }

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


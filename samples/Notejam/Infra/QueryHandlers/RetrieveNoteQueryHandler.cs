using System.Data;
using System.Data.Common;

using Looplex.Foundation.Helpers;
using Looplex.Foundation.Ports;
using Looplex.Samples.Domain.Entities;
using Looplex.Samples.Domain.Queries;

using MediatR;

namespace Looplex.Samples.Infra.QueryHandlers
{
  public class RetrieveNoteQueryHandler(IDbConnections connections) : IRequestHandler<RetrieveNoteQuery, Note?>
  {
    public async Task<Note?> Handle(RetrieveNoteQuery request, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      
      string resourceName = nameof(Note).ToLower();
      string procName = $"USP_{resourceName}_retrieve";

      var dbQuery = await connections.QueryConnection();
      await dbQuery.OpenAsync(cancellationToken);
      await using var command = dbQuery.CreateCommand();

      command.CommandType = CommandType.StoredProcedure;
      command.CommandText = procName;

      command.Parameters.Add(Dbs.CreateParameter(command, "@uuid", request.Id, DbType.Guid));

      Note? obj = null;
      await using DbDataReader? reader = await command.ExecuteReaderAsync(cancellationToken);
      if (await reader.ReadAsync(cancellationToken))
      {
        obj = new Note();
        // MapDataRecordToResource(reader, obj); TODO
      }

      return obj;
    }
  }
}
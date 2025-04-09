using System.Data;

using Looplex.Foundation.Helpers;
using Looplex.Foundation.Ports;
using Looplex.Samples.Domain.Entities;
using Looplex.Samples.Domain.Queries;

using MediatR;

namespace Looplex.Samples.Infra.QueryHandlers
{
  public class QueryNoteQueryHandler(IDbConnections connections) : IRequestHandler<QueryNoteQuery, (IList<Note>, int)>
  {
    internal static readonly string[] ResultSets = [];

    public async Task<(IList<Note>, int)> Handle(QueryNoteQuery request, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      
      string resourceName = nameof(Note).ToLower();
      string procName = $"USP_{resourceName}_pquery";

      var dbQuery = await connections.QueryConnection();
      await dbQuery.OpenAsync(cancellationToken);
      await using var command = dbQuery.CreateCommand();
      
      command.CommandType = CommandType.StoredProcedure;
      command.CommandText = procName;
      
      command.Parameters.Add(Dbs.CreateParameter(command, "@do_count", true, DbType.Boolean));
      command.Parameters.Add(Dbs.CreateParameter(command, "@page", request.Page, DbType.Int32));
      command.Parameters.Add(Dbs.CreateParameter(command, "@page_size", request.PageSize, DbType.Int32));
      command.Parameters.Add(Dbs.CreateParameter(command, "@__dangerouslySetPredicate", request.Filter ?? (object)DBNull.Value, DbType.String));
      command.Parameters.Add(Dbs.CreateParameter(command, "@order_by", DBNull.Value, DbType.String));
      
      IEnumerable<SqlResultSet> result = await command.QueryAsync(ResultSets, cancellationToken);
      
      // (List<Note> list, _) = result.MapNotes(); // TODO

      var totalCount = result.GetTotalCount();

      return (new List<Note>(), totalCount);
    }
  }
}
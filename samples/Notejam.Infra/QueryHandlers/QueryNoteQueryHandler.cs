using System.Data;
using System.Data.Common;

using Looplex.Foundation.Helpers;
using Looplex.Foundation.SCIMv2.Entities;
using Looplex.Samples.Domain.Entities;
using Looplex.Samples.Domain.Queries;

using MediatR;

namespace Looplex.Samples.Infra.QueryHandlers
{
  public class QueryNoteQueryHandler : IRequestHandler<QueryNoteQuery, (IList<Note>, int)>
  {
    public async Task<ListResponse<Note>> Handle(QueryNoteQuery request, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      
      List<Note> list = new();
      string resourceName = nameof(Note).ToLower();
      string procName = $"USP_{resourceName}_pquery";

      await request.DbQuery.OpenAsync(cancellationToken);
      using var command = request.DbQuery.CreateCommand();
      command.CommandType = CommandType.StoredProcedure;
      command.CommandText = procName;

      // Add expected parameters.
      command.Parameters.Add(Dbs.CreateParameter(command, "@do_count", true, DbType.Boolean));
      command.Parameters.Add(Dbs.CreateParameter(command, "@page", request.Page, DbType.Int32));
      command.Parameters.Add(Dbs.CreateParameter(command, "@page_size", request.PageSize, DbType.Int32));
      command.Parameters.Add(Dbs.CreateParameter(command, "@__dangerouslySetPredicate", request.Filter ?? (object)DBNull.Value, DbType.String));
      command.Parameters.Add(Dbs.CreateParameter(command, "@order_by", DBNull.Value, DbType.String));

      using DbDataReader? reader = await command.Query(cancellationToken);
      while (await reader.ReadAsync(cancellationToken))
      {
        Note obj = new();
        Dbs.MapDataRecordToResource(reader, obj);
        list.Add(obj);
      }


      IEnumerable<SqlResultSet> result = command.Query(ResultSetNames.CASE_INFO_QUERY);

      (List<CaseInfo> list, _) = result.MapCaseInfo();

      ctx.Result = new ListResponse<CaseInfo>
      {
        StartIndex = startIndex, 
        ItemsPerPage = count, 
        Resources = list, 
        TotalResults = result.GetTotalCount()
      };
      
      return new ListResponse<Note>
      {
        StartIndex = startIndex, ItemsPerPage = count, Resources = list, TotalResults = 0 // TODO
      };
      
      
      if (!ctx.SkipDefaultAction)
      {
        string procName = ctx.Roles["ProcName"];
        string tenant = _user!.Claims.FirstOrDefault(c => c.Type == "tenant")?.Value!;

        var (dbConnection, databaseName) = await _dbConnectionProvider!.GetConnectionAsync(tenant);

        await using var db = (SqlConnection)dbConnection;

        await db.OpenAsync(cancellationToken);
        await db.ChangeDatabaseAsync(databaseName, cancellationToken);

        
      }
    }
  }
}
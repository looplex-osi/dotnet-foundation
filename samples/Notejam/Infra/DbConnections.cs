using System.Data.Common;

using Looplex.Samples.Application.Abstraction;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Looplex.Samples.Infra;

public class DbConnections(
  IConfiguration configuration) : IDbConnections
{
  public Task<DbConnection> CommandConnection()
  {
    var commandConnString = configuration["CommandConnectionString"];
    return Task.FromResult<DbConnection>(new SqlConnection(commandConnString));
  }

  public Task<DbConnection> QueryConnection()
  {
    var queryConnString = configuration["QueryConnectionString"];
    return Task.FromResult<DbConnection>(new SqlConnection(queryConnString));
  }
}
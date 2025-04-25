using System.Data.Common;

namespace Looplex.Samples.Application.Abstraction;

public interface IDbConnections
{
  /// <summary>
  /// Returns the command (write) connection
  /// </summary>
  /// <returns>Returns connection for the tenant</returns>
  Task<DbConnection> CommandConnection();

  /// <summary>
  /// Returns the query (read) connection
  /// </summary>
  /// <returns>Returns connection for the tenant</returns>
  Task<DbConnection> QueryConnection();
}
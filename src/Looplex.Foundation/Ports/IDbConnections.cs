using System.Data.Common;
using System.Threading.Tasks;

namespace Looplex.Foundation.Ports;

public interface IDbConnections
{
  /// <summary>
  /// Returns the command (write) connection for a tenant
  /// </summary>
  /// <param name="tenant">The tenant to get the database name and command (write) connection for</param>
  /// <returns>Returns (connection, databaseName) for the tenant</returns>
  Task<(DbConnection, string)> CommandConnection(string tenant);

  /// <summary>
  /// Returns the query (read) connection for a tenant
  /// </summary>
  /// <param name="tenant">The tenant to get the database name and query (read) connection for</param>
  /// <returns>Returns (connection, databaseName) for the tenant</returns>
  Task<(DbConnection, string)> QueryConnection(string tenant);

  /// <summary>
  /// Returns the command (write) connection
  /// </summary>
  /// <returns>Returns (connection, databaseName) for the tenant</returns>
  Task<DbConnection> CommandConnection();

  /// <summary>
  /// Returns the query (read) connection
  /// </summary>
  /// <returns>Returns (connection, databaseName) for the tenant</returns>
  Task<DbConnection> QueryConnection();
}
using System.Data.Common;
using System.Threading.Tasks;

namespace Looplex.Foundation.Ports;

public interface IDbConnectionProvider
{
  /// <summary>
  /// </summary>
  /// <param name="tenant">The tenant to get the database name and connection for</param>
  /// <returns>Returns (connection, databaseName) for the tenant</returns>
  Task<(DbConnection, string)> GetConnectionAsync(string tenant);
}
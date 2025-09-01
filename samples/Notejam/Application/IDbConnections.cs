using System.Data.Common;

namespace Looplex.Samples.Application;

public interface IDbConnections
{
    Task<DbConnection> CommandConnection();
    Task<DbConnection> QueryConnection();
}





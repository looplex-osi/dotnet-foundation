using System.Data.Common;

namespace Looplex.Samples.Application.Abstraction;

public interface IDbConnections
{
    Task<DbConnection> CommandConnection();
    Task<DbConnection> QueryConnection();
}
using System.Data.Common;

using Looplex.Samples.Application;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.Sqlite;

namespace Looplex.Samples.Infra;

public class DbConnections(IConfiguration configuration) : IDbConnections
{
  public async Task<DbConnection> CommandConnection()
  {
    return await CreateConnectionAsync();
  }

  public async Task<DbConnection> QueryConnection()
  {
    return await CreateConnectionAsync();
  }
  
  private async Task<DbConnection> CreateConnectionAsync()
  {
    // Use default value to avoid configuration binding issues
    var useRealDatabase = false;
    try
    {
      var value = configuration["UseRealDatabase"];
      useRealDatabase = bool.TryParse(value, out var result) && result;
    }
    catch
    {
      // If configuration binding fails, default to in-memory database
      useRealDatabase = false;
    }
    
    if (useRealDatabase)
    {
      return await CreateRealDatabaseConnection();
    }
    else
    {
      return CreateInMemoryDatabaseConnection();
    }
  }
  
  private async Task<DbConnection> CreateRealDatabaseConnection()
  {
    var connectionString = configuration.GetConnectionString("RoutingDatabaseConnectionString");
    var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    return connection;
  }
  
  private DbConnection CreateInMemoryDatabaseConnection()
  {
    var connection = new SqliteConnection("Data Source=:memory:");
    connection.Open();

    InitializeInMemoryDatabase(connection);
    return connection;
  }
  
  private void InitializeInMemoryDatabase(DbConnection connection)
  {
    CreateNotesTable(connection);
    InsertSampleData(connection);
  }
  
  private void CreateNotesTable(DbConnection connection)
  {
    using var createTableCommand = connection.CreateCommand();
    createTableCommand.CommandText = @"
      CREATE TABLE IF NOT EXISTS Notes (
        Id TEXT PRIMARY KEY,
        Name TEXT,
        Text TEXT,
        Created TEXT,
        Modified TEXT,
        IsActive INTEGER,
        PadId TEXT
      )";
    createTableCommand.ExecuteNonQuery();
  }
  
  private void InsertSampleData(DbConnection connection)
  {
    using var insertCommand = connection.CreateCommand();
    insertCommand.CommandText = @"
      INSERT OR IGNORE INTO Notes (Id, Name, Text, Created, Modified, IsActive, PadId) VALUES
      ('1', 'Minha Nota', 'Esta é uma nota importante sobre o projeto', '2024-01-15T10:00:00Z', '2024-01-15T10:00:00Z', 1, 'pad-1'),
      ('2', 'Nota de Demo', 'Demonstração da funcionalidade de busca', '2024-01-16T14:30:00Z', '2024-01-16T14:30:00Z', 1, 'pad-1'),
      ('3', 'Tarefa Urgente', 'Preciso resolver isso rapidamente', '2024-01-17T09:15:00Z', '2024-01-17T09:15:00Z', 1, 'pad-2'),
      ('4', 'Nota Excluída', 'Esta nota foi removida', '2024-01-18T16:45:00Z', '2024-01-18T16:45:00Z', 0, 'pad-1'),
      ('5', 'Projeto Demo', 'Documentação do projeto de demonstração', '2024-01-19T11:20:00Z', '2024-01-19T11:20:00Z', 1, 'pad-3')";
    insertCommand.ExecuteNonQuery();
  }
}

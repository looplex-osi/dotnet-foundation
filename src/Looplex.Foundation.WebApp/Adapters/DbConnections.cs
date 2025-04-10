using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Net;

using Looplex.Foundation.Ports;
using Looplex.Foundation.SCIMv2.Entities;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Looplex.Foundation.WebApp.Adapters;

public class DbConnections(
  IHostEnvironment hostEnvironment,
  ILogger<DbConnections> logger,
  IConfiguration configuration,
  ISecretsService secretsService) : IDbConnections
{
  private SqlConnection? _routingSqlConnection;

  private static readonly ConcurrentDictionary<string, LawOfficeDatabase> ConnectionStringsCache =
    new ConcurrentDictionary<string, LawOfficeDatabase>();

  internal SqlConnection RoutingSqlConnection
  {
    get
    {
      if (_routingSqlConnection == null)
      {
        var routingConnString = configuration["RoutingDatabaseConnectionString"];

        _routingSqlConnection = new SqlConnection(routingConnString);
      }

      return _routingSqlConnection;
    }
    set => _routingSqlConnection = value;
  }

  public async Task<(DbConnection, string)> CommandConnection(string tenant)
  {
    var database = ConnectionStringsCache.TryGetValue(tenant, out var value)
      ? value
      : await GetDatabaseUsingRoutingDatabaseAsync(tenant, "write");

    var databaseName = database.Name!;
    var connection = new SqlConnection(database.ConnectionString);
    return (connection, databaseName);
  }

  private async Task<LawOfficeDatabase> GetDatabaseUsingRoutingDatabaseAsync(string domain, string user)
  {
    var query = @"
            SELECT d.name AS Name, d.keyvault_id AS KeyVaultId
                
            FROM lawoffice.databases d
            JOIN lawoffice.customers_databases cd ON 
                cd.database_id = d.id
            JOIN lawoffice.lawoffice.customers c ON 
                cd.customer_id = c.id
            WHERE 
                    c.domain = @Domain
                AND c.status = @Status
        ";

    if (RoutingSqlConnection.State == ConnectionState.Closed)
      await RoutingSqlConnection.OpenAsync(CancellationToken.None);

    await using var cmd = new SqlCommand(query, RoutingSqlConnection);
    cmd.Parameters.Add("@Domain", SqlDbType.NVarChar).Value = domain;
    cmd.Parameters.Add("@Status", SqlDbType.Int).Value = (int)CustomerStatus.Active;

    await using var reader = await cmd.ExecuteReaderAsync();
    LawOfficeDatabase? database = null;
    if (await reader.ReadAsync())
    {
      database = new LawOfficeDatabase
      {
        Name = reader["Name"].ToString(),
        KeyVaultId = reader["KeyVaultId"] != DBNull.Value ? reader["KeyVaultId"].ToString() : null
      };
    }

    if (string.IsNullOrEmpty(database?.KeyVaultId) || string.IsNullOrEmpty(database?.Name))
    {
      logger.LogError("Unable to connect to database for tenant {Tenant}. Key vault id is null or empty",
        domain);
      throw new SCIMv2Exception($"Unable to connect to database for domain {domain}",
        (int)HttpStatusCode.InternalServerError);
    }

    var connectionString = await secretsService
      .GetSecretAsync(database.KeyVaultId);

    try
    {
      var builder = new SqlConnectionStringBuilder(connectionString);

      // sets read or write user
      builder.UserID = user;
      database.ConnectionString = builder.ConnectionString;
      
      // logs connction string without password
      if (!hostEnvironment.IsDevelopment())
        builder.Password = "****";
      logger.LogInformation(
        "Database: Getting connection string for tenant {Tenant}. Result: {ConnectionString}",
        domain, builder.ToString());
    }
    catch (Exception e)
    {
      logger.LogError("Unable to connect to database for tenant {Tenant}. Exception: {Exception}", domain, e);
      throw new SCIMv2Exception($"Unable to connect to database for tenant {domain}",
        (int)HttpStatusCode.InternalServerError, e);
    }

    _ = ConnectionStringsCache.TryAdd(domain, database);

    return database;
  }

  public async Task<(DbConnection, string)> QueryConnection(string tenant)
  {
    var database = ConnectionStringsCache.TryGetValue(tenant, out var value)
      ? value
      : await GetDatabaseUsingRoutingDatabaseAsync(tenant, "read");

    var databaseName = database.Name!;
    var connection = new SqlConnection(database.ConnectionString);
    return (connection, databaseName);
  }

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

internal class LawOfficeDatabase
{
  public string? Name { get; set; }
  public string? ConnectionString { get; set; }
  public string? KeyVaultId { get; set; }
}

public enum CustomerStatus
{
  // TODO 
  Active = 1
}
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

public class DbConnectionProvider(
    IHostEnvironment hostEnvironment,
    ILogger<DbConnectionProvider> logger,
    IConfiguration configuration,
    ISecretsService secretsService) : IDbConnectionProvider
{
    private SqlConnection? _routingSqlConnection;

    private readonly IDictionary<string, LawOfficeDatabase> _connectionStringsCache = new Dictionary<string, LawOfficeDatabase>();

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

    public async Task<(DbConnection, string)> GetConnectionAsync(string domain)
    {
        var database = _connectionStringsCache.TryGetValue(domain, out var value)
            ? value
            : await GetDatabaseUsingRoutingDatabaseAsync(domain);

        var databaseName = database.Name!;
        var connection = new SqlConnection(database.ConnectionString);
        return (connection, databaseName);
    }

    private async Task<LawOfficeDatabase> GetDatabaseUsingRoutingDatabaseAsync(string domain)
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
        
        database.ConnectionString = connectionString;

        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);

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

        _connectionStringsCache.Add(domain, database);
        
        return database;
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
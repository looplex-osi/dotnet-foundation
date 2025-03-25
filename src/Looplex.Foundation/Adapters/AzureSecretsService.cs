using System;
using System.Threading.Tasks;

using Azure;
using Azure.Security.KeyVault.Secrets;

using Looplex.Foundation.Ports;

using Microsoft.Extensions.Logging;

using Polly;

namespace Looplex.Foundation.Adapters;

public class AzureSecretsService : ISecretsService
{
  private readonly ILogger<AzureSecretsService> _logger;
  private readonly IAsyncPolicy<string> _retryPolicy;
  private readonly SecretClient _secretClient;

  public AzureSecretsService(SecretClient secretClient, IAsyncPolicy<string> retryPolicy,
    ILogger<AzureSecretsService> logger)
  {
    _secretClient = secretClient ?? throw new ArgumentNullException(nameof(secretClient));
    _retryPolicy = retryPolicy;
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public async Task<string?> GetSecretAsync(string secretName)
  {
    if (secretName == null) throw new ArgumentNullException(nameof(secretName));

    return await _retryPolicy.ExecuteAsync(async () =>
    {
      try
      {
        var keyVaultSecret = await _secretClient.GetSecretAsync(secretName);

        if (keyVaultSecret?.Value == null)
        {
          throw new Exception("AZURE_RESPONSE_IS_NULL");
        }

        return keyVaultSecret.Value.Value;
      }
      catch (RequestFailedException ex)
      {
        _logger.LogError(ex, "Failed to retrieve secret: {secret}", secretName);
        throw;
      }
    });
  }
}
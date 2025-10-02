using System.Threading.Tasks;

namespace Looplex.Foundation.Core.Ports;

public interface ISecretsService
{
  Task<string?> GetSecretAsync(string secretName);
}

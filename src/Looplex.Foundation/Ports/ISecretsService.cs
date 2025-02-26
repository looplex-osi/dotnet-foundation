using System.Threading.Tasks;

namespace Looplex.Foundation.Ports
{
    public interface ISecretsService
    {
        Task<string> GetSecretAsync(string secretName);
    }
}
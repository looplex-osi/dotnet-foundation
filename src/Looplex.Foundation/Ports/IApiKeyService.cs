using System.Threading.Tasks;
using Looplex.OpenForExtension.Abstractions.Contexts;

namespace Looplex.Foundation.Ports;

public interface IApiKeyService
{
    Task GetByIdAndSecretOrDefaultAsync(IContext context);
}
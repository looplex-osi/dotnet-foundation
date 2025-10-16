using System.Threading.Tasks;

namespace Looplex.Foundation.Ports;

public interface ICacheService
{
  Task SetAsync(string key, string value);
  Task<string> GetAsync(string key);
  Task<bool> DeleteAsync(string key);
}

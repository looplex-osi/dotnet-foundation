using System.Collections.Generic;
using System.Threading.Tasks;

using Looplex.OpenForExtension.Abstractions.Contexts;

namespace Looplex.Foundation.Ports;

public interface IJsonSchemaProvider
{
  Task<List<string>> ResolveJsonSchemasAsync(IContext context, List<string> schemaIds, string lang = null);
  Task<string> ResolveJsonSchemaAsync(IContext context, string schemaId, string lang = null);
}
using System.Collections.Generic;
using System.Threading.Tasks;

using Looplex.OpenForExtension.Abstractions.Contexts;

namespace Looplex.SCIMv2;

public interface IJsonSchemaService
{
  Task<List<string>> ResolveJsonSchemasAsync(IContext context, List<string> schemaIds, string? lang = null);
  Task<string> ResolveJsonSchemaAsync(IContext context, string schemaId, string? lang = null);
}

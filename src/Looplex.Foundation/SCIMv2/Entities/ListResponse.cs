using System.Collections.Generic;

using Looplex.Foundation.Entities;

namespace Looplex.Foundation.SCIMv2.Entities;

public class ListResponse<T> : Actor where T : Resource
{
  public long TotalResults { get; set; }
  public List<T> Resources { get; set; } = [];
  public long Page { get; set; }
  public long PageSize { get; set; }
}
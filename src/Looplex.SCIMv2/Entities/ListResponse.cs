using System.Collections.Generic;
using System.Text.Json.Serialization;

using Looplex.Foundation.Entities;

namespace Looplex.SCIMv2.Entities;

public class ListResponse<T> : Actor
{
  /// <summary>
  /// The total number of results returned by the list or query operation. This value may be larger than the number of resources returned if pagination is used. REQUIRED.
  /// </summary>
  public long TotalResults { get; set; }

  /// <summary>
  /// A list of complex objects containing the requested resources. REQUIRED if 'totalResults' is non-zero.
  /// </summary>
  [JsonPropertyName("Resources")]
  public IList<T> Resources { get; set; } = [];

  /// <summary>
  /// The 1-based index of the first result in the current set of list results. REQUIRED when partial results are returned due to pagination.
  /// </summary>
  public long StartIndex { get; set; }

  /// <summary>
  /// The number of resources returned in a list response page. REQUIRED when partial results are returned due to pagination.
  /// </summary>
  public long ItemsPerPage { get; set; }
}

public class ListResponseContinuous<T> : Actor
{
  public long StartIndex { get; set; }
  public long ItemsPerPage { get; set; }
  public bool? HasNext { get; set; } = null;
  [JsonPropertyName("Resources")] public List<T> Resources { get; set; } = [];
}

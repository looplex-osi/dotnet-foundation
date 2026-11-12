using Newtonsoft.Json;

namespace Looplex.Foundation.SCIMv2.Entities;

public class SCIMv2Search
{
  [JsonProperty("schemas")]
  public string[] Schemas { get; set; } = [];

  [JsonProperty("filter")]
  public string? Filter { get; set; } = null;

  [JsonProperty("sortBy")]
  public string? SortBy { get; set; } = null;

  [JsonProperty("sortOrder")]
  public string? SortOrder { get; set; } = null;

  [JsonProperty("startIndex")]
  public int StartIndex { get; set; } = 1;

  [JsonProperty("count")]
  public int Count { get; set; } = 10;
}

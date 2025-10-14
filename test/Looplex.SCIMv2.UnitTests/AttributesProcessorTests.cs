using Looplex.SCIMv2.Entities;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

using System.Text.Json.Nodes;

namespace Looplex.Foundation.Core.UnitTests.SCIMv2.Entities;

// Mock implementation of IQueryCollection for testing
public class MockQueryCollection : IQueryCollection
{
    private readonly Dictionary<string, Microsoft.Extensions.Primitives.StringValues> _values;

    public MockQueryCollection(Dictionary<string, Microsoft.Extensions.Primitives.StringValues> values)
    {
        _values = values;
    }

    public Microsoft.Extensions.Primitives.StringValues this[string key] => _values.TryGetValue(key, out var value) ? value : Microsoft.Extensions.Primitives.StringValues.Empty;

    public int Count => _values.Count;

    public ICollection<string> Keys => _values.Keys;

    public bool ContainsKey(string key) => _values.ContainsKey(key);

    public IEnumerator<KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>> GetEnumerator() => _values.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    public bool TryGetValue(string key, out Microsoft.Extensions.Primitives.StringValues value) => _values.TryGetValue(key, out value);
}

[TestClass]
public class AttributeProcessorTests
{
  private static HttpContext CreateHttpContext(string? attributes = null, string? excludedAttributes = null)
  {
    var context = new DefaultHttpContext();
    var queryParams = new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>();
    if (attributes != null) queryParams["attributes"] = attributes;
    if (excludedAttributes != null) queryParams["excludedAttributes"] = excludedAttributes;
    
    // Create a mock IQueryCollection
    var query = new MockQueryCollection(queryParams);
    context.Request.Query = query;
    return context;
  }

  [TestMethod]
  public void SimpleJson_AttributesOnly()
  {
    var context = CreateHttpContext("name");

    var records = new List<JsonObject>
    {
      new JsonObject { ["name"] = "John", ["age"] = 30, ["email"] = "john@example.com" },
      new JsonObject { ["name"] = "Jane", ["age"] = 25, ["email"] = "jane@example.com" }
    };

    var result = records.ProcessAttributes(context);

    foreach (var r in result)
    {
      Assert.IsTrue(r.ContainsKey("name"));
      Assert.IsFalse(r.ContainsKey("age"));
      Assert.IsFalse(r.ContainsKey("email"));
    }
  }

  [TestMethod]
  public void SimpleJson_ExcludedAttributesOnly()
  {
    var context = CreateHttpContext(null, "email");

    var records = new List<JsonObject>
    {
      new JsonObject { ["name"] = "John", ["age"] = 30, ["email"] = "john@example.com" },
      new JsonObject { ["name"] = "Jane", ["age"] = 25, ["email"] = "jane@example.com" }
    };

    var result = records.ProcessAttributes(context);

    foreach (var r in result)
    {
      Assert.IsTrue(r.ContainsKey("name"));
      Assert.IsTrue(r.ContainsKey("age"));
      Assert.IsFalse(r.ContainsKey("email"));
    }
  }

  [TestMethod]
  public void SimpleJson_BothAttributesAndExcluded()
  {
    var context = CreateHttpContext("name,email", "email");

    var records = new List<JsonObject>
    {
      new JsonObject { ["name"] = "John", ["age"] = 30, ["email"] = "john@example.com" },
      new JsonObject { ["name"] = "Jane", ["age"] = 25, ["email"] = "jane@example.com" }
    };

    var result = records.ProcessAttributes(context);

    foreach (var r in result)
    {
      Assert.IsTrue(r.ContainsKey("name"));
      Assert.IsFalse(r.ContainsKey("email"));
      Assert.IsFalse(r.ContainsKey("age"));
    }
  }

  [TestMethod]
  public void NestedJson_TwoDepth()
  {
    var context = CreateHttpContext("profile.name,profile.address.city");

    var records = new List<JsonObject>
    {
      (JsonObject)JsonObject.Parse(@"{
                  ""profile"": {
                    ""name"": ""John"",
                    ""address"": { ""city"": ""LA"", ""zip"": ""90001"" }
                  }
                }")
    };

    var result = records.ProcessAttributes(context);
    var first = result.First();

    Assert.AreEqual("John", first["profile"]?["name"]?.ToString());
    Assert.AreEqual("LA", first["profile"]?["address"]?["city"]?.ToString());
    Assert.IsNull(first["profile"]?["address"]?["zip"]);
  }

  [TestMethod]
  public void ArrayOfObjects_AttributesOnly()
  {
    var context = CreateHttpContext("items");

    var records = new List<JsonObject>
    {
      (JsonObject)JsonObject.Parse(@"{
                  ""items"": [
                    { ""name"": ""Item1"", ""price"": 10 },
                    { ""name"": ""Item2"", ""price"": 20 }
                  ]
                }")
    };

    var result = records.ProcessAttributes(context);
    var first = result.First();
    var array = first["items"] as JsonArray;

    Assert.IsNotNull(array);
    Assert.IsTrue(array.Count > 0);
    
    foreach (var item in array)
    {
      Assert.IsNotNull(item["name"]);
      Assert.IsNotNull(item["price"]);
    }
  }

  [TestMethod]
  public void ArrayOfObjects_WithChild()
  {
    var context = CreateHttpContext("items");

    var records = new List<JsonObject>
    {
      (JsonObject)JsonObject.Parse(@"{
                  ""items"": [
                    { ""details"": { ""value"": 1, ""extra"": ""x"" } },
                    { ""details"": { ""value"": 2, ""extra"": ""y"" } }
                  ]
                }")
    };

    var result = records.ProcessAttributes(context);
    var first = result.First();
    var array = first["items"] as JsonArray;

    Assert.IsNotNull(array);
    Assert.IsTrue(array.Count > 0);
    
    foreach (var item in array)
    {
      Assert.IsNotNull(item["details"]?["value"]);
      Assert.IsNotNull(item["details"]?["extra"]);
    }
  }
}

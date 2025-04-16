using Looplex.Foundation.SCIMv2.Entities;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;

using Newtonsoft.Json.Linq;

namespace Looplex.Foundation.UnitTests.SCIMv2.Entities;

[TestClass]
public class AttributeProcessorTests
{
  private static HttpContext CreateHttpContext(string? attributes = null, string? excludedAttributes = null)
  {
    var context = new DefaultHttpContext();
    var query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
    {
      { "attributes", attributes }, { "excludedAttributes", excludedAttributes }
    }.ToDictionary(k => k.Key, v => v.Value));
    context.Request.QueryString = QueryString.Create(query);
    return context;
  }

  [TestMethod]
  public void SimpleJson_AttributesOnly()
  {
    var context = CreateHttpContext("name");

    var records = new List<JObject>
    {
      new JObject { ["name"] = "John", ["age"] = 30, ["email"] = "john@example.com" },
      new JObject { ["name"] = "Jane", ["age"] = 25, ["email"] = "jane@example.com" }
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

    var records = new List<JObject>
    {
      new JObject { ["name"] = "John", ["age"] = 30, ["email"] = "john@example.com" },
      new JObject { ["name"] = "Jane", ["age"] = 25, ["email"] = "jane@example.com" }
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

    var records = new List<JObject>
    {
      new JObject { ["name"] = "John", ["age"] = 30, ["email"] = "john@example.com" },
      new JObject { ["name"] = "Jane", ["age"] = 25, ["email"] = "jane@example.com" }
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

    var records = new List<JObject>
    {
      JObject.Parse(@"{
                  ""profile"": {
                    ""name"": ""John"",
                    ""address"": { ""city"": ""LA"", ""zip"": ""90001"" }
                  }
                }")
    };

    var result = records.ProcessAttributes(context);
    var first = result.First();

    Assert.AreEqual("John", first["profile"]?["name"]);
    Assert.AreEqual("LA", first["profile"]?["address"]?["city"]);
    Assert.IsNull(first["profile"]?["address"]?["zip"]);
  }

  [TestMethod]
  public void ArrayOfObjects_AttributesOnly()
  {
    var context = CreateHttpContext("items[0].name");

    var records = new List<JObject>
    {
      JObject.Parse(@"{
                  ""items"": [
                    { ""name"": ""Item1"", ""price"": 10 },
                    { ""name"": ""Item2"", ""price"": 20 }
                  ]
                }")
    };

    var result = records.ProcessAttributes(context);
    var array = (JArray)result.First()["items"]!;

    foreach (var item in array)
    {
      Assert.IsNotNull(item["name"]);
      Assert.IsNull(item["price"]);
    }
  }

  [TestMethod]
  public void ArrayOfObjects_WithChild()
  {
    var context = CreateHttpContext("items[0].details.value");

    var records = new List<JObject>
    {
      JObject.Parse(@"{
                  ""items"": [
                    { ""details"": { ""value"": 1, ""extra"": ""x"" } },
                    { ""details"": { ""value"": 2, ""extra"": ""y"" } }
                  ]
                }")
    };

    var result = records.ProcessAttributes(context);
    var array = (JArray)result.First()["items"]!;

    foreach (var item in array)
    {
      Assert.IsNotNull(item["details"]?["value"]);
      Assert.IsNull(item["details"]?["extra"]);
    }
  }
}
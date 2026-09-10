using Looplex.Foundation.SCIMv2.Entities;

using Newtonsoft.Json.Linq;

namespace Looplex.Foundation.UnitTests.SCIMv2.Entities;

/// <summary>
/// Overload without HttpContext, for drivers that are not HTTP (MCP tools, workers).
/// </summary>
[TestClass]
public class AttributesProcessorStringOverloadTests
{
  private static List<JObject> Records() =>
  [
    new JObject { ["name"] = "John", ["age"] = 30, ["address"] = new JObject { ["city"] = "Rio", ["zip"] = "1" } },
    new JObject { ["name"] = "Jane", ["age"] = 25, ["address"] = new JObject { ["city"] = "SP", ["zip"] = "2" } },
  ];

  [TestMethod]
  public void Attributes_KeepsOnlyListedPaths_IncludingNested()
  {
    var result = Records().ProcessAttributes("name, address.city", null).ToList();

    foreach (var r in result)
    {
      Assert.IsTrue(r.ContainsKey("name"));
      Assert.IsFalse(r.ContainsKey("age"));
      Assert.IsNotNull(r["address"]!["city"]);
      Assert.IsNull(r["address"]!["zip"]);
    }
  }

  [TestMethod]
  public void ExcludedAttributes_RemovesPaths()
  {
    var result = Records().ProcessAttributes(null, "age,address.zip").ToList();

    foreach (var r in result)
    {
      Assert.IsTrue(r.ContainsKey("name"));
      Assert.IsFalse(r.ContainsKey("age"));
      Assert.IsNull(r["address"]!["zip"]);
      Assert.IsNotNull(r["address"]!["city"]);
    }
  }

  [TestMethod]
  public void NoParameters_ReturnsRecordsUnchanged()
  {
    var records = Records();
    var before = records.Select(r => r.ToString()).ToList();

    var result = records.ProcessAttributes(null, "").Select(r => r.ToString()).ToList();

    CollectionAssert.AreEqual(before, result);
  }
}

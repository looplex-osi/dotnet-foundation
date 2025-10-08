using Looplex.Foundation.Entities;
using Looplex.Foundation.Serialization;

namespace Looplex.Foundation.Core.UnitTests.Serialization;

[TestClass]
public class ActorJsonSerializerTests
{
  private TestActor _actor = null!;

  [TestInitialize]
  public void Setup()
  {
    _actor = new TestActor { Name = "Test Name" };
  }

  // JSON Serialization Tests
  [TestMethod]
  public void JsonSerialize_ShouldConvertActorToJsonString()
  {
    // Act
    string json = JsonSerializer.Serialize(_actor, JsonSerializer.DefaultOptions);

    // Assert
    Assert.IsNotNull(json);
    Assert.IsTrue(json.Contains("\"name\":\"Test Name\""));
  }

  [TestMethod]
  public void JsonDeserialize_ShouldConvertJsonStringToActor()
  {
    // Arrange
    string json = JsonSerializer.Serialize(_actor, JsonSerializer.DefaultOptions);

    // Act
    TestActor? deserializedActor = JsonSerializer.Deserialize<TestActor>(json, JsonSerializer.DefaultOptions);

    // Assert
    Assert.IsNotNull(deserializedActor);
    Assert.AreEqual("Test Name", deserializedActor.Name);
  }

  [TestMethod]
  [ExpectedException(typeof(System.Text.Json.JsonException))]
  public void JsonDeserialize_ShouldThrowExceptionForEmptyJson()
  {
    // Act
    JsonSerializer.Deserialize<TestActor>("", JsonSerializer.DefaultOptions);
  }

  [TestMethod]
  [ExpectedException(typeof(ArgumentNullException))]
  public void JsonSerialize_ShouldThrowExceptionForNullActor()
  {
    // Act
    Actor? nullActor = null;
    JsonSerializer.Serialize(nullActor, JsonSerializer.DefaultOptions);
  }

  public class TestActor : Actor
  {
    public required string Name { get; set; }
  }
}

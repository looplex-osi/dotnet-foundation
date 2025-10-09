using Looplex.Foundation.Entities;
using Looplex.Foundation.Serialization;

namespace Looplex.Foundation.Core.UnitTests.Serialization;

[TestClass]
public class ActorFoundationJsonSerializerTests
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
    string json = FoundationJsonSerializer.Serialize(_actor, FoundationJsonSerializer.DefaultOptions);

    // Assert
    Assert.IsNotNull(json);
    Assert.IsTrue(json.Contains("\"name\":\"Test Name\""));
  }

  [TestMethod]
  public void JsonDeserialize_ShouldConvertJsonStringToActor()
  {
    // Arrange
    string json = FoundationJsonSerializer.Serialize(_actor, FoundationJsonSerializer.DefaultOptions);

    // Act
    TestActor? deserializedActor = FoundationJsonSerializer.Deserialize<TestActor>(json, FoundationJsonSerializer.DefaultOptions);

    // Assert
    Assert.IsNotNull(deserializedActor);
    Assert.AreEqual("Test Name", deserializedActor.Name);
  }

  [TestMethod]
  [ExpectedException(typeof(System.Text.Json.JsonException))]
  public void JsonDeserialize_ShouldThrowExceptionForEmptyJson()
  {
    // Act
    FoundationJsonSerializer.Deserialize<TestActor>("", FoundationJsonSerializer.DefaultOptions);
  }

  [TestMethod]
  [ExpectedException(typeof(ArgumentNullException))]
  public void JsonSerialize_ShouldThrowExceptionForNullActor()
  {
    // Act
    Actor? nullActor = null;
    FoundationJsonSerializer.Serialize(nullActor, FoundationJsonSerializer.DefaultOptions);
  }

  public class TestActor : Actor
  {
    public required string Name { get; set; }
  }
}

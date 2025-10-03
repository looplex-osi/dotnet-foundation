using Looplex.Foundation.Entities;
using Looplex.SCIMv2.Serialization;

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
    string json = ActorJsonSerializer.Serialize(_actor);

    // Assert
    Assert.IsNotNull(json);
    Assert.IsTrue(json.Contains("\"name\": \"Test Name\""));
  }

  [TestMethod]
  public void JsonDeserialize_ShouldConvertJsonStringToActor()
  {
    // Arrange
    string json = ActorJsonSerializer.Serialize(_actor);

    // Act
    TestActor? deserializedActor = ActorJsonSerializer.Deserialize<TestActor>(json);

    // Assert
    Assert.IsNotNull(deserializedActor);
    Assert.AreEqual("Test Name", deserializedActor.Name);
  }

  [TestMethod]
  [ExpectedException(typeof(ArgumentException))]
  public void JsonDeserialize_ShouldThrowExceptionForEmptyJson()
  {
    // Act
    ActorJsonSerializer.Deserialize<TestActor>("");
  }

  [TestMethod]
  [ExpectedException(typeof(ArgumentNullException))]
  public void JsonSerialize_ShouldThrowExceptionForNullActor()
  {
    // Act
    Actor? nullActor = null;
    ActorJsonSerializer.Serialize(nullActor);
  }

  public class TestActor : Actor
  {
    public required string Name { get; set; }
  }
}

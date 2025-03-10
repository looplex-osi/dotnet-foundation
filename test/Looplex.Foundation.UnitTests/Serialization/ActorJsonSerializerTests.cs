using Looplex.Foundation.Entities;
using Looplex.Foundation.Serialization.Json;

namespace Looplex.Foundation.UnitTests.Serialization;

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
    string json = _actor.Serialize();

    // Assert
    Assert.IsNotNull(json);
    Assert.IsTrue(json.Contains("\"name\": \"Test Name\""));
  }

  [TestMethod]
  public void JsonDeserialize_ShouldConvertJsonStringToActor()
  {
    // Arrange
    string json = _actor.Serialize();

    // Act
    TestActor? deserializedActor = json.Deserialize<TestActor>();

    // Assert
    Assert.IsNotNull(deserializedActor);
    Assert.AreEqual("Test Name", deserializedActor.Name);
  }

  [TestMethod]
  [ExpectedException(typeof(ArgumentException))]
  public void JsonDeserialize_ShouldThrowExceptionForEmptyJson()
  {
    // Act
    "".Deserialize<TestActor>();
  }

  [TestMethod]
  [ExpectedException(typeof(ArgumentNullException))]
  public void JsonSerialize_ShouldThrowExceptionForNullActor()
  {
    // Act
    Actor? nullActor = null;
    nullActor.Serialize();
  }

  public class TestActor : Actor
  {
    public required string Name { get; set; }
  }
}
using Looplex.Foundation.Entities;
using Looplex.Foundation.Serialization.Protobuf;

using ProtoBuf;

namespace Looplex.Foundation.UnitTests.Serialization;

[TestClass]
public class ActorProtobufSerializerTests
{
  private TestActor _actor = null!;

  [TestInitialize]
  public void Setup()
  {
    _actor = new TestActor { Name = "Test Name" };
  }

  // Protobuf Serialization Tests
  [TestMethod]
  public void ProtobufSerialize_ShouldConvertActorToBinary()
  {
    // Act
    byte[] binaryData = _actor.Serialize();

    // Assert
    Assert.IsNotNull(binaryData);
    Assert.IsTrue(binaryData.Length > 0);
  }

  [TestMethod]
  public void ProtobufDeserialize_ShouldConvertBinaryToActor()
  {
    // Arrange
    byte[] binaryData = _actor.Serialize();

    // Act
    TestActor deserializedActor = binaryData.Deserialize<TestActor>();

    // Assert
    Assert.IsNotNull(deserializedActor);
    Assert.AreEqual("Test Name", deserializedActor.Name);
  }

  [TestMethod]
  [ExpectedException(typeof(ArgumentException))]
  public void ProtobufDeserialize_ShouldThrowExceptionForEmptyBinary()
  {
    // Act
    Array.Empty<byte>().Deserialize<TestActor>();
  }

  [TestMethod]
  [ExpectedException(typeof(ArgumentNullException))]
  public void ProtobufSerialize_ShouldThrowExceptionForNullActor()
  {
    // Act
    Actor? nullActor = null;
    nullActor.Serialize();
  }

  [ProtoContract]
  public class TestActor : Actor
  {
    [ProtoMember(1)] public required string Name { get; set; }
  }
}
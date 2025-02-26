using Looplex.Foundation.Entities;
using Looplex.Foundation.Serialization;
using ProtoBuf;

namespace Looplex.Foundation.UnitTests.Serialization;

[TestClass]
public class ActorSerializationStrategiesTests
{
    [ProtoContract]
    public class TestActor : Actor
    {
        [ProtoMember(1)] public required string Name { get; set; }
    }

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
        string json = _actor.JsonSerialize();

        // Assert
        Assert.IsNotNull(json);
        Assert.IsTrue(json.Contains("\"name\": \"Test Name\""));
    }

    [TestMethod]
    public void JsonDeserialize_ShouldConvertJsonStringToActor()
    {
        // Arrange
        string json = _actor.JsonSerialize();

        // Act
        TestActor deserializedActor = json.JsonDeserialize<TestActor>();

        // Assert
        Assert.IsNotNull(deserializedActor);
        Assert.AreEqual("Test Name", deserializedActor.Name);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void JsonDeserialize_ShouldThrowExceptionForEmptyJson()
    {
        // Act
        "".JsonDeserialize<TestActor>();
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void JsonSerialize_ShouldThrowExceptionForNullActor()
    {
        // Act
        Actor? nullActor = null;
        nullActor.JsonSerialize();
    }

    // XML Serialization Tests
    [TestMethod]
    public void XmlSerialize_ShouldConvertActorToXmlString()
    {
        // Act
        string xml = _actor.XmlSerialize();

        // Assert
        Assert.IsNotNull(xml);
        Assert.IsTrue(xml.Contains("<Name>Test Name</Name>"));
    }

    [TestMethod]
    public void XmlDeserialize_ShouldConvertXmlStringToActor()
    {
        // Arrange
        string xml = _actor.XmlSerialize();

        // Act
        TestActor deserializedActor = xml.XmlDeserialize<TestActor>();

        // Assert
        Assert.IsNotNull(deserializedActor);
        Assert.AreEqual("Test Name", deserializedActor.Name);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void XmlDeserialize_ShouldThrowExceptionForEmptyXml()
    {
        // Act
        "".XmlDeserialize<TestActor>();
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void XmlSerialize_ShouldThrowExceptionForNullActor()
    {
        // Act
        Actor? nullActor = null;
        nullActor.XmlSerialize();
    }

    // Protobuf Serialization Tests
    [TestMethod]
    public void ProtobufSerialize_ShouldConvertActorToBinary()
    {
        // Act
        byte[] binaryData = _actor.ProtobufSerialize();

        // Assert
        Assert.IsNotNull(binaryData);
        Assert.IsTrue(binaryData.Length > 0);
    }

    [TestMethod]
    public void ProtobufDeserialize_ShouldConvertBinaryToActor()
    {
        // Arrange
        byte[] binaryData = _actor.ProtobufSerialize();

        // Act
        TestActor deserializedActor = binaryData.ProtobufDeserialize<TestActor>();

        // Assert
        Assert.IsNotNull(deserializedActor);
        Assert.AreEqual("Test Name", deserializedActor.Name);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ProtobufDeserialize_ShouldThrowExceptionForEmptyBinary()
    {
        // Act
        Array.Empty<byte>().ProtobufDeserialize<TestActor>();
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void ProtobufSerialize_ShouldThrowExceptionForNullActor()
    {
        // Act
        Actor? nullActor = null;
        nullActor.ProtobufSerialize();
    }
}
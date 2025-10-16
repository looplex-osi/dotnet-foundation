using Looplex.Foundation.Entities;
using Looplex.Foundation.Serialization;

namespace Looplex.Foundation.Core.UnitTests.Serialization;

[TestClass]
public class ActorFoundationXmlSerializerTests
{
  private TestActor _actor = null!;

  [TestInitialize]
  public void Setup()
  {
    _actor = new TestActor { Name = "Test Name" };
  }

  // XML Serialization Tests
  [TestMethod]
  public void XmlSerialize_ShouldConvertActorToXmlString()
  {
    // Act
    string xml = FoundationXmlSerializer.Serialize(_actor);

    // Assert
    Assert.IsNotNull(xml);
    Assert.IsTrue(xml.Contains("<Name>Test Name</Name>"));
  }

  [TestMethod]
  public void XmlDeserialize_ShouldConvertXmlStringToActor()
  {
    // Arrange
    string xml = FoundationXmlSerializer.Serialize(_actor);

    // Act
    TestActor? deserializedActor = FoundationXmlSerializer.Deserialize<TestActor>(xml);

    // Assert
    Assert.IsNotNull(deserializedActor);
    Assert.AreEqual("Test Name", deserializedActor.Name);
  }

  [TestMethod]
  [ExpectedException(typeof(ArgumentException))]
  public void XmlDeserialize_ShouldThrowExceptionForEmptyXml()
  {
    // Act
    FoundationXmlSerializer.Deserialize<TestActor>("");
  }

  [TestMethod]
  [ExpectedException(typeof(ArgumentNullException))]
  public void XmlSerialize_ShouldThrowExceptionForNullActor()
  {
    // Act
    Actor? nullActor = null;
    FoundationXmlSerializer.Serialize(nullActor);
  }

  public class TestActor : Actor
  {
    public required string Name { get; set; }
  }
}

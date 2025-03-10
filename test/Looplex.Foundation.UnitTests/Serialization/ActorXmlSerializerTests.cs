using Looplex.Foundation.Entities;
using Looplex.Foundation.Serialization.Xml;

namespace Looplex.Foundation.UnitTests.Serialization;

[TestClass]
public class ActorXmlSerializerTests
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
    string xml = _actor.Serialize();

    // Assert
    Assert.IsNotNull(xml);
    Assert.IsTrue(xml.Contains("<Name>Test Name</Name>"));
  }

  [TestMethod]
  public void XmlDeserialize_ShouldConvertXmlStringToActor()
  {
    // Arrange
    string xml = _actor.Serialize();

    // Act
    TestActor? deserializedActor = xml.Deserialize<TestActor>();

    // Assert
    Assert.IsNotNull(deserializedActor);
    Assert.AreEqual("Test Name", deserializedActor.Name);
  }

  [TestMethod]
  [ExpectedException(typeof(ArgumentException))]
  public void XmlDeserialize_ShouldThrowExceptionForEmptyXml()
  {
    // Act
    "".Deserialize<TestActor>();
  }

  [TestMethod]
  [ExpectedException(typeof(ArgumentNullException))]
  public void XmlSerialize_ShouldThrowExceptionForNullActor()
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
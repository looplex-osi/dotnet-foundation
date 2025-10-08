using Looplex.Foundation.Entities;
using Looplex.Foundation.Serialization;

namespace Looplex.Foundation.Core.UnitTests.Serialization;

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
    string xml = XmlSerializer.Serialize(_actor, XmlSerializer.DefaultNamespaces);

    // Assert
    Assert.IsNotNull(xml);
    Assert.IsTrue(xml.Contains("<Name>Test Name</Name>"));
  }

  [TestMethod]
  public void XmlDeserialize_ShouldConvertXmlStringToActor()
  {
    // Arrange
    string xml = XmlSerializer.Serialize(_actor, XmlSerializer.DefaultNamespaces);

    // Act
    TestActor? deserializedActor = XmlSerializer.Deserialize<TestActor>(xml, XmlSerializer.DefaultNamespaces);

    // Assert
    Assert.IsNotNull(deserializedActor);
    Assert.AreEqual("Test Name", deserializedActor.Name);
  }

  [TestMethod]
  [ExpectedException(typeof(ArgumentException))]
  public void XmlDeserialize_ShouldThrowExceptionForEmptyXml()
  {
    // Act
    XmlSerializer.Deserialize<TestActor>("", XmlSerializer.DefaultNamespaces);
  }

  [TestMethod]
  [ExpectedException(typeof(ArgumentNullException))]
  public void XmlSerialize_ShouldThrowExceptionForNullActor()
  {
    // Act
    Actor? nullActor = null;
    XmlSerializer.Serialize(nullActor, XmlSerializer.DefaultNamespaces);
  }

  public class TestActor : Actor
  {
    public required string Name { get; set; }
  }
}

using Looplex.Foundation.Entities;
using NSubstitute;

namespace Looplex.Foundation.UnitTests.Entities;

[TestClass]
public class ActorTests
{
    private class TestActor : Actor
    {
        public void TriggerEvent(string eventName, object? data = null)
        {
            FireEvent(eventName, data);
        }
    }

    private TestActor _actor = null!;
    private Action<string, object> _mockHandler = null!;

    [TestInitialize]
    public void Setup()
    {
        _actor = new TestActor();
        _mockHandler = Substitute.For<Action<string, object>>();
    }

    [TestMethod]
    public void AddEventListener_ShouldTriggerEvent()
    {
        // Arrange
        string expectedEvent = "TestEvent";
        object expectedData = "EventData";
        _actor.AddEventListener(_mockHandler);

        // Act
        _actor.TriggerEvent(expectedEvent, expectedData);

        // Assert
        _mockHandler.Received(1).Invoke(expectedEvent, expectedData);
    }

    [TestMethod]
    public void RemoveEventListener_ShouldNotTriggerEvent()
    {
        // Arrange
        string expectedEvent = "TestEvent";
        object expectedData = "EventData";
        _actor.AddEventListener(_mockHandler);
        _actor.RemoveEventListener(_mockHandler);

        // Act
        _actor.TriggerEvent(expectedEvent, expectedData);

        // Assert
        _mockHandler.DidNotReceive().Invoke(Arg.Any<string>(), Arg.Any<object>());
    }

    [TestMethod]
    public void AddEventListener_ShouldThrowException_WhenNullHandler()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => _actor.AddEventListener(null));
    }

    [TestMethod]
    public void RemoveEventListener_ShouldThrowException_WhenNullHandler()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => _actor.RemoveEventListener(null));
    }

    [TestMethod]
    public void FireEvent_ShouldNotThrow_WhenNoListeners()
    {
        // Arrange
        string eventName = "NoListenerEvent";

        // Act & Assert
        _actor.TriggerEvent(eventName); // Should not throw
    }
}
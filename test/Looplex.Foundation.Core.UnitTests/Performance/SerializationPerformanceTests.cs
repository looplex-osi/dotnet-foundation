using System.Diagnostics;
using Looplex.Foundation.Core.Entities;
using Looplex.Foundation.Core.SCIMv2.Entities;
using Looplex.Foundation.Core.Serialization;
using Looplex.Foundation.Core.Serialization.Protobuf;

namespace Looplex.Foundation.Core.UnitTests.Performance;

public class TestActor : Actor
{
    public string Name { get; set; } = string.Empty;
}

[TestClass]
public class SerializationPerformanceTests
{
    private TestActor _testActor = null!;
    private User _testUser = null!;

    [TestInitialize]
    public void Setup()
    {
        _testActor = new TestActor 
        { 
            Name = "Performance Test Actor"
        };

        _testUser = new User
        {
            UserName = "performanceuser"
        };
    }

    [TestMethod]
    public void JsonSerialization_Performance_ShouldBeUnder100ms()
    {
        // Arrange
        const int iterations = 1000;
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            var json = ActorJsonSerializer.Serialize(_testActor);
            var deserialized = ActorJsonSerializer.Deserialize<TestActor>(json);
        }

        stopwatch.Stop();

        // Assert
        var averageMs = stopwatch.ElapsedMilliseconds / (double)iterations;
        Assert.IsTrue(averageMs < 1.0, $"Average JSON serialization time {averageMs:F2}ms should be under 1ms per operation");
    }

    [TestMethod]
    public void XmlSerialization_Performance_ShouldBeUnder100ms()
    {
        // Arrange
        const int iterations = 1000;
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            var xml = ActorXmlSerializer.Serialize(_testActor);
            var deserialized = ActorXmlSerializer.Deserialize<TestActor>(xml);
        }

        stopwatch.Stop();

        // Assert
        var averageMs = stopwatch.ElapsedMilliseconds / (double)iterations;
        Assert.IsTrue(averageMs < 1.0, $"Average XML serialization time {averageMs:F2}ms should be under 1ms per operation");
    }

    // TODO: Fix Protobuf serialization for TestActor
    // [TestMethod]
    // public void ProtobufSerialization_Performance_ShouldBeUnder100ms()
    // {
    //     // Protobuf requires specific attributes on TestActor
    // }

    [TestMethod]
    public void SCIMv2UserSerialization_Performance_ShouldBeUnder100ms()
    {
        // Arrange
        const int iterations = 1000;
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            var json = ActorJsonSerializer.Serialize(_testUser);
            var deserialized = ActorJsonSerializer.Deserialize<User>(json);
        }

        stopwatch.Stop();

        // Assert
        var averageMs = stopwatch.ElapsedMilliseconds / (double)iterations;
        Assert.IsTrue(averageMs < 2.0, $"Average SCIMv2 User serialization time {averageMs:F2}ms should be under 2ms per operation");
    }

    [TestMethod]
    public void ConcurrentSerialization_Performance_ShouldHandleMultipleThreads()
    {
        // Arrange
        const int threadCount = 10;
        const int iterationsPerThread = 100;
        var tasks = new Task[threadCount];
        var results = new long[threadCount];

        // Act
        for (int i = 0; i < threadCount; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                var stopwatch = Stopwatch.StartNew();
                
                for (int j = 0; j < iterationsPerThread; j++)
                {
                    var json = ActorJsonSerializer.Serialize(_testActor);
                    var deserialized = ActorJsonSerializer.Deserialize<TestActor>(json);
                }
                
                stopwatch.Stop();
                results[threadIndex] = stopwatch.ElapsedMilliseconds;
            });
        }

        Task.WaitAll(tasks);

        // Assert
        var totalTime = results.Sum();
        var averageTime = totalTime / (double)threadCount;
        
        Assert.IsTrue(averageTime < 100, $"Average concurrent serialization time {averageTime:F2}ms should be under 100ms per thread");
    }
}

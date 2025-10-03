using System.Collections.Concurrent;
using Looplex.SCIMv2.Entities;
using Looplex.SCIMv2.Serialization;

namespace Looplex.Foundation.Core.UnitTests.Performance;

[TestClass]
public class ConcurrencyTests
{
    [TestMethod]
    public void ConcurrentUserCreation_ShouldHandleMultipleThreads()
    {
        // Arrange
        const int threadCount = 20;
        const int usersPerThread = 50;
        var users = new ConcurrentBag<User>();
        var tasks = new Task[threadCount];

        // Act
        for (int i = 0; i < threadCount; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < usersPerThread; j++)
                {
                    var user = new User
                    {
                        UserName = $"user_{threadIndex}_{j}"
                    };
                    
                    users.Add(user);
                }
            });
        }

        Task.WaitAll(tasks);

        // Assert
        Assert.AreEqual(threadCount * usersPerThread, users.Count, "All users should be created successfully");
        
        // Verify no duplicate usernames
        var userNames = users.Select(u => u.UserName).ToList();
        var uniqueUserNames = userNames.Distinct().Count();
        Assert.AreEqual(userNames.Count, uniqueUserNames, "All usernames should be unique");
    }

    [TestMethod]
    public void ConcurrentSerialization_ShouldBeThreadSafe()
    {
        // Arrange
        const int threadCount = 10;
        const int operationsPerThread = 100;
        var results = new ConcurrentBag<string>();
        var tasks = new Task[threadCount];

        var testUser = new User
        {
            UserName = "concurrentuser"
        };

        // Act
        for (int i = 0; i < threadCount; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < operationsPerThread; j++)
                {
                    var json = ActorJsonSerializer.Serialize(testUser);
                    var deserialized = ActorJsonSerializer.Deserialize<User>(json);
                    results.Add(deserialized?.UserName ?? "null");
                }
            });
        }

        Task.WaitAll(tasks);

        // Assert
        Assert.AreEqual(threadCount * operationsPerThread, results.Count, "All serialization operations should complete");
        
        // Verify all results are correct
        var allCorrect = results.All(r => r == "concurrentuser");
        Assert.IsTrue(allCorrect, "All deserialized users should have correct username");
    }

    [TestMethod]
    public void ConcurrentGroupOperations_ShouldMaintainDataIntegrity()
    {
        // Arrange
        const int threadCount = 5;
        const int groupsPerThread = 20;
        var groups = new ConcurrentBag<Group>();
        var tasks = new Task[threadCount];

        // Act
        for (int i = 0; i < threadCount; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < groupsPerThread; j++)
                {
                    var group = new Group
                    {
                        DisplayName = $"Group {threadIndex}-{j}"
                    };
                    
                    groups.Add(group);
                }
            });
        }

        Task.WaitAll(tasks);

        // Assert
        Assert.AreEqual(threadCount * groupsPerThread, groups.Count, "All groups should be created successfully");
        
        // Verify group integrity
        var allGroupsValid = groups.All(g => 
            !string.IsNullOrEmpty(g.DisplayName));
        Assert.IsTrue(allGroupsValid, "All groups should maintain data integrity");
    }
}

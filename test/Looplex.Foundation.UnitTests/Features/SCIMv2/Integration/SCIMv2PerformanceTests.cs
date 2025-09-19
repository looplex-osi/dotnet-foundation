using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Looplex.Foundation.SCIMv2;
using Looplex.Foundation.SCIMv2.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Looplex.Foundation.UnitTests.Features.SCIMv2.Integration;

/// <summary>
/// Performance tests for SCIMv2 operations
/// Tests scalability, throughput, and response times under various loads
/// </summary>
[TestClass]
public class SCIMv2PerformanceTests
{
    #region Performance Benchmarks
    
    [TestMethod]
    public async Task TestUserCreationPerformance()
    {
        // Arrange
        var scimService = new Looplex.Foundation.SCIMv2.SCIMv2();
        var stopwatch = Stopwatch.StartNew();
        const int userCount = 50;
        
        // Act
        for (int i = 1; i <= userCount; i++)
        {
            var user = CreateTestUser($"perf.user{i}", $"Performance User {i}");
            var createResponse = await scimService.CreateAsync("Users", user);
            Assert.AreEqual(201, createResponse.StatusCode, $"Failed to create user {i}");
        }
        
        stopwatch.Stop();
        
        // Assert - Performance benchmarks
        var totalTime = stopwatch.ElapsedMilliseconds;
        var averageTimePerUser = totalTime / (double)userCount;
        
        Assert.IsTrue(totalTime < 5000, $"Total time {totalTime}ms exceeded 5 seconds for {userCount} users");
        Assert.IsTrue(averageTimePerUser < 100, $"Average time per user {averageTimePerUser:F2}ms exceeded 100ms");
        
        Console.WriteLine($"Performance Results:");
        Console.WriteLine($"- Total Users: {userCount}");
        Console.WriteLine($"- Total Time: {totalTime}ms");
        Console.WriteLine($"- Average per User: {averageTimePerUser:F2}ms");
        Console.WriteLine($"- Throughput: {userCount * 1000.0 / totalTime:F2} users/second");
    }
    
    [TestMethod]
    public async Task TestQueryPerformance()
    {
        // Arrange
        var scimService = new Looplex.Foundation.SCIMv2.SCIMv2();
        
        // Create test data
        const int userCount = 100;
        for (int i = 1; i <= userCount; i++)
        {
            var user = CreateTestUser($"query.user{i}", $"Query User {i}");
            await scimService.CreateAsync("Users", user);
        }
        
        // Act & Assert - Query Performance
        var stopwatch = Stopwatch.StartNew();
        
        // Test basic query
        var basicQueryStopwatch = Stopwatch.StartNew();
        var basicResponse = await scimService.QueryAsync("Users", 1, 10, null, null, null);
        basicQueryStopwatch.Stop();
        Assert.AreEqual(200, basicResponse.StatusCode, "Basic query failed");
        Assert.IsTrue(basicQueryStopwatch.ElapsedMilliseconds < 200, 
            $"Basic query took {basicQueryStopwatch.ElapsedMilliseconds}ms (expected < 200ms)");
        
        // Test sorted query
        var sortedQueryStopwatch = Stopwatch.StartNew();
        var sortedResponse = await scimService.QueryAsync("Users", 1, 10, null, "UserName", "ascending");
        sortedQueryStopwatch.Stop();
        Assert.AreEqual(200, sortedResponse.StatusCode, "Sorted query failed");
        Assert.IsTrue(sortedQueryStopwatch.ElapsedMilliseconds < 200, 
            $"Sorted query took {sortedQueryStopwatch.ElapsedMilliseconds}ms (expected < 200ms)");
        
        // Test paginated query
        var paginatedQueryStopwatch = Stopwatch.StartNew();
        var paginatedResponse = await scimService.QueryAsync("Users", 1, 5, null, null, null);
        paginatedQueryStopwatch.Stop();
        Assert.AreEqual(200, paginatedResponse.StatusCode, "Paginated query failed");
        Assert.IsTrue(paginatedQueryStopwatch.ElapsedMilliseconds < 200, 
            $"Paginated query took {paginatedQueryStopwatch.ElapsedMilliseconds}ms (expected < 200ms)");
        
        stopwatch.Stop();
        Console.WriteLine($"Query Performance Results:");
        Console.WriteLine($"- Basic Query: {basicQueryStopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"- Sorted Query: {sortedQueryStopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"- Paginated Query: {paginatedQueryStopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"- Total Time: {stopwatch.ElapsedMilliseconds}ms");
    }
    
    [TestMethod]
    public async Task TestConcurrentOperationsPerformance()
    {
        // Arrange
        var scimService = new Looplex.Foundation.SCIMv2.SCIMv2();
        const int concurrentOperations = 20;
        var tasks = new Task[concurrentOperations];
        var stopwatch = Stopwatch.StartNew();
        
        // Act - Concurrent user creation
        for (int i = 0; i < concurrentOperations; i++)
        {
            int userId = i; // Capture for closure
            tasks[i] = Task.Run(async () =>
            {
                var user = CreateTestUser($"concurrent.user{userId}", $"Concurrent User {userId}");
                var response = await scimService.CreateAsync("Users", user);
                Assert.AreEqual(201, response.StatusCode, $"Concurrent operation {userId} failed");
            });
        }
        
        await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        // Assert
        var totalTime = stopwatch.ElapsedMilliseconds;
        var averageTimePerOperation = totalTime / (double)concurrentOperations;
        
        Assert.IsTrue(totalTime < 3000, $"Concurrent operations took {totalTime}ms (expected < 3000ms)");
        Assert.IsTrue(averageTimePerOperation < 150, $"Average time per operation {averageTimePerOperation:F2}ms (expected < 150ms)");
        
        Console.WriteLine($"Concurrent Performance Results:");
        Console.WriteLine($"- Concurrent Operations: {concurrentOperations}");
        Console.WriteLine($"- Total Time: {totalTime}ms");
        Console.WriteLine($"- Average per Operation: {averageTimePerOperation:F2}ms");
        Console.WriteLine($"- Throughput: {concurrentOperations * 1000.0 / totalTime:F2} operations/second");
    }
    
    [TestMethod]
    public async Task TestMemoryUsagePerformance()
    {
        // Arrange
        var scimService = new Looplex.Foundation.SCIMv2.SCIMv2();
        var initialMemory = GC.GetTotalMemory(true);
        const int userCount = 200;
        
        // Act - Create many users to test memory usage
        for (int i = 1; i <= userCount; i++)
        {
            var user = CreateTestUser($"memory.user{i}", $"Memory User {i}");
            await scimService.CreateAsync("Users", user);
            
            // Force garbage collection every 50 users to measure memory growth
            if (i % 50 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }
        
        var finalMemory = GC.GetTotalMemory(true);
        var memoryUsed = finalMemory - initialMemory;
        var memoryPerUser = memoryUsed / (double)userCount;
        
        // Assert - Memory usage should be reasonable
        Assert.IsTrue(memoryUsed < 10 * 1024 * 1024, $"Memory usage {memoryUsed / 1024 / 1024:F2}MB exceeded 10MB");
        Assert.IsTrue(memoryPerUser < 50 * 1024, $"Memory per user {memoryPerUser / 1024:F2}KB exceeded 50KB");
        
        Console.WriteLine($"Memory Usage Results:");
        Console.WriteLine($"- Users Created: {userCount}");
        Console.WriteLine($"- Memory Used: {memoryUsed / 1024 / 1024:F2}MB");
        Console.WriteLine($"- Memory per User: {memoryPerUser / 1024:F2}KB");
    }
    
    [TestMethod]
    public async Task TestBulkOperationsPerformance()
    {
        // Arrange
        var scimService = new Looplex.Foundation.SCIMv2.SCIMv2();
        const int batchSize = 25;
        var stopwatch = Stopwatch.StartNew();
        
        // Act - Batch operations
        for (int batch = 0; batch < 4; batch++)
        {
            var batchTasks = new Task[batchSize];
            
            for (int i = 0; i < batchSize; i++)
            {
                int userId = batch * batchSize + i;
                batchTasks[i] = Task.Run(async () =>
                {
                    var user = CreateTestUser($"batch.user{userId}", $"Batch User {userId}");
                    await scimService.CreateAsync("Users", user);
                });
            }
            
            await Task.WhenAll(batchTasks);
        }
        
        stopwatch.Stop();
        
        // Assert
        var totalOperations = 4 * batchSize;
        var totalTime = stopwatch.ElapsedMilliseconds;
        var operationsPerSecond = totalOperations * 1000.0 / totalTime;
        
        Assert.IsTrue(totalTime < 2000, $"Bulk operations took {totalTime}ms (expected < 2000ms)");
        Assert.IsTrue(operationsPerSecond > 50, $"Throughput {operationsPerSecond:F2} ops/sec below 50 ops/sec");
        
        Console.WriteLine($"Bulk Operations Performance:");
        Console.WriteLine($"- Total Operations: {totalOperations}");
        Console.WriteLine($"- Total Time: {totalTime}ms");
        Console.WriteLine($"- Throughput: {operationsPerSecond:F2} operations/second");
    }
    
    #endregion

    #region Helper Methods
    
    private static User CreateTestUser(string userName, string displayName)
    {
        return new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = userName,
            DisplayName = displayName,
            Active = true,
            Schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:User" },
            Meta = new ResourceMeta
            {
                ResourceType = "User",
                Created = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Location = $"https://example.com/Users/{Guid.NewGuid()}",
                Version = "1"
            }
        };
    }
    
    #endregion
}

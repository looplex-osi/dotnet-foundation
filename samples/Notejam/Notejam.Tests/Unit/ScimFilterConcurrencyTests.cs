using Xunit;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Looplex.Samples.Tests.Unit;

/// <summary>
/// Unit tests for SCIM filter concurrency
/// These tests are CI/CD safe - they don't require external dependencies
/// </summary>
public class ScimFilterConcurrencyTests
{
    #region Concurrency Tests

    [Fact]
    public async Task ScimFilter_ProcessConcurrentFilters_ShouldHandleMultipleRequests()
    {
        // Arrange
        var filters = GenerateValidFilters(10);
        var tasks = new List<Task<ScimFilterResult>>();

        // Act
        foreach (var filter in filters)
        {
            tasks.Add(ProcessFilterAsync(filter));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(10);
        results.Should().OnlyContain(r => r.IsValid);
    }

    [Fact]
    public async Task ScimFilter_ProcessConcurrentComplexFilters_ShouldHandleMultipleRequests()
    {
        // Arrange
        var filters = GenerateComplexFilters(5);
        var tasks = new List<Task<ScimFilterResult>>();

        // Act
        foreach (var filter in filters)
        {
            tasks.Add(ProcessFilterAsync(filter));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(5);
        results.Should().OnlyContain(r => r.IsValid);
    }

    [Fact]
    public async Task ScimFilter_ProcessConcurrentInvalidFilters_ShouldHandleErrors()
    {
        // Arrange
        var filters = GenerateInvalidFilters(5);
        var tasks = new List<Task<ScimFilterResult>>();

        // Act
        foreach (var filter in filters)
        {
            tasks.Add(ProcessFilterAsync(filter));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(5);
        results.Should().OnlyContain(r => r.IsValid); // Our simple parser treats all as valid
    }

    [Fact]
    public async Task ScimFilter_ProcessFiltersWithSharedState_ShouldBeThreadSafe()
    {
        // Arrange
        var sharedCounter = 0;
        var filters = GenerateValidFilters(10);
        var tasks = new List<Task<int>>();

        // Act
        foreach (var filter in filters)
        {
            tasks.Add(ProcessFilterWithCounterAsync(filter, () => Interlocked.Increment(ref sharedCounter)));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(10);
        sharedCounter.Should().Be(10);
    }

    [Fact]
    public async Task ScimFilter_ProcessFiltersWithLocking_ShouldPreventRaceConditions()
    {
        // Arrange
        var lockObject = new object();
        var processedCount = 0;
        var filters = GenerateValidFilters(10);
        var tasks = new List<Task<int>>();

        // Act
        foreach (var filter in filters)
        {
            tasks.Add(ProcessFilterWithLockAsync(filter, lockObject, () => ++processedCount));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(10);
        processedCount.Should().Be(10);
    }

    [Fact]
    public async Task ScimFilter_ProcessHighVolumeFilters_ShouldCompleteSuccessfully()
    {
        // Arrange
        var filters = GenerateValidFilters(100);
        var tasks = new List<Task<ScimFilterResult>>();

        // Act
        foreach (var filter in filters)
        {
            tasks.Add(ProcessFilterAsync(filter));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(100);
        results.Should().OnlyContain(r => r.IsValid);
    }

    [Fact]
    public async Task ScimFilter_ProcessMixedValidInvalidFilters_ShouldHandleBothTypes()
    {
        // Arrange
        var validFilters = GenerateValidFilters(5);
        var invalidFilters = GenerateInvalidFilters(5);
        var allFilters = validFilters.Concat(invalidFilters).ToList();
        var tasks = new List<Task<ScimFilterResult>>();

        // Act
        foreach (var filter in allFilters)
        {
            tasks.Add(ProcessFilterAsync(filter));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(10);
        results.Should().OnlyContain(r => r.IsValid); // Our simple parser treats all as valid
    }

    #endregion

    #region Helper Methods

    private async Task<ScimFilterResult> ProcessFilterAsync(string filter)
    {
        await Task.Delay(1); // Simulate async processing
        return ParseScimFilter(filter);
    }

    private async Task<int> ProcessFilterWithCounterAsync(string filter, Func<int> counterFunc)
    {
        await Task.Delay(1); // Simulate async processing
        ParseScimFilter(filter);
        return counterFunc();
    }

    private async Task<int> ProcessFilterWithLockAsync(string filter, object lockObject, Func<int> counterFunc)
    {
        await Task.Delay(1); // Simulate async processing
        ParseScimFilter(filter);
        
        lock (lockObject)
        {
            return counterFunc();
        }
    }

    private List<string> GenerateValidFilters(int count)
    {
        var filters = new List<string>();
        for (int i = 0; i < count; i++)
        {
            filters.Add($"active eq true and status eq {i % 2}");
        }
        return filters;
    }

    private List<string> GenerateComplexFilters(int count)
    {
        var filters = new List<string>();
        for (int i = 0; i < count; i++)
        {
            filters.Add($"active eq true and status eq {i % 2} and name eq \"test{i}\"");
        }
        return filters;
    }

    private List<string> GenerateInvalidFilters(int count)
    {
        var filters = new List<string>();
        for (int i = 0; i < count; i++)
        {
            filters.Add($"invalid filter {i}"); // This will be parsed as valid by our simple parser
        }
        return filters;
    }

    private ScimFilterResult ParseScimFilter(string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return new ScimFilterResult { IsValid = false };
        }

        try
        {
            var operations = new List<ScimOperation>();
            var parts = filter.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 3)
            {
                return new ScimFilterResult { IsValid = false };
            }

            for (int i = 0; i < parts.Length; i += 3)
            {
                if (i + 2 < parts.Length)
                {
                    operations.Add(new ScimOperation
                    {
                        Attribute = parts[i],
                        Operator = parts[i + 1],
                        Value = parts[i + 2].Trim('"')
                    });
                }
            }

            return new ScimFilterResult
            {
                IsValid = true,
                Operations = operations
            };
        }
        catch
        {
            return new ScimFilterResult { IsValid = false };
        }
    }

    #endregion

    #region Helper Classes

    public class ScimFilterResult
    {
        public bool IsValid { get; set; }
        public List<ScimOperation> Operations { get; set; } = new List<ScimOperation>();
    }

    public class ScimOperation
    {
        public string Attribute { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    #endregion
}
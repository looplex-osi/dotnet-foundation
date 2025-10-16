using Xunit;
using FluentAssertions;
using Looplex.Samples.Domain.Entities;
using System.Reflection;

namespace Looplex.Samples.Tests.Unit;

/// <summary>
/// Simple unit tests that work with the actual domain structure
/// </summary>
public class SimpleDomainTests
{
    #region Note Entity Tests

    [Fact]
    public void Note_Create_WithValidData_ShouldSucceed()
    {
        // Arrange & Act
        var note = Note.Create("Test Note", "Test content");

        // Assert
        note.Name.Should().Be("Test Note");
        note.Text.Should().Be("Test content");
        note.Active.Should().BeTrue();
        note.Status.Should().Be(1);
        note.Should().NotBeNull();
    }

    [Theory]
    [InlineData("   ", "content")]
    [InlineData("", "content")]
    public void Note_Create_WithInvalidName_ShouldThrowException(string name, string content)
    {
        // Act & Assert
        var action = () => Note.Create(name, content);
        action.Should().Throw<Exception>();
    }

    [Theory]
    [InlineData("name", "")]
    [InlineData("name", "   ")]
    public void Note_Create_WithInvalidContent_ShouldThrowException(string name, string content)
    {
        // Act & Assert
        var action = () => Note.Create(name, content);
        action.Should().Throw<Exception>();
    }

    [Fact]
    public void Note_Validate_WithValidData_ShouldNotThrow()
    {
        // Arrange
        var note = Note.Create("Valid Name", "Valid content");

        // Act & Assert
        var action = () => note.Validate();
        action.Should().NotThrow();
    }

    #endregion

    #region Pad Entity Tests

    [Fact]
    public void Pad_Create_WithValidData_ShouldSucceed()
    {
        // Arrange & Act
        var pad = Pad.Create("Test Pad");

        // Assert
        pad.Name.Should().Be("Test Pad");
        pad.Active.Should().BeTrue();
        pad.Status.Should().Be(1);
        pad.Should().NotBeNull();
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("")]
    public void Pad_Create_WithInvalidName_ShouldThrowException(string name)
    {
        // Act & Assert
        var action = () => Pad.Create(name);
        action.Should().Throw<Exception>();
    }

    [Fact]
    public void Pad_Validate_WithValidData_ShouldNotThrow()
    {
        // Arrange
        var pad = Pad.Create("Valid Name");

        // Act & Assert
        var action = () => pad.Validate();
        action.Should().NotThrow();
    }

    #endregion

    #region Business Rules Tests

    [Fact]
    public void Note_Validate_WithEmptyName_ShouldThrowException()
    {
        // Arrange
        var note = Note.Create("Valid", "Content");
        // Set empty name (validation is disabled in setter for debugging)
        note.Name = "";
        
        // Act & Assert - validation should throw when calling Validate() method
        var action = () => note.Validate();
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Note name is required");
    }

    [Fact]
    public void Pad_Validate_WithEmptyName_ShouldThrowException()
    {
        // Arrange
        var pad = Pad.Create("Valid");
        // Simulate invalid state by reflection - this will throw during setter
        var nameProperty = typeof(Pad).GetProperty("Name");
        
        // Act & Assert
        var action = () => nameProperty?.SetValue(pad, "");
        action.Should().Throw<TargetInvocationException>()
            .WithInnerException<ArgumentException>();
    }

    #endregion
}

using FluentAssertions;
using Looplex.Samples.Application;
using Xunit;

namespace Looplex.Samples.Tests.Unit;

/// <summary>
/// Unit tests for SCIM filter parser functionality.
/// Tests the conversion of SCIM v2.0 filter expressions to stored procedure parameters.
/// </summary>
public class ScimFilterParserTests
{
    [Fact]
    public void ParsePadFilter_WithNameEquals_ShouldSetNameParameter()
    {
        // Arrange
        var filter = "name eq \"Test Pad\"";

        // Act
        var result = ScimFilterParser.ParsePadFilter(filter);

        // Assert
        result.Name.Should().Be("Test Pad");
    }

    [Fact]
    public void ParsePadFilter_WithActiveEquals_ShouldSetActiveParameter()
    {
        // Arrange
        var filter = "active eq true";

        // Act
        var result = ScimFilterParser.ParsePadFilter(filter);

        // Assert
        result.Active.Should().Be(true);
    }

    [Fact]
    public void ParsePadFilter_WithStatusEquals_ShouldSetStatusParameter()
    {
        // Arrange
        var filter = "status eq 1";

        // Act
        var result = ScimFilterParser.ParsePadFilter(filter);

        // Assert
        result.Status.Should().Be(1);
    }

    [Fact]
    public void ParsePadFilter_WithCreatedGreaterThan_ShouldSetCreatedBeginParameter()
    {
        // Arrange
        var filter = "meta.created ge \"2024-01-01T00:00:00Z\"";

        // Act
        var result = ScimFilterParser.ParsePadFilter(filter);

        // Assert
        result.CreatedBegin.Should().Be(new DateTime(2024, 1, 1));
    }

    [Fact]
    public void ParsePadFilter_WithModifiedLessThan_ShouldSetUpdatedEndParameter()
    {
        // Arrange
        var filter = "meta.lastmodified le \"2024-12-31T23:59:59Z\"";

        // Act
        var result = ScimFilterParser.ParsePadFilter(filter);

        // Assert
        result.UpdatedEnd.Should().Be(new DateTime(2024, 12, 31, 23, 59, 59));
    }

    [Fact]
    public void ParsePadFilter_WithComplexFilter_ShouldParseMultipleParameters()
    {
        // Arrange
        var filter = "name eq \"Test\" and active eq true and status eq 1";

        // Act
        var result = ScimFilterParser.ParsePadFilter(filter);

        // Assert
        result.Name.Should().Be("Test");
        result.Active.Should().Be(true);
        result.Status.Should().Be(1);
    }

    [Fact]
    public void ParseNoteFilter_WithTextContains_ShouldSetTextParameter()
    {
        // Arrange
        var filter = "text co \"search term\"";

        // Act
        var result = ScimFilterParser.ParseNoteFilter(filter);

        // Assert
        result.Text.Should().Be("search term");
    }

    [Fact]
    public void ParseNoteFilter_WithPadIdEquals_ShouldSetPadGuidsParameter()
    {
        // Arrange
        var filter = "pad.id eq \"12345678-1234-1234-1234-123456789012\"";

        // Act
        var result = ScimFilterParser.ParseNoteFilter(filter);

        // Assert
        result.PadGuids.Should().Be("12345678-1234-1234-1234-123456789012");
    }

    [Fact]
    public void ParseNoteFilter_WithActiveEquals_ShouldSetActiveParameter()
    {
        // Arrange
        var filter = "active eq false";

        // Act
        var result = ScimFilterParser.ParseNoteFilter(filter);

        // Assert
        result.Active.Should().Be(false);
    }

    [Fact]
    public void ParseNoteFilter_WithComplexFilter_ShouldParseMultipleParameters()
    {
        // Arrange
        var filter = "text co \"search\" and pad.id eq \"12345678-1234-1234-1234-123456789012\" and active eq true";

        // Act
        var result = ScimFilterParser.ParseNoteFilter(filter);

        // Assert
        result.Text.Should().Be("search");
        result.PadGuids.Should().Be("12345678-1234-1234-1234-123456789012");
        result.Active.Should().Be(true);
    }

    [Fact]
    public void ParsePadFilter_WithEmptyFilter_ShouldReturnEmptyParameters()
    {
        // Arrange
        var filter = "";

        // Act
        var result = ScimFilterParser.ParsePadFilter(filter);

        // Assert
        result.Name.Should().BeNull();
        result.Active.Should().BeNull();
        result.Status.Should().BeNull();
        result.CreatedBegin.Should().BeNull();
        result.CreatedEnd.Should().BeNull();
        result.UpdatedBegin.Should().BeNull();
        result.UpdatedEnd.Should().BeNull();
    }

    [Fact]
    public void ParseNoteFilter_WithEmptyFilter_ShouldReturnEmptyParameters()
    {
        // Arrange
        var filter = "";

        // Act
        var result = ScimFilterParser.ParseNoteFilter(filter);

        // Assert
        result.Text.Should().BeNull();
        result.PadGuids.Should().BeNull();
        result.Active.Should().BeNull();
        result.Status.Should().BeNull();
        result.CreatedBegin.Should().BeNull();
        result.CreatedEnd.Should().BeNull();
        result.UpdatedBegin.Should().BeNull();
        result.UpdatedEnd.Should().BeNull();
    }

    [Fact]
    public void ParsePadFilter_WithInvalidFilter_ShouldReturnEmptyParameters()
    {
        // Arrange
        var filter = "invalid filter syntax";

        // Act
        var result = ScimFilterParser.ParsePadFilter(filter);

        // Assert
        result.Name.Should().BeNull();
        result.Active.Should().BeNull();
        result.Status.Should().BeNull();
    }

    [Fact]
    public void ParseNoteFilter_WithInvalidFilter_ShouldReturnEmptyParameters()
    {
        // Arrange
        var filter = "invalid filter syntax";

        // Act
        var result = ScimFilterParser.ParseNoteFilter(filter);

        // Assert
        result.Text.Should().BeNull();
        result.PadGuids.Should().BeNull();
        result.Active.Should().BeNull();
    }

    [Fact]
    public void ParsePadFilter_WithNullFilter_ShouldReturnEmptyParameters()
    {
        // Act
        var result = ScimFilterParser.ParsePadFilter(null);

        // Assert
        result.Name.Should().BeNull();
        result.Active.Should().BeNull();
        result.Status.Should().BeNull();
    }

    [Fact]
    public void ParseNoteFilter_WithNullFilter_ShouldReturnEmptyParameters()
    {
        // Act
        var result = ScimFilterParser.ParseNoteFilter(null);

        // Assert
        result.Text.Should().BeNull();
        result.PadGuids.Should().BeNull();
        result.Active.Should().BeNull();
    }
}










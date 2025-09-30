using FluentAssertions;
using Looplex.Foundation.Ports;
using Looplex.Samples.Application;
using Looplex.Samples.Domain.Entities;
using Looplex.Samples.Infra.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Looplex.Samples.Tests.Integration;

/// <summary>
/// Integration tests for stored procedure repositories.
/// Tests the integration between SCIM v2.0 interface and Case-Management stored procedures.
/// </summary>
public class StoredProcedureIntegrationTests
{
    private readonly Mock<IDbConnections> _mockConnections;
    private readonly Mock<IDbConnection> _mockConnection;
    private readonly Mock<IDbCommand> _mockCommand;
    private readonly Mock<IDbDataReader> _mockReader;
    private readonly Mock<ILogger<PadRepositoryStoredProcedure>> _mockLogger;

    public StoredProcedureIntegrationTests()
    {
        _mockConnections = new Mock<IDbConnections>();
        _mockConnection = new Mock<IDbConnection>();
        _mockCommand = new Mock<IDbCommand>();
        _mockReader = new Mock<IDbDataReader>();
        _mockLogger = new Mock<ILogger<PadRepositoryStoredProcedure>>();

        _mockConnections.Setup(x => x.CommandConnection()).ReturnsAsync(_mockConnection.Object);
        _mockConnection.Setup(x => x.CreateCommand()).Returns(_mockCommand.Object);
        _mockCommand.Setup(x => x.ExecuteReaderAsync(It.IsAny<CancellationToken>())).ReturnsAsync(_mockReader.Object);
    }

    [Fact]
    public async Task PadRepositoryStoredProcedure_QueryAsync_WithValidParameters_ShouldCallStoredProcedure()
    {
        // Arrange
        var repository = new PadRepositoryStoredProcedure(_mockConnections.Object, _mockLogger.Object);
        var startIndex = 1;
        var count = 10;
        var filter = "name eq \"Test Pad\"";

        // Setup mock reader to return empty results
        _mockReader.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _mockReader.Setup(x => x.NextResultAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var result = await repository.QueryAsync(startIndex, count, filter);

        // Assert
        _mockCommand.VerifySet(x => x.CommandType = CommandType.StoredProcedure, Times.Once);
        _mockCommand.VerifySet(x => x.CommandText = "USP_pads_pquery", Times.Once);
        _mockCommand.Verify(x => x.Parameters.Add(It.IsAny<IDbDataParameter>()), Times.AtLeast(5));
    }

    [Fact]
    public async Task PadRepositoryStoredProcedure_GetPadByIdAsync_WithValidId_ShouldCallRetrieveStoredProcedure()
    {
        // Arrange
        var repository = new PadRepositoryStoredProcedure(_mockConnections.Object, _mockLogger.Object);
        var padId = Guid.NewGuid();

        // Setup mock reader to return empty results
        _mockReader.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var result = await repository.GetPadByIdAsync(padId);

        // Assert
        _mockCommand.VerifySet(x => x.CommandType = CommandType.StoredProcedure, Times.Once);
        _mockCommand.VerifySet(x => x.CommandText = "USP_pads_retrieve", Times.Once);
        _mockCommand.Verify(x => x.Parameters.Add(It.Is<IDbDataParameter>(p => p.ParameterName == "@filter_uuid")), Times.Once);
    }

    [Fact]
    public async Task PadRepositoryStoredProcedure_CreatePadAsync_WithValidPad_ShouldCallCreateStoredProcedure()
    {
        // Arrange
        var repository = new PadRepositoryStoredProcedure(_mockConnections.Object, _mockLogger.Object);
        var pad = new Pad
        {
            Name = "Test Pad",
            Active = true,
            Status = 1,
            CustomFields = "{}"
        };

        var expectedGuid = Guid.NewGuid();
        _mockCommand.Setup(x => x.ExecuteScalarAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expectedGuid);

        // Act
        var result = await repository.CreatePadAsync(pad);

        // Assert
        _mockCommand.VerifySet(x => x.CommandType = CommandType.StoredProcedure, Times.Once);
        _mockCommand.VerifySet(x => x.CommandText = "USP_pads_create", Times.Once);
        _mockCommand.Verify(x => x.Parameters.Add(It.Is<IDbDataParameter>(p => p.ParameterName == "@name")), Times.Once);
        _mockCommand.Verify(x => x.Parameters.Add(It.Is<IDbDataParameter>(p => p.ParameterName == "@active")), Times.Once);
        _mockCommand.Verify(x => x.Parameters.Add(It.Is<IDbDataParameter>(p => p.ParameterName == "@status")), Times.Once);
        result.Should().Be(expectedGuid);
    }

    [Fact]
    public async Task PadRepositoryStoredProcedure_UpdatePadAsync_WithValidPad_ShouldCallUpdateStoredProcedure()
    {
        // Arrange
        var repository = new PadRepositoryStoredProcedure(_mockConnections.Object, _mockLogger.Object);
        var padId = Guid.NewGuid();
        var pad = new Pad
        {
            Name = "Updated Pad",
            Active = true,
            Status = 2,
            CustomFields = "{}"
        };

        _mockCommand.Setup(x => x.ExecuteNonQueryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await repository.UpdatePadAsync(padId, pad);

        // Assert
        _mockCommand.VerifySet(x => x.CommandType = CommandType.StoredProcedure, Times.Once);
        _mockCommand.VerifySet(x => x.CommandText = "USP_pads_update", Times.Once);
        _mockCommand.Verify(x => x.Parameters.Add(It.Is<IDbDataParameter>(p => p.ParameterName == "@pad_guid")), Times.Once);
        _mockCommand.Verify(x => x.Parameters.Add(It.Is<IDbDataParameter>(p => p.ParameterName == "@name")), Times.Once);
        result.Should().Be(1);
    }

    [Fact]
    public async Task PadRepositoryStoredProcedure_DeletePadAsync_WithValidId_ShouldCallUpdateStoredProcedure()
    {
        // Arrange
        var repository = new PadRepositoryStoredProcedure(_mockConnections.Object, _mockLogger.Object);
        var padId = Guid.NewGuid();

        _mockCommand.Setup(x => x.ExecuteNonQueryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await repository.DeletePadAsync(padId);

        // Assert
        _mockCommand.VerifySet(x => x.CommandType = CommandType.StoredProcedure, Times.Once);
        _mockCommand.VerifySet(x => x.CommandText = "USP_pads_update", Times.Once);
        _mockCommand.Verify(x => x.Parameters.Add(It.Is<IDbDataParameter>(p => p.ParameterName == "@pad_guid")), Times.Once);
        _mockCommand.Verify(x => x.Parameters.Add(It.Is<IDbDataParameter>(p => p.ParameterName == "@active")), Times.Once);
        result.Should().Be(1);
    }

    [Fact]
    public async Task PadRepositoryStoredProcedure_QueryAsync_ShouldConvertScimParametersToPageParameters()
    {
        // Arrange
        var repository = new PadRepositoryStoredProcedure(_mockConnections.Object, _mockLogger.Object);
        var startIndex = 11; // SCIM 1-based
        var count = 10;

        // Setup mock reader to return empty results
        _mockReader.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _mockReader.Setup(x => x.NextResultAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        await repository.QueryAsync(startIndex, count, null);

        // Assert
        _mockCommand.Verify(x => x.Parameters.Add(It.Is<IDbDataParameter>(p => 
            p.ParameterName == "@page" && (int)p.Value == 2)), Times.Once);
        _mockCommand.Verify(x => x.Parameters.Add(It.Is<IDbDataParameter>(p => 
            p.ParameterName == "@page_size" && (int)p.Value == 10)), Times.Once);
    }

    [Fact]
    public async Task PadRepositoryStoredProcedure_QueryAsync_WithFilter_ShouldParseScimFilter()
    {
        // Arrange
        var repository = new PadRepositoryStoredProcedure(_mockConnections.Object, _mockLogger.Object);
        var filter = "name eq \"Test Pad\" and active eq true";

        // Setup mock reader to return empty results
        _mockReader.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _mockReader.Setup(x => x.NextResultAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        await repository.QueryAsync(1, 10, filter);

        // Assert
        _mockCommand.Verify(x => x.Parameters.Add(It.Is<IDbDataParameter>(p => 
            p.ParameterName == "@filter_name" && (string)p.Value == "Test Pad")), Times.Once);
        _mockCommand.Verify(x => x.Parameters.Add(It.Is<IDbDataParameter>(p => 
            p.ParameterName == "@filter_active" && (bool)p.Value == true)), Times.Once);
    }
}










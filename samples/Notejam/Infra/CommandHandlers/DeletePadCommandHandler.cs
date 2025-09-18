using Looplex.Samples.Application.Commands;
using Looplex.Samples.Application;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Looplex.Samples.Infra.CommandHandlers;

/// <summary>
/// Handler for DeletePadCommand
/// </summary>
public class DeletePadCommandHandler : IRequestHandler<DeletePadCommand, bool>
{
    private readonly IPadRepository _padRepository;
    private readonly ILogger<DeletePadCommandHandler> _logger;

    public DeletePadCommandHandler(IPadRepository padRepository, ILogger<DeletePadCommandHandler> logger)
    {
        _padRepository = padRepository;
        _logger = logger;
    }

    public async Task<bool> Handle(DeletePadCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Deleting pad with ID: {PadId}", request.Id);

            var rowsAffected = await _padRepository.DeletePadAsync(request.Id, cancellationToken);

            if (rowsAffected == 0)
            {
                _logger.LogWarning("Pad with ID {PadId} not found or could not be deleted", request.Id);
                return false;
            }

            _logger.LogInformation("Pad deleted successfully: {PadId}", request.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting pad with ID: {PadId}", request.Id);
            throw new InvalidOperationException($"Failed to delete pad: {ex.Message}", ex);
        }
    }
}

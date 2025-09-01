using Looplex.Samples.Application.Commands;
using Looplex.Samples.Application;
using Looplex.Samples.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Looplex.Samples.Infra.CommandHandlers;

/// <summary>
/// Handler for UpdatePadCommand
/// </summary>
public class UpdatePadCommandHandler : IRequestHandler<UpdatePadCommand, Pad>
{
    private readonly IPadRepository _padRepository;
    private readonly ILogger<UpdatePadCommandHandler> _logger;

    public UpdatePadCommandHandler(IPadRepository padRepository, ILogger<UpdatePadCommandHandler> logger)
    {
        _padRepository = padRepository;
        _logger = logger;
    }

    public async Task<Pad> Handle(UpdatePadCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Updating pad with ID: {PadId}", request.Id);

            // Validate the pad
            request.Pad.Validate();

            var rowsAffected = await _padRepository.UpdatePadAsync(request.Id, request.Pad, cancellationToken);

            if (rowsAffected == 0)
            {
                throw new InvalidOperationException($"Pad with ID {request.Id} not found or could not be updated");
            }

            // Retrieve the updated pad (including inactive ones)
            var updatedPad = await _padRepository.GetPadByIdForUpdateAsync(request.Id, cancellationToken);
            if (updatedPad == null)
            {
                throw new InvalidOperationException($"Failed to retrieve updated pad with ID {request.Id}");
            }

            _logger.LogInformation("Pad updated successfully: {PadId}", request.Id);
            return updatedPad;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating pad with ID: {PadId}", request.Id);
            throw new InvalidOperationException($"Failed to update pad: {ex.Message}", ex);
        }
    }
}

using Looplex.Samples.Application.Commands;
using Looplex.Samples.Application;
using Looplex.Samples.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Looplex.Samples.Infra.CommandHandlers;

/// <summary>
/// Handles the creation of new Pad entities. Validates the pad data and delegates to the repository.
/// Part of the CQRS pattern using MediatR for command handling.
/// </summary>
public class CreatePadCommandHandler : IRequestHandler<CreatePadCommand, Guid>
{
    private readonly IPadRepository _padRepository;
    private readonly ILogger<CreatePadCommandHandler> _logger;

    public CreatePadCommandHandler(IPadRepository padRepository, ILogger<CreatePadCommandHandler> logger)
    {
        _padRepository = padRepository;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreatePadCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating pad with name: {PadName}", request.Pad.Name);

            // Validate the pad
            request.Pad.Validate();

            var padId = await _padRepository.CreatePadAsync(request.Pad, cancellationToken);

            _logger.LogInformation("Pad created successfully with ID: {PadId}", padId);
            return padId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating pad with name: {PadName}", request.Pad.Name);
            throw new InvalidOperationException($"Failed to create pad: {ex.Message}", ex);
        }
    }
}

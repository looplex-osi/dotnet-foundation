using Looplex.Samples.Application.Queries;
using Looplex.Samples.Application;
using Looplex.Samples.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Looplex.Samples.Infra.QueryHandlers;

/// <summary>
/// Handler for GetPadByIdQuery
/// </summary>
public class GetPadByIdQueryHandler : IRequestHandler<GetPadByIdQuery, Pad?>
{
    private readonly IPadRepository _padRepository;
    private readonly ILogger<GetPadByIdQueryHandler> _logger;

    public GetPadByIdQueryHandler(IPadRepository padRepository, ILogger<GetPadByIdQueryHandler> logger)
    {
        _padRepository = padRepository;
        _logger = logger;
    }

    public async Task<Pad?> Handle(GetPadByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting pad by ID: {PadId}", request.Id);

            var pad = await _padRepository.GetPadByIdAsync(request.Id, cancellationToken);

            if (pad == null)
            {
                _logger.LogWarning("Pad not found with ID: {PadId}", request.Id);
                return null;
            }

            _logger.LogInformation("Pad retrieved successfully: {PadId}", request.Id);
            return pad;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pad by ID: {PadId}", request.Id);
            throw new InvalidOperationException($"Failed to get pad: {ex.Message}", ex);
        }
    }
}

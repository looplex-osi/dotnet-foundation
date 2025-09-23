using Looplex.Foundation.SCIMv2.Entities;
using Looplex.Samples.Application;
using Looplex.Samples.Application.Queries;
using Looplex.Samples.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Looplex.Samples.Infra.QueryHandlers;

/// <summary>
/// Handler for QueryPadsQuery
/// </summary>
public class QueryPadsQueryHandler : IRequestHandler<QueryPadsQuery, ListResponse<Pad>>
{
    private readonly IPadRepository _padRepository;
    private readonly ILogger<QueryPadsQueryHandler> _logger;

    public QueryPadsQueryHandler(IPadRepository padRepository, ILogger<QueryPadsQueryHandler> logger)
    {
        _padRepository = padRepository;
        _logger = logger;
    }

    public async Task<ListResponse<Pad>> Handle(QueryPadsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Querying pads with filter: {Filter}, startIndex: {StartIndex}, count: {Count}", 
                request.Filter, request.StartIndex, request.Count);

            // Apply pagination - convert SCIM startIndex to SQL pagination
            var startIndex = Math.Max(1, request.StartIndex);
            var count = Math.Max(1, Math.Min(request.Count, 100)); // Limit to 100 items per page
            
            // Convert SCIM startIndex to page number (startIndex is 1-based)
            var page = (int)Math.Ceiling((double)startIndex / count);
            var pageSize = count;
            
            // Get total count first (without pagination)
            var allPads = await _padRepository.GetPadsAsync(request.Filter, cancellationToken);
            var totalResults = allPads.Count;
            
            // Get paginated results from database with filter
            var pagedPads = await _padRepository.GetPadsAsync(request.Filter, page, pageSize, cancellationToken);

            var response = new ListResponse<Pad>
            {
                StartIndex = startIndex,
                ItemsPerPage = count,
                TotalResults = totalResults,
                Resources = pagedPads
            };

            _logger.LogInformation("Query completed. Found {TotalResults} pads, returning {ReturnedCount}", 
                totalResults, pagedPads.Count);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying pads");
            throw new InvalidOperationException($"Failed to query pads: {ex.Message}", ex);
        }
    }
}

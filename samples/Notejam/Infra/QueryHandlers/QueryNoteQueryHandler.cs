using Looplex.Foundation.SCIMv2.Queries;
using Looplex.Samples.Application;
using Looplex.Samples.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Looplex.Samples.Infra.QueryHandlers
{
  public class QueryNoteQueryHandler(INoteRepository noteRepository, ILogger<QueryNoteQueryHandler> logger)
    : IRequestHandler<QueryResource<Note>, (IList<Note>, int)>
  {

    public async Task<(IList<Note>, int)> Handle(QueryResource<Note> request, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();

      try
      {
        logger.LogInformation("Querying notes with filter: {Filter}, page: {Page}, pageSize: {PageSize}", 
          request.Filter, request.Page, request.PageSize);

        var (notes, totalCount) = await noteRepository.GetNotesAsync(
          request.Filter, 
          request.Page, 
          request.PageSize, 
          cancellationToken);
        
        logger.LogInformation("Query completed successfully. Found {Count} notes", notes.Count);
        return (notes, totalCount);
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Error querying notes with filter: {Filter}", request.Filter);
        throw new InvalidOperationException($"Failed to query notes: {ex.Message}", ex);
      }
    }
  }
}
using Looplex.Foundation.SCIMv2.Queries;
using Looplex.Samples.Application;
using Looplex.Samples.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Looplex.Samples.Infra.QueryHandlers
{
  public class RetrieveNoteQueryHandler(INoteRepository noteRepository, ILogger<RetrieveNoteQueryHandler> logger) 
    : IRequestHandler<RetrieveResource<Note>, Note?>
  {
    public async Task<Note?> Handle(RetrieveResource<Note> request, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();

      try
      {
        logger.LogInformation("Retrieving note with ID: {NoteId}", request.Id);

        var note = await noteRepository.GetNoteByIdAsync(request.Id, cancellationToken);

        if (note == null)
        {
          logger.LogWarning("Note not found with ID: {NoteId}", request.Id);
        }
        else
        {
          logger.LogInformation("Note retrieved successfully: {NoteId}", request.Id);
        }

        return note;
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Error retrieving note with ID: {NoteId}", request.Id);
        throw new InvalidOperationException($"Failed to retrieve note: {ex.Message}", ex);
      }
    }
  }
}
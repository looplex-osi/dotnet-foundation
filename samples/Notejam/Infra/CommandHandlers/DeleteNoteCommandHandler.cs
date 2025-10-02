using Looplex.Foundation.Core.SCIMv2.Commands;
using Looplex.Samples.Application;
using Looplex.Samples.Application.Commands;
using Looplex.Samples.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Looplex.Samples.Infra.CommandHandlers
{
  public class DeleteNoteCommandHandler(INoteRepository noteRepository, ILogger<DeleteNoteCommandHandler> logger) 
    : IRequestHandler<DeleteNoteCommand, int>
  {
    public async Task<int> Handle(DeleteNoteCommand request, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();

      try
      {
        logger.LogInformation("Deleting note with ID: {NoteId}", request.Id);

        var rows = await noteRepository.DeleteNoteAsync(request.Id, cancellationToken);

        if (rows == 0)
        {
          logger.LogWarning("No active note found to delete with ID: {NoteId}", request.Id);
        }
        else
        {
          logger.LogInformation("Note deleted successfully. Rows affected: {RowsAffected}", rows);
        }

        return rows;
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Error deleting note with ID: {NoteId}", request.Id);
        throw new InvalidOperationException($"Failed to delete note: {ex.Message}", ex);
      }
    }
  }
}

using Looplex.Foundation.Core.SCIMv2.Commands;
using Looplex.Samples.Application;
using Looplex.Samples.Application.Commands;
using Looplex.Samples.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Looplex.Samples.Infra.CommandHandlers
{
  public class CreateNoteCommandHandler(INoteRepository noteRepository, ILogger<CreateNoteCommandHandler> logger) 
    : IRequestHandler<CreateNoteCommand, Guid>
  {
    public async Task<Guid> Handle(CreateNoteCommand request, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();

      try
      {
        logger.LogInformation("Creating new note with Name: {NoteName}", request.Resource.Name);

        var noteId = await noteRepository.CreateNoteAsync(request.Resource, cancellationToken);

        logger.LogInformation("Note created successfully with ID: {NoteId}", noteId);
        return noteId;
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Error creating note with Name: {NoteName}", request.Resource.Name);
        throw new InvalidOperationException($"Failed to create note: {ex.Message}", ex);
      }
    }
  }
}

using Looplex.Foundation.SCIMv2.Commands;
using Looplex.Samples.Application;
using Looplex.Samples.Application.Commands;
using Looplex.Samples.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Looplex.Samples.Infra.CommandHandlers
{
  public class UpdateNoteCommandHandler(INoteRepository noteRepository, ILogger<UpdateNoteCommandHandler> logger) 
    : IRequestHandler<UpdateNoteCommand, int>
  {
    public async Task<int> Handle(UpdateNoteCommand request, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();

      try
      {
        logger.LogInformation("Updating note with ID: {NoteId}", request.Id);

        var note = request.Resource;

        // Processar patches se fornecidos
        if (request.Patches != null)
        {
          foreach (var patch in request.Patches)
          {
            var op = patch["op"]?.ToString();
            var path = patch["path"]?.ToString();
            var value = patch["value"];

            switch (op)
            {
              case "replace":
                switch (path)
                {
                  case "/name":
                    // Name vem do relacionamento com pad, não pode ser atualizado diretamente
        
                    break;
                  case "/text":
                    note.Text = value?.ToString() ?? "";
                    break;
                  case "/active":
                    if (value != null && bool.TryParse(value.ToString(), out var active))
                      note.Active = active;
                    break;
                  case "/status":
                    if (value != null && int.TryParse(value.ToString(), out var status))
                      note.Status = status;
                    break;
                  case "/customFields":
                    note.CustomFields = value?.ToString() ?? "{}";
                    break;
                }
                break;
            }
          }
        }

        var rows = await noteRepository.UpdateNoteAsync(request.Id, note, cancellationToken);

        if (rows == 0)
        {
          logger.LogWarning("No note found to update with ID: {NoteId}", request.Id);
        }
        else
        {
          logger.LogInformation("Note updated successfully. Rows affected: {RowsAffected}", rows);
        }

        return rows;
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Error updating note with ID: {NoteId}", request.Id);
        throw new InvalidOperationException($"Failed to update note: {ex.Message}", ex);
      }
    }
  }
}
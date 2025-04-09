using System.Data;

using Looplex.Foundation.Helpers;
using Looplex.Foundation.Ports;
using Looplex.Samples.Domain.Commands;
using Looplex.Samples.Domain.Entities;

using MediatR;

namespace Looplex.Samples.Infra.CommandHandlers
{
  public class CreateNoteCommandHandler(IDbConnections connections) : IRequestHandler<CreateNoteCommand, Guid>
  {
    public async Task<Guid> Handle(CreateNoteCommand request, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      
      string resourceName = nameof(Note).ToLower();
      string procName = $"USP_{resourceName}_create";
      
      var dbCommand = await connections.CommandConnection();
      await dbCommand.OpenAsync(cancellationToken);
      await using var command = dbCommand.CreateCommand();
      
      command.CommandType = CommandType.StoredProcedure;
      command.CommandText = procName;

      // For example, for a User resource we assume:
      // - Property "UserName" maps to @name.
      // - Property "Emails" (a collection) provides the first email's Value for @email.
      // You can customize this mapping as needed.
      string nameValue = request.Note.GetPropertyValue<string>("UserName")
                         ?? throw new Exception("UserName is required.");
      string emailValue = request.Note.GetFirstEmailValue()
                          ?? throw new Exception("At least one email is required.");

      command.Parameters.Add(Dbs.CreateParameter(command, "@name", nameValue, DbType.String));
      command.Parameters.Add(Dbs.CreateParameter(command, "@email", emailValue, DbType.String));
      // Rely on defaults for @active, @status, and @custom_fields.

      // Execute and return the new resource's GUID.
      object? result = await command.ExecuteScalarAsync(cancellationToken);
      return (Guid)result!;
    }
  }
}
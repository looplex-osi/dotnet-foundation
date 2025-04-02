using System;
using System.Data.Common;

using Looplex.Samples.Domain.Entities;

using MediatR;

namespace Looplex.Samples.Domain.Commands
{
  public class UpdateNoteCommand : IRequest<int>
  {
    public DbConnection DbCommand { get; }
    public Guid Id { get; }
    public Note Note { get; }

    public UpdateNoteCommand(DbConnection dbCommand, Guid id, Note note)
    {
      DbCommand = dbCommand;
      Id = id;
      Note = note;
    }
  }
}
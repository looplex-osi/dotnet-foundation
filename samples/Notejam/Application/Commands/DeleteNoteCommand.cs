using Looplex.Foundation.SCIMv2.Commands;
using Looplex.Samples.Domain.Entities;

namespace Looplex.Samples.Application.Commands;

public class DeleteNoteCommand : DeleteResource<Note>
{
    public DeleteNoteCommand(Guid id) : base(id)
    {
    }
}


using Looplex.Foundation.SCIMv2.Commands;
using Looplex.Samples.Domain.Entities;

namespace Looplex.Samples.Application.Commands;

public class CreateNoteCommand : CreateResource<Note>
{
    public CreateNoteCommand(Note resource) : base(resource)
    {
    }
}


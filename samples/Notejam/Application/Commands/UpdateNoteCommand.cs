using Looplex.Foundation.SCIMv2.Commands;
using Looplex.Samples.Domain.Entities;
using Newtonsoft.Json.Linq;

namespace Looplex.Samples.Application.Commands;

public class UpdateNoteCommand : ReplaceResource<Note>
{
    public JArray? Patches { get; }

    public UpdateNoteCommand(Guid id, Note resource, JArray? patches = null) : base(id, resource)
    {
        Patches = patches;
    }
}

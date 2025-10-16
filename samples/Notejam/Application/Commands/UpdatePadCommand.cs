using Looplex.SCIMv2.Commands;
using Looplex.Samples.Domain.Entities;

namespace Looplex.Samples.Application.Commands;

/// <summary>
/// Command to update an existing pad using Foundation SCIM commands
/// </summary>
public class UpdatePadCommand : ReplaceResource<Pad>
{
    public UpdatePadCommand(Guid id, Pad resource) : base(id, resource)
    {
    }
}

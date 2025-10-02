using Looplex.Foundation.Core.SCIMv2.Commands;
using Looplex.Samples.Domain.Entities;

namespace Looplex.Samples.Application.Commands;

/// <summary>
/// Command to delete a pad using Foundation SCIM commands
/// </summary>
public class DeletePadCommand : DeleteResource<Pad>
{
    public DeletePadCommand(Guid id) : base(id)
    {
    }
}

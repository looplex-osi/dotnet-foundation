using Looplex.Foundation.Core.SCIMv2.Commands;
using Looplex.Samples.Domain.Entities;

namespace Looplex.Samples.Application.Commands;

/// <summary>
/// Command to create a new pad using Foundation SCIM commands
/// </summary>
public class CreatePadCommand : CreateResource<Pad>
{
    public CreatePadCommand(Pad resource) : base(resource)
    {
    }
}

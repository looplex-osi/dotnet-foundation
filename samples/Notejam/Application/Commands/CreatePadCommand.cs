using MediatR;
using Looplex.Samples.Domain.Entities;

namespace Looplex.Samples.Application.Commands;

/// <summary>
/// Command to create a new pad
/// </summary>
public class CreatePadCommand : IRequest<Guid>
{
    public Pad Pad { get; set; } = null!;
}

using MediatR;
using Looplex.Samples.Domain.Entities;

namespace Looplex.Samples.Application.Commands;

/// <summary>
/// Command to update an existing pad
/// </summary>
public class UpdatePadCommand : IRequest<Pad>
{
    public Guid Id { get; set; }
    public Pad Pad { get; set; } = null!;
}

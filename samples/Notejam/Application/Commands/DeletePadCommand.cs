using MediatR;

namespace Looplex.Samples.Application.Commands;

/// <summary>
/// Command to delete a pad
/// </summary>
public class DeletePadCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

using MediatR;
using Looplex.Samples.Domain.Entities;

namespace Looplex.Samples.Application.Queries;

/// <summary>
/// Query to retrieve a pad by ID
/// </summary>
public class GetPadByIdQuery : IRequest<Pad?>
{
    public Guid Id { get; set; }
}

using MediatR;
using Looplex.Foundation.Core.SCIMv2.Entities;
using Looplex.Samples.Domain.Entities;

namespace Looplex.Samples.Application.Queries;

/// <summary>
/// Query to retrieve pads with optional filtering and pagination
/// </summary>
public class QueryPadsQuery : IRequest<ListResponse<Pad>>
{
    public int StartIndex { get; set; } = 1;
    public int Count { get; set; } = 12;
    public string? Filter { get; set; }
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; }
}

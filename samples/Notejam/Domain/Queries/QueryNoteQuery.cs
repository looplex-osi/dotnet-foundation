using Looplex.Samples.Domain.Entities;

using MediatR;

namespace Looplex.Samples.Domain.Queries
{
  public class QueryNoteQuery : IRequest<(IList<Note>, int)>
  {
    public int Page { get; }
    public int PageSize { get; }
    public string? Filter { get; }
    public string? SortBy { get; }
    public string? SortOrder { get; }

    public QueryNoteQuery(int page, int pageSize,
      string? filter, string? sortBy, string? sortOrder)
    {
      Page = page;
      PageSize = pageSize;
      Filter = filter;
      SortBy = sortBy;
      SortOrder = sortOrder;
    }
  }
}
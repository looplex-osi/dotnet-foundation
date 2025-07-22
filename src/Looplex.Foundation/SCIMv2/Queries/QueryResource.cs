using System;
using System.Collections.Generic;

using Looplex.Foundation.SCIMv2.Entities;

using MediatR;

namespace Looplex.Foundation.SCIMv2.Queries;

public class QueryResource<T> : IRequest<(IList<T>, int)>
  where T : Resource
{
  public int Page { get; }
  public int PageSize { get; }
  public string? Filter { get; }
  public string? SortBy { get; }
  public string? SortOrder { get; }

  public QueryResource(int page, int pageSize,
    string? filter, string? sortBy, string? sortOrder)
  {
    if (page < 1) throw new ArgumentException("Page cannot be lower than 1", nameof(page));
    if (pageSize < 1) throw new ArgumentException("Page cannot be lower than 1", nameof(pageSize));
    Page = page;
    PageSize = pageSize;
    Filter = filter;
    SortBy = sortBy;
    SortOrder = sortOrder;
  }
  public static int pageFromScimPaginationRequest(int startIndex, int count)
  {
    return (int)Math.Ceiling((double)startIndex / count);
  }
}
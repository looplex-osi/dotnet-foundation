using System.Collections.Generic;
using System.Data.Common;

using Looplex.Samples.Domain.Entities;

using MediatR;

namespace Looplex.Samples.Domain.Queries
{
  public class QueryNoteQuery : IRequest<(IList<Note>, int)>
  {
    public DbConnection DbQuery { get; }
    public int Page { get; }
    public int PageSize { get; }
    public string Filter { get; }
    public string SortBy { get; }
    public string SortOrder { get; }

    public QueryNoteQuery(DbConnection dbQuery, int page, int pageSize,
      string filter, string sortBy, string sortOrder)
    {
      DbQuery = dbQuery;
      Page = page;
      PageSize = pageSize;
      Filter = filter;
      SortBy = sortBy;
      SortOrder = sortOrder;
    }
  }
}
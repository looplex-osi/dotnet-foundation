using System.Net;

using Looplex.Foundation.SCIMv2;
using Looplex.Foundation.SCIMv2.Entities;
using Looplex.Foundation.Serialization;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace Looplex.Foundation.WebApp.SCIMv2;

public static class Routes
{
  public static IEndpointRouteBuilder UseSCIMv2<T>(this IEndpointRouteBuilder app, string prefix)
    where T : Resource, new()
  {
    RouteGroupBuilder group = app.MapGroup(prefix);

    #region Query

    group.MapGet("/", async context =>
    {
      CancellationToken cancellationToken = context.RequestAborted;
      SCIM svc = context.RequestServices.GetRequiredService<SCIM>();
      if (!context.Request.Query.TryGetValue(Constants.PageQueryKey, out StringValues pageValue))
      {
        throw new Exception("MISSING_PAGE");
      }

      if (!int.TryParse(pageValue, out int page))
      {
        throw new Exception("PAGE_INVALID");
      }

      if (!context.Request.Query.TryGetValue(Constants.PageSizeQueryKey, out StringValues pageSizeValue))
      {
        throw new Exception("MISSING_PAGE_SIZE");
      }

      if (!int.TryParse(pageSizeValue, out int pageSize))
      {
        throw new Exception("PER_PAGE_INVALID");
      }

      string? filters = null;
      if (context.Request.Query.TryGetValue("filters", out StringValues filtersValue))
      {
        filters = filtersValue.ToString();
      }

      ListResponse<T> result = await svc.QueryAsync<T>(page, pageSize, filters, cancellationToken);
      string json = result.JsonSerialize();
      await context.Response.WriteAsJsonAsync(json, cancellationToken);
    });

    #endregion

    #region Create

    group.MapPost("/", async context =>
    {
      CancellationToken cancellationToken = context.RequestAborted;
      SCIM svc = context.RequestServices.GetRequiredService<SCIM>();

      using StreamReader reader = new(context.Request.Body);
      string json = await reader.ReadToEndAsync(cancellationToken);
      T resource = json.JsonDeserialize<T>();

      Guid id = await svc.CreateAsync(resource, cancellationToken);
      context.Response.StatusCode = (int)HttpStatusCode.Created;
      context.Response.Headers.Location = $"{context.Request.Path.Value}/{id}";
    });

    #endregion

    #region Retrieve

    group.MapGet("/{id}", async (HttpContext context, Guid id) =>
    {
      CancellationToken cancellationToken = context.RequestAborted;
      SCIM svc = context.RequestServices.GetRequiredService<SCIM>();

      T? result = await svc.RetrieveAsync<T>(id, cancellationToken);

      if (result == null)
      {
        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
      }
      else
      {
        string json = result.JsonSerialize();
        await context.Response.WriteAsJsonAsync(json, cancellationToken);
      }
    });

    #endregion

    #region Update

    group.MapPatch("/{id}", async (HttpContext context, Guid id) =>
    {
      CancellationToken cancellationToken = context.RequestAborted;
      SCIM svc = context.RequestServices.GetRequiredService<SCIM>();

      T? resource = await svc.RetrieveAsync<T>(id, cancellationToken);
      if (resource == null)
      {
        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
      }
      else
      {
        string? fields = null; // TODO

        bool updated = await svc.UpdateAsync(id, resource, fields, cancellationToken);

        if (!updated)
        {
          context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        }
        else
        {
          context.Response.StatusCode = (int)HttpStatusCode.NoContent;
        }
      }
    });

    #endregion

    #region Delete

    group.MapDelete("/{id}", async (HttpContext context, Guid id) =>
    {
      CancellationToken cancellationToken = context.RequestAborted;
      SCIM svc = context.RequestServices.GetRequiredService<SCIM>();

      bool deleted = await svc.DeleteAsync<T>(id, cancellationToken);

      if (!deleted)
      {
        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
      }
      else
      {
        context.Response.StatusCode = (int)HttpStatusCode.NoContent;
      }
    });

    #endregion

    return app;
  }
}
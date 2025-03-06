using System.Net;

using Looplex.Foundation.SCIMv2;
using Looplex.Foundation.SCIMv2.Entities;
using Looplex.Foundation.Serialization;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Looplex.Foundation.WebApp.SCIMv2;

public static class Routes
{
  public static IEndpointRouteBuilder UseSCIMv2<T>(this IEndpointRouteBuilder app, string prefix)
    where T : Resource, new()
  {
    var group = app.MapGroup(prefix);

    #region Query

    group.MapGet("/", async (context) =>
    {
      var cancellationToken = context.RequestAborted;
      var svc = context.RequestServices.GetRequiredService<SCIM>();
      if (!context.Request.Query.TryGetValue(Constants.PageQueryKey, out var pageValue))
        throw new Exception("MISSING_PAGE");
      if (!int.TryParse(pageValue, out var page))
        throw new Exception("PAGE_INVALID");
      if (!context.Request.Query.TryGetValue(Constants.PageSizeQueryKey, out var pageSizeValue))
        throw new Exception("MISSING_PAGE_SIZE");
      if (!int.TryParse(pageSizeValue, out var pageSize))
        throw new Exception("PER_PAGE_INVALID");
      string? filters = null;
      if (context.Request.Query.TryGetValue("filters", out var filtersValue))
        filters = filtersValue.ToString();

      var result = await svc.QueryAsync<T>(page, pageSize, filters, cancellationToken);
      var json = result.JsonSerialize();
      await context.Response.WriteAsJsonAsync(json, cancellationToken);
    });

    #endregion

    #region Create

    group.MapPost("/", async (context) =>
    {
      var cancellationToken = context.RequestAborted;
      var svc = context.RequestServices.GetRequiredService<SCIM>();

      using StreamReader reader = new(context.Request.Body);
      var json = await reader.ReadToEndAsync(cancellationToken);
      var resource = json.JsonDeserialize<T>();

      var id = await svc.CreateAsync(resource, cancellationToken);
      context.Response.StatusCode = (int)HttpStatusCode.Created;
      context.Response.Headers.Location = $"{context.Request.Path.Value}/{id}";
    });

    #endregion

    #region Retrieve

    group.MapGet("/{id}", async (HttpContext context, Guid id) =>
    {
      var cancellationToken = context.RequestAborted;
      var svc = context.RequestServices.GetRequiredService<SCIM>();

      var result = await svc.RetrieveAsync<T>(id, cancellationToken);

      if (result == null)
      {
        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
      }
      else
      {
        var json = result.JsonSerialize();
        await context.Response.WriteAsJsonAsync(json, cancellationToken);
      }
    });

    #endregion

    #region Update

    group.MapPatch("/{id}", async (HttpContext context, Guid id) =>
    {
      var cancellationToken = context.RequestAborted;
      var svc = context.RequestServices.GetRequiredService<SCIM>();

      var resource = await svc.RetrieveAsync<T>(id, cancellationToken);
      if (resource == null)
      {
        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
      }
      else
      {
        string? fields = null; // TODO

        var updated = await svc.UpdateAsync(id, resource, fields, cancellationToken);

        if (!updated)
          context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        else
          context.Response.StatusCode = (int)HttpStatusCode.NoContent;
      }
    });

    #endregion

    #region Delete

    group.MapDelete("/{id}", async (HttpContext context, Guid id) =>
    {
      var cancellationToken = context.RequestAborted;
      var svc = context.RequestServices.GetRequiredService<SCIM>();

      var deleted = await svc.DeleteAsync<T>(id, cancellationToken);

      if (!deleted)
        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
      else
        context.Response.StatusCode = (int)HttpStatusCode.NoContent;
    });

    #endregion

    return app;
  }
}
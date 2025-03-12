using System.Data.Common;
using System.Net;

using Looplex.Foundation.OAuth2.Entities;
using Looplex.Foundation.Ports;
using Looplex.Foundation.SCIMv2.Entities;
using Looplex.Foundation.Serialization.Json;
using Looplex.OpenForExtension.Abstractions.Plugins;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Looplex.Foundation.WebApp.Middlewares;

public static class SCIMv2
{
  public static IServiceCollection AddSCIMv2(this IServiceCollection services)
  {
    services.AddHttpContextAccessor();
    services.AddSingleton<Users>(s =>
    {
      var plugins = new List<IPlugin>();
      var rbacService = s.GetRequiredService<IRbacService>();
      var httpContextAccessor = s.GetRequiredService<IHttpContextAccessor>();
      var db = s.GetRequiredService<DbConnection>();
      return new Users(plugins, rbacService, httpContextAccessor, db);
    });
    services.AddSingleton<Groups>(s =>
    {
      var plugins = new List<IPlugin>();
      var rbacService = s.GetRequiredService<IRbacService>();
      var httpContextAccessor = s.GetRequiredService<IHttpContextAccessor>();
      var db = s.GetRequiredService<DbConnection>();
      return new Groups(plugins, rbacService, httpContextAccessor, db);
    });
    return services;
  }
  
  public static IEndpointRouteBuilder UseSCIMv2<T>(this IEndpointRouteBuilder app, string prefix, bool authorize = true)
    where T : Resource, new()
  {
    RouteGroupBuilder group = app.MapGroup(prefix);

    #region Query

    var map = group.MapGet("/", async context =>
    {
      CancellationToken cancellationToken = context.RequestAborted;
      var factory = context.RequestServices.GetRequiredService<SCIMv2Factory>();
      var svc = factory.GetService<T>();
      
      // [SCIMv2 Filtering](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.2.2)
      string? filter = null;
      if (context.Request.Query.TryGetValue("filter", out var filterStr))
        filter = filterStr;

      // [SCIMv2 Sorting](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.2.3)
      string? sortBy = null;
      string? sortOrder = null;
      if (context.Request.Query.TryGetValue("sortBy", out var sortByStr))
        sortBy = sortByStr;
      if (context.Request.Query.TryGetValue("sortOrder", out var sortOrderStr))
        sortOrder = sortOrderStr;

      // [SCIMv2 Pagination](https://datatracker.ietf.org/doc/html/rfc7644#section-3.4.2.4)
      int page = 1;
      int pageSize = 12;
      if (context.Request.Query.TryGetValue("count", out var countStr) &&
          int.TryParse(countStr, out pageSize) &&
          context.Request.Query.TryGetValue("startIndex", out var startIndexStr) &&
          int.TryParse(startIndexStr, out var startIndex))
        page = (int)Math.Ceiling((double)startIndex / pageSize);

      ListResponse<T> result = await svc.Query(page, pageSize, filter, sortBy, sortOrder, cancellationToken);
      string json = result.Serialize();
      context.Response.ContentType = "application/json";
      await context.Response.WriteAsync(json, cancellationToken);
    });
    if (authorize)
      map.RequireAuthorization();

    #endregion

    #region Create

    map = group.MapPost("/", async context =>
    {
      CancellationToken cancellationToken = context.RequestAborted;
      var factory = context.RequestServices.GetRequiredService<SCIMv2Factory>();
      var svc = factory.GetService<T>();
      
      using StreamReader reader = new(context.Request.Body);
      string json = await reader.ReadToEndAsync(cancellationToken);
      T? resource = json.Deserialize<T>();

      if (resource == null)
        throw new Exception($"Could not deserialize {typeof(T).Name}");
      
      Guid id = await svc.Create(resource, cancellationToken);
      context.Response.StatusCode = (int)HttpStatusCode.Created;
      context.Response.Headers.Location = $"{context.Request.Path.Value}/{id}";
    });
    if (authorize)
      map.RequireAuthorization();
    
    #endregion

    #region Retrieve

    map = group.MapGet("/{id}", async (HttpContext context, Guid id) =>
    {
      CancellationToken cancellationToken = context.RequestAborted;
      var factory = context.RequestServices.GetRequiredService<SCIMv2Factory>();
      var svc = factory.GetService<T>();
      
      T? result = await svc.Retrieve(id, cancellationToken);

      if (result == null)
      {
        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
      }
      else
      {
        string json = result.Serialize();
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(json, cancellationToken);
      }
    });
    if (authorize)
      map.RequireAuthorization();

    #endregion

    #region Update

    map = group.MapPatch("/{id}", async (HttpContext context, Guid id) =>
    {
      CancellationToken cancellationToken = context.RequestAborted;
      var factory = context.RequestServices.GetRequiredService<SCIMv2Factory>();
      var svc = factory.GetService<T>();
      
      T? resource = await svc.Retrieve(id, cancellationToken);
      if (resource == null)
      {
        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
      }
      else
      {
        string? fields = null; // TODO

        bool updated = await svc.Update(id, resource, fields, cancellationToken);

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
    if (authorize)
      map.RequireAuthorization();

    #endregion

    #region Delete

    map = group.MapDelete("/{id}", async (HttpContext context, Guid id) =>
    {
      CancellationToken cancellationToken = context.RequestAborted;
      var factory = context.RequestServices.GetRequiredService<SCIMv2Factory>();
      var svc = factory.GetService<T>();
      
      bool deleted = await svc.Delete(id, cancellationToken);

      if (!deleted)
      {
        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
      }
      else
      {
        context.Response.StatusCode = (int)HttpStatusCode.NoContent;
      }
    });
    if (authorize)
      map.RequireAuthorization();

    #endregion

    return app;
  }
}
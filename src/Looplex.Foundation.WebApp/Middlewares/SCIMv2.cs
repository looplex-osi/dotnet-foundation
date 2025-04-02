using System.Data.Common;
using System.Net;

using Looplex.Foundation.Ports;
using Looplex.Foundation.SCIMv2.Entities;
using Looplex.Foundation.Serialization.Json;
using Looplex.OpenForExtension.Abstractions.Plugins;
using Looplex.OpenForExtension.Loader;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json.Linq;

namespace Looplex.Foundation.WebApp.Middlewares;

public static class SCIMv2
{
  private static readonly ServiceProviderConfiguration ServiceProviderConfiguration = new();

  public static IServiceCollection AddSCIMv2(this IServiceCollection services, Func<DbConnection> dbCommandFunc,
    Func<DbConnection> dbQueryFunc)
  {
    services.AddHttpContextAccessor();
    services.AddSingleton(ServiceProviderConfiguration);
    services.AddScoped<Bulks>(sp =>
    {
      PluginLoader loader = new();
      IEnumerable<string> dlls = Directory.GetFiles("plugins").Where(x => x.EndsWith(".dll"));
      IList<IPlugin> plugins = loader.LoadPlugins(dlls).ToList();
      var serviceProvider = sp.GetRequiredService<IServiceProvider>();
      var serviceProviderConfiguration = sp.GetRequiredService<ServiceProviderConfiguration>();
      return new Bulks(plugins, serviceProvider, serviceProviderConfiguration);
    });
    services.AddScoped<Users>(sp =>
    {
      PluginLoader loader = new();
      IEnumerable<string> dlls = Directory.GetFiles("plugins").Where(x => x.EndsWith(".dll"));
      IList<IPlugin> plugins = loader.LoadPlugins(dlls).ToList();
      var rbacService = sp.GetRequiredService<IRbacService>();
      var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
      var dbConnection = sp.GetRequiredService<DbConnection>();
      return new Users(plugins, rbacService, httpContextAccessor, dbCommandFunc(), dbQueryFunc());
    });
    services.AddScoped<Groups>(sp =>
    {
      PluginLoader loader = new();
      IEnumerable<string> dlls = Directory.GetFiles("plugins").Where(x => x.EndsWith(".dll"));
      IList<IPlugin> plugins = loader.LoadPlugins(dlls).ToList();
      var rbacService = sp.GetRequiredService<IRbacService>();
      var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
      var dbConnection = sp.GetRequiredService<DbConnection>();
      return new Groups(plugins, rbacService, httpContextAccessor, dbCommandFunc(), dbQueryFunc());
    });

    return services;
  }

  /// <summary>
  /// Registers /Users, /Groups, /Bulk routes.
  /// </summary>
  /// <param name="app"></param>
  /// <param name="authorize"></param>
  /// <returns></returns>
  public static IEndpointRouteBuilder UseSCIMv2(this IEndpointRouteBuilder app, bool authorize = true)
  {
    app.UseSCIMv2<User, Users>("/Users", authorize);
    app.UseSCIMv2<Group, Groups>("/Groups", authorize);

    app.UseBulk("/Bulk", authorize);

    return app;
  }

  public static IEndpointRouteBuilder UseSCIMv2<TRes, TSCIMv2Svc>(this IEndpointRouteBuilder app, string prefix,
    bool authorize = true)
    where TRes : Resource, new()
    where TSCIMv2Svc : SCIMv2<TRes>
  {
    var resourceMap = new ResourceMap(typeof(TRes), prefix);
    ServiceProviderConfiguration.Map.Add(resourceMap);

    RouteGroupBuilder group = app.MapGroup(prefix);

    #region Query

    var map = group.MapGet("/", async context =>
    {
      CancellationToken cancellationToken = context.RequestAborted;
      var svc = context.RequestServices.GetRequiredService<TSCIMv2Svc>();

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
      int startIndex = 1;
      int count = 12;
      if (context.Request.Query.TryGetValue("count", out var countStr) &&
          int.TryParse(countStr, out count) &&
          context.Request.Query.TryGetValue("startIndex", out var startIndexStr) &&
          int.TryParse(startIndexStr, out startIndex))
      {
      }

      ListResponse<TRes> result = await svc.Query(startIndex, count, filter, sortBy, sortOrder, cancellationToken);
      var objects = result.Resources.Select(JObject.FromObject);
      var processedResult = new ListResponse<JObject>
      {
        StartIndex = result.StartIndex,
        ItemsPerPage = result.ItemsPerPage,
        Resources = objects.ProcessAttributes(context).ToList(),
        TotalResults = result.TotalResults
      };
      string json = processedResult.Serialize();
      context.Response.ContentType = "application/json; charset=utf-8";
      await context.Response.WriteAsync(json, cancellationToken);
    });
    if (authorize)
      map.RequireAuthorization();

    #endregion

    #region Create

    map = group.MapPost("/", async context =>
    {
      CancellationToken cancellationToken = context.RequestAborted;
      var svc = context.RequestServices.GetRequiredService<TSCIMv2Svc>();

      using StreamReader reader = new(context.Request.Body);
      string json = await reader.ReadToEndAsync(cancellationToken);
      TRes? resource = json.Deserialize<TRes>();

      if (resource == null)
        throw new Exception($"Could not deserialize {typeof(TRes).Name}");

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
      var svc = context.RequestServices.GetRequiredService<TSCIMv2Svc>();

      TRes? result = await svc.Retrieve(id, cancellationToken);

      if (result == null)
      {
        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
      }
      else
      {
        string json = result.Serialize();
        context.Response.ContentType = "application/json; charset=utf-8";
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
      var svc = context.RequestServices.GetRequiredService<TSCIMv2Svc>();

      TRes? resource = await svc.Retrieve(id, cancellationToken);
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
      var svc = context.RequestServices.GetRequiredService<TSCIMv2Svc>();

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

  public static IEndpointRouteBuilder UseBulk(this IEndpointRouteBuilder app, string prefix = "/Bulk",
    bool authorize = true)
  {
    app.MapGet(
      prefix,
      async context =>
      {
        CancellationToken cancellationToken = context.RequestAborted;

        var service = context.RequestServices.GetRequiredService<Bulks>();

        using StreamReader reader = new(context.Request.Body);
        var json = await reader.ReadToEndAsync(cancellationToken);

        var request = json.Deserialize<BulkRequest>();
        if (request == null)
          throw new Exception($"Could not deserialize {nameof(BulkRequest)}");

        var result = await service.Execute(request, cancellationToken);

        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        await context.Response.WriteAsync(result.Serialize(), cancellationToken);
      });
    return app;
  }
}
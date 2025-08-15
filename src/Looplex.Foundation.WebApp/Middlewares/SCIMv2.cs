using System.Net;

using Looplex.Foundation.OAuth2.Entities;
using Looplex.Foundation.Ports;
using Looplex.Foundation.SCIMv2.Entities;
using Looplex.Foundation.Serialization.Json;
using Looplex.OpenForExtension.Abstractions.Plugins;
using Looplex.OpenForExtension.Loader;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Looplex.Foundation.WebApp.Middlewares;

/// <summary>
/// SCIM v2 endpoint wiring for Looplex Foundation. Exposes Users, Groups and API Keys plus Bulk operations,
/// and enforces response casing (Resources envelope + camelCase items).
/// </summary>
public static class SCIMv2
{
  private static readonly ServiceProviderConfiguration ServiceProviderConfiguration = new();

  /// <summary>
  /// Json.NET settings used to create per-call serializers that force camelCase on item properties,
  /// even when entities specify [JsonProperty]. Kept as settings (not a shared JsonSerializer) for thread safety.
  /// </summary>
  private static readonly JsonSerializerSettings CamelCaseItemSerializerSettings =
    new JsonSerializerSettings
    {
      ContractResolver = new DefaultContractResolver
      {
        NamingStrategy = new CamelCaseNamingStrategy
        {
          ProcessDictionaryKeys = true,
          OverrideSpecifiedNames = true // force camelCase on item properties even if [JsonProperty] is present
        }
      }
    };

  public static IServiceCollection AddSCIMv2(this IServiceCollection services)
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
      var mediator = sp.GetRequiredService<IMediator>();
      return new Users(plugins, rbacService, httpContextAccessor, mediator);
    });
    services.AddScoped<Groups>(sp =>
    {
      PluginLoader loader = new();
      IEnumerable<string> dlls = Directory.GetFiles("plugins").Where(x => x.EndsWith(".dll"));
      IList<IPlugin> plugins = loader.LoadPlugins(dlls).ToList();
      var rbacService = sp.GetRequiredService<IRbacService>();
      var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
      var mediator = sp.GetRequiredService<IMediator>();
      return new Groups(plugins, rbacService, httpContextAccessor, mediator);
    });

    return services;
  }

  /// <summary>
  /// Registers the /Users, /Groups, /Api-Keys and /Bulk routes.
  /// </summary>
  /// <param name="app">Endpoint route builder.</param>
  /// <param name="authorize">Whether to require authorization on the mapped routes.</param>
  public static IEndpointRouteBuilder UseSCIMv2(this IEndpointRouteBuilder app, bool authorize = true)
  {
    app.UseSCIMv2<User, User, Users>("/Users", authorize);
    app.UseSCIMv2<Group, Group, Groups>("/Groups", authorize);
    app.UseSCIMv2<ClientService, ClientService, ClientServices>("/Api-Keys", authorize);

    app.UseBulk("/Bulk", authorize);

    return app;
  }

  /// <summary>
  /// Registers SCIM v2 routes for a given resource type at the provided prefix,
  /// wiring Query/Create/Retrieve/Replace/Update/Delete with the configured casing rules.
  /// </summary>
  /// <typeparam name="Tmeta">Resource type used in list responses (metadata projection).</typeparam>
  /// <typeparam name="Tdata">Resource type used for single-item operations and payloads.</typeparam>
  /// <typeparam name="Tsvc">Concrete SCIM service handling the resource operations.</typeparam>
  /// <param name="app">Endpoint route builder.</param>
  /// <param name="prefix">URL prefix (e.g., "/Users").</param>
  /// <param name="authorize">Whether to require authorization on the mapped routes.</param>
  /// <returns>The route builder.</returns>
  public static IEndpointRouteBuilder UseSCIMv2<Tmeta, Tdata, Tsvc>(this IEndpointRouteBuilder app, string prefix,
    bool authorize = true)
    where Tmeta : Resource, new()
    where Tdata : Resource, new()
    where Tsvc : SCIMv2<Tmeta, Tdata>
  {
    var resourceMap = new ResourceMap(typeof(Tmeta), prefix);
    ServiceProviderConfiguration.Map.Add(resourceMap);

    RouteGroupBuilder group = app.MapGroup(prefix);

    #region Query (GET /)

    var map = group.MapGet("/", async context =>
    {
      CancellationToken cancellationToken = context.RequestAborted;
      var svc = context.RequestServices.GetRequiredService<Tsvc>();

      // SCIMv2 Filtering (RFC 7644 §3.4.2.2)
      string? filter = null;
      if (context.Request.Query.TryGetValue("filter", out var filterStr))
        filter = filterStr;

      // SCIMv2 Sorting (RFC 7644 §3.4.2.3)
      string? sortBy = null;
      string? sortOrder = null;
      if (context.Request.Query.TryGetValue("sortBy", out var sortByStr))
        sortBy = sortByStr;
      if (context.Request.Query.TryGetValue("sortOrder", out var sortOrderStr))
        sortOrder = sortOrderStr;

      // SCIMv2 Pagination (RFC 7644 §3.4.2.4)
      int startIndex = 1;
      int count = 12;

      if (context.Request.Query.TryGetValue("count", out var countStr) &&
          int.TryParse(countStr, out var parsedCount))  
      {
        count = parsedCount;

      }
      if (context.Request.Query.TryGetValue("startIndex", out var startIndexStr) &&
          int.TryParse(startIndexStr, out var parsedStart))
      {
        startIndex = parsedStart;
      }

      ListResponse<Tmeta> result = await svc.Query(startIndex, count, filter, sortBy, sortOrder, cancellationToken);

      // Create per-call JsonSerializer and materialize items enforcing camelCase; ignore potential null entries.
      var serializer = JsonSerializer.Create(CamelCaseItemSerializerSettings);

      // Treat null Resources as empty, then filter null elements -> defensively
      var resources = result.Resources ?? Enumerable.Empty<Tmeta>();

      var objects = resources
        .Where(r => r != null)
        .Select(r => JObject.FromObject(r!, serializer));

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

    #region Create (POST /)

    map = group.MapPost("/", async context =>
    {
      CancellationToken cancellationToken = context.RequestAborted;
      var svc = context.RequestServices.GetRequiredService<Tsvc>();

      using StreamReader reader = new(context.Request.Body);
      string json = await reader.ReadToEndAsync(cancellationToken);
      Tdata? resource = json.Deserialize<Tdata>();

      if (resource == null)
        throw new Exception($"Could not deserialize {typeof(Tdata).Name}");

      Guid id = await svc.Create(resource, cancellationToken);
      context.Response.StatusCode = (int)HttpStatusCode.Created;
      context.Response.Headers.Location = $"{context.Request.Path.Value}/{id}";
    });
    if (authorize)
      map.RequireAuthorization();

    #endregion

    #region Retrieve (GET /:id)

    map = group.MapGet("/{id}", async (HttpContext context, Guid id) =>
    {
      CancellationToken cancellationToken = context.RequestAborted;
      var svc = context.RequestServices.GetRequiredService<Tsvc>();

      Tdata? result = await svc.Retrieve(id, cancellationToken);

      if (result == null)
      {
        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
      }
      else
      {
        var serializer = JsonSerializer.Create(CamelCaseItemSerializerSettings);
        JObject jItem = JObject.FromObject(result, serializer);
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsync(jItem.ToString(Formatting.Indented), cancellationToken);
      }
    });
    if (authorize)
      map.RequireAuthorization();

    #endregion

    #region Replace (PUT /:id)

    map = group.MapPut("/{id}", async (HttpContext context, Guid id) =>
    {
      CancellationToken cancellationToken = context.RequestAborted;
      var svc = context.RequestServices.GetRequiredService<Tsvc>();

      Tdata? result = await svc.Retrieve(id, cancellationToken);
      if (result == null)
      {
        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
      }
      else
      {
        using StreamReader reader = new(context.Request.Body);
        string json = await reader.ReadToEndAsync(cancellationToken);
        Tdata? resource = json.Deserialize<Tdata>();

        if (resource == null)
          throw new Exception($"Could not deserialize {typeof(Tdata).Name}");

        bool replaced = await svc.Replace(id, resource, cancellationToken);

        if (!replaced)
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

    #region Update (PATCH /:id)

    map = group.MapPatch("/{id}", async (HttpContext context, Guid id) =>
    {
      CancellationToken cancellationToken = context.RequestAborted;
      var svc = context.RequestServices.GetRequiredService<Tsvc>();

      Tdata? resource = await svc.Retrieve(id, cancellationToken);
      if (resource == null)
      {
        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
      }
      else
      {
        // JSON Patch (RFC 6902 §3)
        using StreamReader reader = new(context.Request.Body);
        string json = await reader.ReadToEndAsync(cancellationToken);
        JArray patches = JArray.Parse(json);

        bool updated = await svc.Update(id, resource, patches, cancellationToken);

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

    #region Delete (DELETE /:id)

    map = group.MapDelete("/{id}", async (HttpContext context, Guid id) =>
    {
      CancellationToken cancellationToken = context.RequestAborted;
      var svc = context.RequestServices.GetRequiredService<Tsvc>();

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

  /// <summary>
  /// Registers SCIM Bulk operation handler at the given prefix.
  /// </summary>
  /// <param name="app">Endpoint route builder.</param>
  /// <param name="prefix">URL prefix, defaults to "/Bulk".</param>
  /// <param name="authorize">Whether to require authorization on the mapped route.</param>
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
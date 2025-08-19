using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using Looplex.Foundation.Helpers;
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

namespace Looplex.Foundation.WebApp.Middlewares;

public static class SCIMv2
{
  private static readonly ServiceProviderConfiguration ServiceProviderConfiguration = new();

  // === Envelopes to guarantee SCIM compliance: "Resources"/"Resource" with uppercase R ===
  private sealed class ResourceEnvelope<T>
  {
    [JsonPropertyName("Resource")]
    public T Item { get; set; } = default!;
  }

  private sealed class ResourcesEnvelope<T>
  {
    [JsonPropertyName("Resources")]
    public IList<T> Items { get; set; } = new List<T>();

    public long totalResults { get; set; }
    public long startIndex { get; set; }
    public long itemsPerPage { get; set; }

  }

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
  /// Registers /Users, /Groups, /Bulk routes.
  /// </summary>
  public static IEndpointRouteBuilder UseSCIMv2(this IEndpointRouteBuilder app, bool authorize = true)
  {
    app.UseSCIMv2<User, User, Users>("/Users", authorize);
    app.UseSCIMv2<Group, Group, Groups>("/Groups", authorize);
    app.UseSCIMv2<ClientService, ClientService, ClientServices>("/Api-Keys", authorize);

    app.UseBulk("/Bulk", authorize);

    return app;
  }

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
      if (context.Request.Query.TryGetValue("startIndex", out var si) &&
      int.TryParse(si, out var parsedSi) && parsedSi >= 1)
        startIndex = parsedSi;

      if (context.Request.Query.TryGetValue("count", out var c) &&
          int.TryParse(c, out var parsedCount))
      {
        var max = ServiceProviderConfiguration.MaxPageSize > 0 ? ServiceProviderConfiguration.MaxPageSize : 200;
        count = Math.Clamp(parsedCount, 0, max);
      }

      var result = await svc.Query(startIndex, count, filter, sortBy, sortOrder, cancellationToken);

      // Materialize each resource as camelCase JSON string via our serializer
      var jsource = result.Resources
        .Select(r => JObject.Parse(JsonSerializerFoundation.Serialize(r)))
        .ToList();

      // Keep your attribute pipeline
      var processed = jsource.ProcessAttributes(context).ToList();

      // Convert processed JObject -> JsonElement to serialize envelope with System.Text.Json
      var items = new List<JsonElement>();
      foreach (var p in processed)
      {
        using var doc = JsonDocument.Parse(p.ToString(Newtonsoft.Json.Formatting.None));
        items.Add(doc.RootElement.Clone());
      }

      // Envelope with PascalCase top-level key "Resources"
      var envelope = new ResourcesEnvelope<JsonElement>
      {
        startIndex = result.StartIndex,
        itemsPerPage = result.ItemsPerPage,
        totalResults = result.TotalResults,
        Items = items
      };
      // Final JSON via our serializer (camelCase by default; "Resources" preserved by attribute)
      string json = JsonSerializerFoundation.Serialize(envelope);
      context.Response.ContentType = "application/scim+json; charset=utf-8";
      await context.Response.WriteAsync(json, cancellationToken);
    });
    if (authorize)
      map.RequireAuthorization();

    #endregion

    #region Create (POST /)

    // [SCIMv2 Create](https://datatracker.ietf.org/doc/html/rfc7644#section-3.3)
    // [SCIMv2 Representation: id, meta](https://datatracker.ietf.org/doc/html/rfc7644#section-3.1)
    // [SCIMv2 Content-Type](https://datatracker.ietf.org/doc/html/rfc7644#section-3)
    // [HTTP Semantics — 201 Created](https://www.rfc-editor.org/rfc/rfc9110#name-201-created)
    // [HTTP ETag (Weak)](https://www.rfc-editor.org/rfc/rfc9110#field.etag)
    map = group.MapPost("/", async context =>
    {
      CancellationToken cancellationToken = context.RequestAborted;
      var svc = context.RequestServices.GetRequiredService<Tsvc>();

      using var reader = new StreamReader(context.Request.Body);
      string json = await reader.ReadToEndAsync(cancellationToken);
      Tdata? resource = json.Deserialize<Tdata>();

      if (resource == null)
        throw new Exception($"Could not deserialize {typeof(Tdata).Name}");

      // Persist and obtain server-generated UUID
      Guid id = await svc.Create(resource, cancellationToken);

      // Build absolute and relative locations (SCIM meta.location SHOULD be absolute)
      var relativeLocation = $"{context.Request.Path.Value}/{id}";
      var absoluteLocation = $"{context.Request.Scheme}://{context.Request.Host}{relativeLocation}";



      // Minimal SCIM resource payload (schemas + id + meta)

      var now = DateTimeOffset.UtcNow;
      var resourceObject = context.Request.Path.Value?.Trim('/').Split('/').Last();

      var scimBody = new JsonObject
      {
        // Core "Resource" schema for custom resources
        // [SCIMv2 Core Schema Identifier](https://datatracker.ietf.org/doc/html/rfc7643#section-3)
        // Custom extension schema defined by Looplex (RFC 7643 §6 — schema extension).
        ["schemas"] = new JsonArray($"urn:ietf:params:scim:schemas:core:2.0:Resource" , $"urn:looplex:params:scim:schemas:{resourceObject}:2.0:{typeof(Tdata).Name}"),

        ["id"] = id.ToString(), // MUST be stable, non-reassignable

        // meta fields per SCIM representation (resourceType, created, lastModified, location, version)
        ["meta"] = new JsonObject
        {
          ["resourceType"] = typeof(Tdata).Name, // Prefer a stable literal like "Policy" if applicable
          ["created"] = now.ToString("o"),
          ["lastModified"] = now.ToString("o"),
          ["location"] = absoluteLocation,
          // ETag SHOULD reflect a object version
          ["version"] = $"W/\"{resource.ComputeMD5()}\""
        }
      };

      // Serialize SCIM resource response
      // [SCIMv2 Representation — Attribute presence vs. null](https://datatracker.ietf.org/doc/html/rfc7644#section-3.1)
      // Attributes with null values MUST be omitted from the representation
      var scimJsonOptions = new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        // This ensures properties with null values are not written
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
      };

      // Merge payload attributes into SCIM root (RFC 7644 requires attributes at the top-level)
      // [SCIMv2 Attribute Representation](https://datatracker.ietf.org/doc/html/rfc7644#section-3.1)
      var payload = System.Text.Json.JsonSerializer.SerializeToNode(resource, scimJsonOptions)!.AsObject();

      foreach (var kv in payload)
      {
        // Do not overwrite reserved SCIM fields
        if (kv.Key is "id" or "meta" or "schemas") continue;

        scimBody[kv.Key] = kv.Value?.DeepClone();
      }

      // Add Location header: relative is allowed per RFC 9110
      context.Response.StatusCode = (int)HttpStatusCode.Created;
      context.Response.Headers.Location = $"{context.Request.Path.Value}/{id}";

      // Prepare HTTP response
      context.Response.StatusCode = StatusCodes.Status201Created;
      context.Response.Headers.Location = $"{context.Request.Path.Value}/{id}";           // RFC 9110 §10.2.2
      context.Response.Headers.ETag = $"W/\"{resource.ComputeMD5()}\"";                  // RFC 7644 §3.14; rfc 9110 §8.8
      context.Response.ContentType = "application/scim+json";                           // RFC 7644 §3; [SCIMv2 Media Type](https://www.iana.org/assignments/media-types/application/scim+json)

      // Serialize in camelCase to match SCIM examples
      var jsonOptions = new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
      };

      await System.Text.Json.JsonSerializer.SerializeAsync(context.Response.Body, scimBody, jsonOptions, cancellationToken);
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
        // Wrap single item under "Resource" (uppercase R)
        var envelope = new ResourceEnvelope<Tdata> { Item = result };
        string json = envelope.Serialize();

        context.Response.ContentType = "application/scim+json; charset=utf-8";
        await context.Response.WriteAsync(json, cancellationToken);
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
        // [JSON Patch](https://datatracker.ietf.org/doc/html/rfc6902#section-3)
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

        context.Response.ContentType = "application/scim+json; charset=utf-8";
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        await context.Response.WriteAsync(result.Serialize(), cancellationToken);
      });
    return app;
  }
}

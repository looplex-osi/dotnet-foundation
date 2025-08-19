using System.Net;
using System.Security.Cryptography;
using System.Text;
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
using Newtonsoft.Json.Serialization;

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
    // SCIM requires the top-level "Resources" key (uppercase R)
    [JsonPropertyName("Resources")]
    public IList<T> Items { get; set; } = new List<T>();

    // SCIM ListResponse MUST include this schemas value
    // RFC 7644 §3.4.2.2 / §3.4.2.4
    [JsonPropertyName("schemas")]
    public string[] Schemas { get; set; } = new[]
    {
    "urn:ietf:params:scim:api:messages:2.0:ListResponse"
    };

    // These names are already camelCase by default, but we pin them explicitly.
    [JsonPropertyName("totalResults")]
    public long TotalResults { get; set; }

    [JsonPropertyName("startIndex")]
    public long StartIndex { get; set; }

    [JsonPropertyName("itemsPerPage")]
    public long ItemsPerPage { get; set; }

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

      // startIndex
      if (context.Request.Query.TryGetValue("startIndex", out var si) &&
          int.TryParse(si, out var parsedSi) && parsedSi >= 1)
      {
        startIndex = parsedSi;
      }

      // count with clamp
      if (context.Request.Query.TryGetValue("count", out var c) &&
          int.TryParse(c, out var parsedCount))
      {
        var cfg = context.RequestServices.GetRequiredService<ServiceProviderConfiguration>();
        var max = cfg.Filter?.MaxResults > 0 ? cfg.Filter.MaxResults : 200;
        count = (int)Math.Clamp(parsedCount, 0, max);
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

      // SCIM ListResponse envelope (includes required "schemas")
      var envelope = new ResourcesEnvelope<JsonElement>
      {
        StartIndex = result.StartIndex,
        ItemsPerPage = result.ItemsPerPage,
        TotalResults = result.TotalResults,
        Items = items
      };

      // Serialize with omitNulls=true for SCIM parity
      string json = JsonSerializerFoundation.Serialize(envelope, omitNulls: true);
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
        ["schemas"] = new JsonArray(
          "urn:ietf:params:scim:schemas:core:2.0:Resource",
          $"urn:looplex:params:scim:schemas:{resourceObject}:2.0:{typeof(Tdata).Name}"
        ),

        ["id"] = id.ToString(), // MUST be stable, non-reassignable

        // meta fields per SCIM representation (resourceType, created, lastModified, location)
        ["meta"] = new JsonObject
        {
          ["resourceType"] = typeof(Tdata).Name, // Prefer a stable literal like "Policy" if applicable
          ["created"] = now.ToString("o"),
          ["lastModified"] = now.ToString("o"),
          ["location"] = absoluteLocation
          // Do NOT set "version" yet; it must reflect the final representation (see ETag block below)
        }
      };

      // Serialize SCIM resource response
      // [SCIMv2 Representation — Attribute presence vs. null](https://datatracker.ietf.org/doc/html/rfc7644#section-3.1)
      // Attributes with null values MUST be omitted from the representation
      var scimJsonOptions = new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull // omit nulls for SCIM parity
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

      // Prepare HTTP response (single place)
      context.Response.StatusCode = StatusCodes.Status201Created;                 // RFC 9110 §10.2.2
      context.Response.Headers.Location = relativeLocation;                       // Relative is OK
      context.Response.ContentType = "application/scim+json; charset=utf-8";      // SCIM media type

      // ---- Compute ETag from the FINAL SCIM representation ----
      // Canonical JSON for hashing: camelCase, compact, omit nulls (SCIM parity)
      var etagJsonOptions = new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
      };

      // Serialize the final body (after merging payload + server-generated fields)
      string etagPayload = System.Text.Json.JsonSerializer.Serialize(scimBody, etagJsonOptions);

      // Weak ETag only (NOT for cryptographic security) — RFC 9110 §8.8
      string etagValue;
      using (var md5 = MD5.Create())
      {
        etagValue = Convert.ToBase64String(md5.ComputeHash(Encoding.UTF8.GetBytes(etagPayload)));
      }

      // Set ETag header and mirror it into meta.version to keep both in sync
      context.Response.Headers.ETag = $"W/\"{etagValue}\"";                        // RFC 7644 §3.14; RFC 9110 §8.8
      if (scimBody["meta"] is JsonObject metaObj)
      {
        metaObj["version"] = $"W/\"{etagValue}\"";
      }

      // ---- Write response body ----
      // Pretty output for readability; still omit nulls for SCIM parity
      var outputOptions = new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
      };

      await System.Text.Json.JsonSerializer.SerializeAsync(
        context.Response.Body,
        scimBody,
        outputOptions,
        cancellationToken);
    });

    if (authorize)
      map.RequireAuthorization();

    #endregion



    #region Retrieve (GET /:id)

    map = group.MapGet("/{id}", async (HttpContext context, Guid id) =>
    {
      CancellationToken cancellationToken = context.RequestAborted;
      var svc = context.RequestServices.GetRequiredService<Tsvc>();

      var result = await svc.Retrieve(id, cancellationToken);

      if (result is null)
      {
        // [HTTP Semantics — 404 Not Found](https://www.rfc-editor.org/rfc/rfc9110#name-404-not-found)
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
      }

      // Newtonsoft-only pipeline (no System.Text.Json round-trips).
      // [SCIM Representation — omit null attributes](https://www.rfc-editor.org/rfc/rfc7644#section-3.1)
      var settings = new JsonSerializerSettings
      {
        ContractResolver = new DefaultContractResolver
        {
          NamingStrategy = new CamelCaseNamingStrategy(
            processDictionaryKeys: true,
            overrideSpecifiedNames: false // Preserve explicit PascalCase keys when specified
          )
        },
        NullValueHandling = NullValueHandling.Ignore // Omit nulls for SCIM parity
      };
      var serializer = Newtonsoft.Json.JsonSerializer.Create(settings);

      // Domain -> JObject
      var jobj = (JObject)JToken.FromObject(result, serializer);

      // Apply 'attributes' and 'excludedAttributes' if supplied by the client.
      // [SCIMv2 Attributes Parameter](https://www.rfc-editor.org/rfc/rfc7644#section-3.4.2.5)
      var processed = new[] { jobj }.ProcessAttributes(context).First();

      // Envelope with PascalCase top-level key "Resource".
      // Note: SCIM single-resource responses are commonly bare resources; this envelope is an implementation choice.
      // [SCIMv2 Resource Retrieval](https://www.rfc-editor.org/rfc/rfc7644#section-3.4.1)
      var envelope = new JObject
      {
        ["Resource"] = processed
      };

      // Write response using SCIM media type.
      // [SCIMv2 Media Type](https://www.rfc-editor.org/rfc/rfc7644#section-3)
      context.Response.ContentType = "application/scim+json; charset=utf-8";
      string json = envelope.ToString(Formatting.None); // or Formatting.Indented if desired
      await context.Response.WriteAsync(json, cancellationToken);
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

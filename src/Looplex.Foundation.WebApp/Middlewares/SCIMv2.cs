using System.Net;
using System.Reflection;

using Looplex.Foundation.SCIMv2.Entities;
using Looplex.Foundation.Serialization.Json;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Looplex.Foundation.WebApp.Middlewares;

public static class SCIMv2
{
  private static readonly ServiceProviderConfiguration ServiceProviderConfiguration = new();

  public static IServiceCollection AddSCIMv2(this IServiceCollection services)
  {
    services.AddHttpContextAccessor();
    services.AddSingleton(ServiceProviderConfiguration);
    services.AddTransient<Bulks>();

    foreach (var type in ServiceProviderConfiguration.Services)
      services.AddTransient(type);

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
    app.UseSCIMv2<User>("/Users", authorize);
    app.UseSCIMv2<Group>("/Groups", authorize);

    app.UseBulk("/Bulk", authorize);

    return app;
  }

  public static IEndpointRouteBuilder RegisterSCIMv2Services(this IEndpointRouteBuilder app, Assembly assembly)
  {
    var openGeneric = typeof(SCIMv2<>);

    Type[] types;
    try
    {
      types = assembly.GetTypes();
    }
    catch (ReflectionTypeLoadException ex)
    {
      // Some assemblies might fail to load certain types. 
      // We can safely skip or log them:
      types = (ex.Types?.Where(t => t != null) ?? []).ToArray()!;
    }

    foreach (var type in types)
    {
      if (!type.IsGenericType)
        continue;

      var genericDefinition = type.GetGenericTypeDefinition();

      if (genericDefinition == openGeneric && ServiceProviderConfiguration.Services.All(s => s != type))
      {
        ServiceProviderConfiguration.Services.Add(type);
      }
    }

    return app;
  }
  
  public static bool InheritsFromSCIMv2OfType(Type candidateType, Type runtimeType)
  {
    while (candidateType != typeof(object))
    {
      if (candidateType.IsGenericType &&
          candidateType.GetGenericTypeDefinition() == typeof(SCIMv2<>))
      {
        Type[] genericArgs = candidateType.GetGenericArguments();
        if (genericArgs.Length == 1 && genericArgs[0] == runtimeType)
        {
          return true;
        }
      }

      if (candidateType.BaseType == null) break;
      
      candidateType = candidateType.BaseType;
    }

    return false;
  }

  public static IEndpointRouteBuilder UseSCIMv2<T>(this IEndpointRouteBuilder app, string prefix, bool authorize = true)
    where T : Resource, new()
  {
    var serviceType = ServiceProviderConfiguration.Services.FirstOrDefault(type =>InheritsFromSCIMv2OfType(type, typeof(T)));

    if (serviceType == null)
      throw new Exception(
        $"A service type configuration for {typeof(T).Name} was not found. Make sure to call RegisterSCIMv2Services for your assemblies.");
    
    var resourceMap = new ResourceMap(typeof(T), prefix, serviceType);

    ServiceProviderConfiguration.Map.Add(resourceMap);

    RouteGroupBuilder group = app.MapGroup(prefix);

    #region Query

    var map = group.MapGet("/", async context =>
    {
      CancellationToken cancellationToken = context.RequestAborted;
      SCIMv2<T> svc = GetService<T>(app.ServiceProvider, resourceMap);

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

      ListResponse<T> result = await svc.Query(startIndex, count, filter, sortBy, sortOrder, cancellationToken);
      string json = result.Serialize();
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
      SCIMv2<T> svc = GetService<T>(app.ServiceProvider, resourceMap);

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
      SCIMv2<T> svc = GetService<T>(app.ServiceProvider, resourceMap);

      T? result = await svc.Retrieve(id, cancellationToken);

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
      SCIMv2<T> svc = GetService<T>(app.ServiceProvider, resourceMap);

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
      SCIMv2<T> svc = GetService<T>(app.ServiceProvider, resourceMap);

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

  private static SCIMv2<T> GetService<T>(IServiceProvider serviceProvider, ResourceMap resourceMap) where T : Resource, new()
  {
    var svc = serviceProvider.GetService(resourceMap.Service) as SCIMv2<T>;
    if (svc == null)
      throw new Exception(
        $"Could not instantiate {nameof(SCIMv2<T>)}. Make sure to call AddSCIMv2 after the RegisterSCIMv2Services.");
    return svc;
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
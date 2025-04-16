using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Looplex.Foundation.Entities;
using Looplex.Foundation.Helpers;
using Looplex.Foundation.Serialization.Json;
using Looplex.OpenForExtension.Abstractions.Commands;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Looplex.OpenForExtension.Abstractions.ExtensionMethods;
using Looplex.OpenForExtension.Abstractions.Plugins;

using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Looplex.Foundation.SCIMv2.Entities;

public class Bulks : Service
{
  private readonly ServiceProviderConfiguration? _serviceProviderConfiguration;
  private readonly IServiceProvider? _serviceProvider;

  #region Reflectivity

  // ReSharper disable once PublicConstructorInAbstractClass
  public Bulks() : base()
  {
  }

  #endregion

  [ActivatorUtilitiesConstructor]
  public Bulks(
    IList<IPlugin> plugins,
    IServiceProvider serviceProvider,
    ServiceProviderConfiguration serviceProviderConfiguration) : base(plugins)
  {
    _serviceProvider = serviceProvider;
    _serviceProviderConfiguration = serviceProviderConfiguration;
  }

  public async Task<BulkResponse> Execute(BulkRequest request, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    IContext ctx = NewContext();

    await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);

    ValidateBulkIdsUniqueness(request);
    await ctx.Plugins.ExecuteAsync<IValidateInput>(ctx, cancellationToken);

    ctx.Roles["BulkRequest"] = request;
    await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

    await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

    if (!ctx.SkipDefaultAction)
    {
      var response = new BulkResponse();

      request = ctx.Roles["BulkRequest"];

      var errorCount = 0;
      Dictionary<string, string> bulkIdCrossReference = [];

      foreach (var operation in request.Operations)
      {
        try
        {
          ValidateOperation(operation);

          var (resourceMap, resourceUniqueId) = GetResourceMap(operation, _serviceProviderConfiguration!);

          var service = _serviceProvider!.GetRequiredService(resourceMap.Type);

          if (operation.Data != null)
          {
            JsonHelper.Traverse(operation.Data, BulkIdVisitor(bulkIdCrossReference));
          }

          if (operation.Method == Method.Post)
          {
            var id = await ExecutePostMethod(
              operation,
              service,
              response,
              resourceMap,
              cancellationToken);

            bulkIdCrossReference[operation.BulkId!] = id;
          }
          else if (operation.Method == Method.Patch)
          {
            await ExecutePatchMethod(
              operation,
              service,
              response,
              resourceMap,
              resourceUniqueId!.Value,
              cancellationToken);
          }
          else if (operation.Method == Method.Delete)
          {
            await ExecuteDeleteMethod(
              operation,
              service,
              response,
              resourceUniqueId!.Value,
              cancellationToken);
          }
        }
        catch (Exception e)
        {
          Error error;
          if (e is SCIMv2Exception scimEx)
            error = scimEx.Error;
          else
            error = new Error(
              e.Message,
              (int)HttpStatusCode.InternalServerError);

          errorCount++;
          if (errorCount > request.FailOnErrors)
            break;
          response.Operations.Add(new()
          {
            Method = operation.Method, Path = operation.Path, Status = error.Status, Response = error.Serialize()
          });
        }
      }

      ctx.Result = response;
    }

    await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);
    await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);

    return (BulkResponse)ctx.Result;
  }

  internal static Action<JToken> BulkIdVisitor(Dictionary<string, string> bulkIdCrossReference)
  {
    return (node) =>
    {
      if (node.Type == JTokenType.String)
      {
        var nodeValue = node.Value<string>();

        if (nodeValue != null && nodeValue.StartsWith("bulkId:"))
        {
          var key = nodeValue["bulkId:".Length..];

          if (!bulkIdCrossReference.TryGetValue(key, out var bulkIdValue))
            throw new SCIMv2Exception(
              $"Bulk id {key} not defined",
              ErrorScimType.InvalidValue,
              (int)HttpStatusCode.BadRequest);

          node.Replace(bulkIdValue);
        }
      }
    };
  }

  internal static async Task<string> ExecutePostMethod(
    BulkRequestOperation operation, object service, BulkResponse bulkResponse,
    ResourceMap resourceMap, CancellationToken cancellationToken)
  {
    var resource = operation.Data!.ToObject(resourceMap.Type);

    var createMethod = service.GetType().GetMethod("Create", [resourceMap.GetType(), typeof(CancellationToken)]);
    if (createMethod is null)
      throw new InvalidOperationException("Create method not found.");

    object createTaskObj = createMethod.Invoke(service, [resource, cancellationToken])!;
    var createTask = (Task<Guid>)createTaskObj;
    Guid createdId = await createTask;
    var id = createdId.ToString();

    bulkResponse.Operations.Add(new()
    {
      Method = operation.Method,
      Path = operation.Path,
      Location = $"{resourceMap.Resource}/{id}",
      Status = (int)HttpStatusCode.Created
    });

    return id;
  }

  internal static async Task ExecutePatchMethod(
    BulkRequestOperation operation, object service, BulkResponse bulkResponse,
    ResourceMap resourceMap, Guid resourceUniqueId, CancellationToken cancellationToken)
  {
    var resource = operation.Data!.ToObject(resourceMap.Type);

    var updateMethod = service.GetType().GetMethod("Update", [
      typeof(Guid),
      resourceMap.Type,
      typeof(string),
      typeof(CancellationToken)
    ]);
    if (updateMethod is null)
      throw new InvalidOperationException("Update method not found.");

    object updateTaskObj = updateMethod.Invoke(service, [resourceUniqueId, resource, null, cancellationToken])!;
    var updateTask = (Task<bool>)updateTaskObj;
    bool updateSuccess = await updateTask;
    var id = resourceUniqueId.ToString();

    if (!updateSuccess)
      throw new SCIMv2Exception($"Resource with id {id} was not patched.", (int)HttpStatusCode.ExpectationFailed);

    bulkResponse.Operations.Add(new()
    {
      Method = operation.Method,
      Path = operation.Path,
      Location = $"{resourceMap.Resource}/{id}",
      Status = (int)HttpStatusCode.NoContent
    });
  }

  internal static async Task ExecuteDeleteMethod(
    BulkRequestOperation operation, object service, BulkResponse bulkResponse,
    Guid resourceUniqueId, CancellationToken cancellationToken)
  {
    var deleteMethod = service.GetType().GetMethod("Delete", new[] { typeof(Guid), typeof(CancellationToken) });
    if (deleteMethod is null)
      throw new InvalidOperationException("Delete method not found.");

    object deleteTaskObj = deleteMethod.Invoke(service, new object[] { resourceUniqueId, cancellationToken })!;
    var deleteTask = (Task<bool>)deleteTaskObj;
    bool deleteSuccess = await deleteTask;
    var id = resourceUniqueId.ToString();

    if (!deleteSuccess)
      throw new SCIMv2Exception($"Resource with id {id} was not deleted.", (int)HttpStatusCode.ExpectationFailed);
  }

  internal static void ValidateOperation(BulkRequestOperation operation)
  {
    if (operation.Data == null &&
        operation.Method != Method.Delete)
      throw new SCIMv2Exception(
        $"Data should have value for method {operation.Method}",
        ErrorScimType.InvalidValue,
        (int)HttpStatusCode.BadRequest);

    if (string.IsNullOrWhiteSpace(operation.BulkId) &&
        operation.Method == Method.Post)
      throw new SCIMv2Exception(
        $"BulkId should have value for method {operation.Method}",
        ErrorScimType.InvalidValue,
        (int)HttpStatusCode.BadRequest);
  }

  internal static void ValidateBulkIdsUniqueness(BulkRequest bulkRequest)
  {
    var nonUniqueBulkIds = bulkRequest.Operations
      .Where(o => !string.IsNullOrWhiteSpace(o.BulkId))
      .GroupBy(o => o.BulkId)
      .Where(g => g.Count() > 1)
      .Select(g => g.Key)
      .ToList();
    if (nonUniqueBulkIds.Any())
    {
      var bulkIds = string.Join(", ", nonUniqueBulkIds);
      throw new SCIMv2Exception(
        $"BulkIds {bulkIds} must be unique",
        ErrorScimType.Uniqueness,
        (int)HttpStatusCode.BadRequest);
    }
  }

  internal static (ResourceMap, Guid?) GetResourceMap(BulkRequestOperation operation,
    ServiceProviderConfiguration serviceProviderConfiguration)
  {
    var path = operation.Path;
    if (path.StartsWith("/"))
      path = path[1..];

    var indexOfSlash = path.IndexOf('/');

    if (indexOfSlash <= 0 && operation.Method != Method.Post)
      throw new SCIMv2Exception(
        $"Path {operation.Path} should refer to a specific resource when method is {operation.Method}",
        ErrorScimType.InvalidPath,
        (int)HttpStatusCode.BadRequest);

    var resource = path;
    Guid? resourceUniqueId = null;

    if (indexOfSlash > 0 && indexOfSlash < path.Length)
    {
      resource = path[..indexOfSlash];
      var resourceIdentifier = path[(indexOfSlash + 1)..];

      if (Guid.TryParse(resourceIdentifier, out var uuid))
      {
        resourceUniqueId = uuid;
      }
      else
        throw new SCIMv2Exception(
          $"Resource identifier {resourceIdentifier} is not valid",
          ErrorScimType.InvalidValue,
          (int)HttpStatusCode.BadRequest);
    }

    var resourceMap = serviceProviderConfiguration.Map
      .FirstOrDefault(rm => rm.Resource == resource);

    if (resourceMap == null)
      throw new SCIMv2Exception(
        $"Path {resource} does not exist",
        ErrorScimType.InvalidPath,
        (int)HttpStatusCode.BadRequest);

    return (resourceMap, resourceUniqueId);
  }
}

public sealed class BulkRequest : Actor
{
  /// <summary>
  /// The number of errors that the service provider will accept before the operation is
  /// terminated. OPTIONAL in a request.
  /// </summary>
  public long? FailOnErrors { get; set; }

  /// <summary>
  /// Defines operations within a bulk job. Each operation corresponds to a single HTTP request
  /// against a resource endpoint.
  /// </summary>
  [JsonProperty("Operations")]
  public List<BulkRequestOperation> Operations { get; set; } = [];
}

public sealed class BulkRequestOperation
{
  /// <summary>
  /// The transient identifier of a newly created resource. REQUIRED when 'method' is 'POST'.
  /// </summary>
  public string? BulkId { get; set; }

  /// <summary>
  /// The resource data as it would appear for a single SCIM POST, PUT, or PATCH operation.
  /// REQUIRED when 'method' is 'POST', 'PUT', or 'PATCH'.
  /// </summary>
  public JToken? Data { get; set; }

  /// <summary>
  /// The HTTP method of the current operation.
  /// </summary>
  public Method Method { get; set; }

  /// <summary>
  /// The resource's relative path. REQUIRED in a request.
  /// </summary>
  public string? Path { get; set; }

  /// <summary>
  /// The current resource version. Used if the service provider supports ETags and 'method' is
  /// 'PUT', 'PATCH', or 'DELETE'.
  /// </summary>
  public string? Version { get; set; }
}

/// <summary>
/// The HTTP method of the current operation.
/// </summary>
public enum Method
{
  Delete,
  Patch,
  Post,
  Put
};

public sealed class BulkResponse : Actor
{
  /// <summary>
  /// Defines operations within a bulk job. Each operation corresponds to a single HTTP request
  /// against a resource endpoint.
  /// </summary>
  [JsonProperty("Operations")]
  public List<BulkResponseOperation> Operations { get; set; } = [];
}

public partial class BulkResponseOperation
{
  /// <summary>
  /// The transient identifier of a newly created resource. REQUIRED when 'method' is 'POST'.
  /// </summary>
  [JsonProperty("bulkId", NullValueHandling = NullValueHandling.Ignore)]
  public string? BulkId { get; set; }

  /// <summary>
  /// The resource data as it would appear for a single SCIM POST, PUT, or PATCH operation.
  /// REQUIRED when 'method' is 'POST', 'PUT', or 'PATCH'.
  /// </summary>
  [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
  public JToken? Data { get; set; }

  /// <summary>
  /// The resource endpoint URL. REQUIRED in a response, except in the event of a POST failure.
  /// </summary>
  [JsonProperty("location")]
  public string? Location { get; set; }

  /// <summary>
  /// The HTTP method of the current operation.
  /// </summary>
  [JsonProperty("method")]
  public Method Method { get; set; }

  /// <summary>
  /// The resource's relative path. REQUIRED in a request.
  /// </summary>
  [JsonProperty("path", NullValueHandling = NullValueHandling.Ignore)]
  public string? Path { get; set; }

  /// <summary>
  /// The HTTP response body for the specified request operation. MUST be included when
  /// indicating an HTTP status other than 200.
  /// </summary>
  [JsonProperty("response", NullValueHandling = NullValueHandling.Ignore)]
  public JToken? Response { get; set; }

  /// <summary>
  /// The HTTP response status code for the requested operation.
  /// </summary>
  [JsonProperty("status", NullValueHandling = NullValueHandling.Ignore)]
  public int? Status { get; set; }

  /// <summary>
  /// The current resource version. Used if the service provider supports ETags and 'method' is
  /// 'PUT', 'PATCH', or 'DELETE'.
  /// </summary>
  [JsonProperty("version", NullValueHandling = NullValueHandling.Ignore)]
  public string? Version { get; set; }
}
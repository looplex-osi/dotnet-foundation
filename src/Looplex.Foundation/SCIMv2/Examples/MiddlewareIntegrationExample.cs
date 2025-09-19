using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Looplex.Foundation.SCIMv2.Entities;
using Looplex.Foundation.SCIMv2;
using Microsoft.AspNetCore.Http;

namespace Looplex.Foundation.SCIMv2.Examples;

/// <summary>
/// Example of how to integrate SCIMv2Service with ASP.NET Core middleware
/// This shows the pattern for updating the existing middleware
/// </summary>
public class MiddlewareIntegrationExample
{
    private readonly ISCIMv2 _scimService;
    private readonly JsonSerializerOptions _jsonOptions;

    public MiddlewareIntegrationExample(ISCIMv2 scimService)
    {
        _scimService = scimService ?? throw new ArgumentNullException(nameof(scimService));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    /// <summary>
    /// Example of handling GET /Users request
    /// </summary>
    public async Task HandleQueryUsersAsync(HttpContext context)
    {
        var startIndex = int.Parse(context.Request.Query["startIndex"].FirstOrDefault() ?? "1");
        var count = int.Parse(context.Request.Query["count"].FirstOrDefault() ?? "100");
        var filter = context.Request.Query["filter"].FirstOrDefault();
        var sortBy = context.Request.Query["sortBy"].FirstOrDefault();
        var sortOrder = context.Request.Query["sortOrder"].FirstOrDefault();

        var response = await _scimService.QueryAsync("Users", startIndex, count, filter, sortBy, sortOrder);
        await WriteResponseAsync(context, response);
    }

    /// <summary>
    /// Example of handling POST /Users request
    /// </summary>
    public async Task HandleCreateUserAsync(HttpContext context)
    {
        var user = await JsonSerializer.DeserializeAsync<User>(context.Request.Body, _jsonOptions);
        if (user == null)
        {
            await WriteErrorResponseAsync(context, 400, "Invalid request body");
            return;
        }

        var response = await _scimService.CreateAsync("Users", user);
        await WriteResponseAsync(context, response);
    }

    /// <summary>
    /// Example of handling GET /Users/{id} request
    /// </summary>
    public async Task HandleRetrieveUserAsync(HttpContext context, string id)
    {
        var response = await _scimService.RetrieveAsync("Users", id);
        await WriteResponseAsync(context, response);
    }

    /// <summary>
    /// Example of handling PUT /Users/{id} request
    /// </summary>
    public async Task HandleReplaceUserAsync(HttpContext context, string id)
    {
        var user = await JsonSerializer.DeserializeAsync<User>(context.Request.Body, _jsonOptions);
        if (user == null)
        {
            await WriteErrorResponseAsync(context, 400, "Invalid request body");
            return;
        }

        var response = await _scimService.ReplaceAsync("Users", id, user);
        await WriteResponseAsync(context, response);
    }

    /// <summary>
    /// Example of handling PATCH /Users/{id} request
    /// </summary>
    public async Task HandleModifyUserAsync(HttpContext context, string id)
    {
        var patches = await JsonSerializer.DeserializeAsync<JsonElement>(context.Request.Body, _jsonOptions);
        if (patches.ValueKind != JsonValueKind.Array)
        {
            await WriteErrorResponseAsync(context, 400, "Invalid patch format");
            return;
        }

        // Convert JsonElement to PatchOperation[] (simplified)
        var patchOperations = new List<PatchOperation>();
        foreach (var patch in patches.EnumerateArray())
        {
            var patchObj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(patch.GetRawText());
            if (patchObj != null)
            {
                patchOperations.Add(new PatchOperation(
                    patchObj["op"]?.ToString() ?? "",
                    patchObj["path"]?.ToString() ?? "",
                    patchObj.ContainsKey("value") ? patchObj["value"] : null
                ));
            }
        }

        var response = await _scimService.ModifyAsync("Users", id, patchOperations.ToArray());
        await WriteResponseAsync(context, response);
    }

    /// <summary>
    /// Example of handling DELETE /Users/{id} request
    /// </summary>
    public async Task HandleDeleteUserAsync(HttpContext context, string id)
    {
        var response = await _scimService.DeleteAsync("Users", id);
        await WriteResponseAsync(context, response);
    }

    private async Task WriteResponseAsync(HttpContext context, SCIMv2Response response)
    {
        context.Response.StatusCode = response.StatusCode;
        context.Response.ContentType = "application/scim+json";

        if (response.Location != null)
        {
            context.Response.Headers.Add("Location", response.Location);
        }

        if (response.ETag != null)
        {
            context.Response.Headers.Add("ETag", response.ETag);
        }

        if (response.Data != null)
        {
            await JsonSerializer.SerializeAsync(context.Response.Body, response.Data, _jsonOptions);
        }
    }

    private async Task WriteErrorResponseAsync(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/scim+json";

        var errorResponse = new SCIMv2Response
        {
            StatusCode = statusCode,
            Error = new SCIMv2Error
            {
                Status = statusCode.ToString(),
                Detail = message
            }
        };

        await JsonSerializer.SerializeAsync(context.Response.Body, errorResponse, _jsonOptions);
    }
}

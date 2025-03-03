using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Looplex.Foundation.Ports;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Looplex.Foundation.Middlewares;

public class JsonResponseMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HashSet<string> _publicEndpoints = new()
    {
        "/token",
        "/health"
    };
    
    public JsonResponseMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task Invoke(HttpContext context, IConfiguration configuration, IJwtService jwtService)
    {
        var originalBodyStream = context.Response.Body;
        using var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;

        await _next(context);

        context.Response.ContentType = "application/json";

        memoryStream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(memoryStream);
        var responseBody = await reader.ReadToEndAsync();

        if (responseBody.StartsWith("\"") && responseBody.EndsWith("\""))
        {
            var rawJson = responseBody[1..^1]
                .Replace("\\\"", "\"")
                .Replace("\\r", "")
                .Replace("\\n", "");

            context.Response.Body = originalBodyStream;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(rawJson, Encoding.UTF8);
        }
        else
        {
            context.Response.Body = originalBodyStream;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(responseBody, Encoding.UTF8);
        }
    }
}
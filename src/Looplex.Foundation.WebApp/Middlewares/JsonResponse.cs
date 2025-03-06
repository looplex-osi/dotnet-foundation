using System.Text;

using Looplex.Foundation.Ports;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Looplex.Foundation.WebApp.Middlewares;

public class JsonResponse(RequestDelegate next)
{
  private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));

  private readonly HashSet<string> _publicEndpoints = new() { "/token", "/health" };

  public async Task Invoke(HttpContext context, IConfiguration configuration, IJwtService jwtService)
  {
    Stream? originalBodyStream = context.Response.Body;
    using MemoryStream memoryStream = new();
    context.Response.Body = memoryStream;

    await _next(context);

    context.Response.ContentType = "application/json";

    memoryStream.Seek(0, SeekOrigin.Begin);
    using StreamReader reader = new(memoryStream);
    string? responseBody = await reader.ReadToEndAsync();

    if (responseBody.StartsWith("\"") && responseBody.EndsWith("\""))
    {
      string rawJson = responseBody[1..^1]
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
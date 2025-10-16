using Microsoft.AspNetCore.Http;

namespace Looplex.Protocols.HTTP.Ports;

/// <summary>
/// Port for accessing HTTP context functionality
/// </summary>
public interface IHttpContextPort
{
    /// <summary>
    /// Gets the current HTTP context
    /// </summary>
    HttpContext? GetHttpContext();
    
    /// <summary>
    /// Sets the HTTP context
    /// </summary>
    void SetHttpContext(HttpContext context);
}


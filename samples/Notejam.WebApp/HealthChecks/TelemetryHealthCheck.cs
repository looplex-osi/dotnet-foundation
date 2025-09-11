using Microsoft.Extensions.Diagnostics.HealthChecks;
using Looplex.Foundation.Helpers;
using Looplex.Foundation.Ports;

namespace Looplex.Samples.WebApp.HealthChecks;

/// <summary>
/// Health check for telemetry service.
/// </summary>
public class TelemetryHealthCheck : IHealthCheck
{
    private readonly ITelemetryService _telemetryService;

    /// <summary>
    /// Initializes a new instance of the TelemetryHealthCheck class.
    /// </summary>
    /// <param name="telemetryService">The telemetry service to check.</param>
    public TelemetryHealthCheck(ITelemetryService telemetryService)
    {
        _telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
    }

    /// <summary>
    /// Performs the health check.
    /// </summary>
    /// <param name="context">The health check context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The health check result.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var (isHealthy, message) = await TelemetryServiceFactory.HealthCheckAsync(_telemetryService);
            
            return isHealthy 
                ? HealthCheckResult.Healthy(message)
                : HealthCheckResult.Unhealthy(message);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Telemetry health check failed with exception: {ex.Message}", ex);
        }
    }
}

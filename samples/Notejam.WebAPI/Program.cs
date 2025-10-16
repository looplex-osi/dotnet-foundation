using Looplex.Samples.WebAPI.Configuration;

namespace Looplex.Samples.WebAPI;

public static class Program
{
  #region Main Application Entry Point
  
  /// <summary>
  /// Main entry point for the Notejam WebAPI application.
  /// Configures services, middleware, and endpoints using extension methods for better organization.
  /// </summary>
  public static void Main(string[] args)
  {
    var builder = WebApplication.CreateBuilder(args);

    // Configure services using extension methods
    builder.Services.ConfigureHttpClients();
    var (services, configuration) = builder.Services.ConfigureEnvironment(builder.Configuration);
    services.ConfigureSCIMv2(configuration);
    services.ConfigureSCIMv2ServiceProvider();
    services.ConfigureRepositories();
    services.ConfigureAuthentication(configuration);

    // Build application and configure pipeline
    var app = builder.Build();
    
    // Configure middleware and endpoints using extension methods
    app.ConfigureMiddleware();
    app.ConfigureHealthChecks();
    app.ConfigureSCIMv2Endpoints();

    app.Run();
  }
  
  #endregion
}

#region Integration Testing Support

// Non-static Program class for integration testing
public class TestProgram
{
    // This class is needed for WebApplicationFactory in integration tests
    // The actual startup logic is in the static Program class above
}

#endregion
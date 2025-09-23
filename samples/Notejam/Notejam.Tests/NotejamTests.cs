using System.Reflection;
using System.Security.Claims;

using Casbin;

using Looplex.Foundation.Adapters.AuthZ.Casbin;
using Looplex.Foundation.Ports;
using Looplex.OpenForExtension.Abstractions.Commands;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Looplex.OpenForExtension.Abstractions.Plugins;
using Looplex.OpenForExtension.Loader;
using Looplex.Samples.Application.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using NSubstitute;
using Xunit;

namespace Looplex.Samples.Tests;

// xUnit doesn't need class attributes
public class NotejamTests
{
  private IEnforcer InitRbacEnforcer()
  {
    string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                 ?? throw new InvalidOperationException("Could not determine directory");
    string modelPath = Path.Combine(dir, "rbac", "model.ini");
    string policyPath = Path.Combine(dir, "rbac", "policy.csv");

    return new Enforcer(modelPath, policyPath);
  }

  [Fact]
  public void service_should_be_initialized()
  {
    Notejam service = new();
    Assert.IsInstanceOfType(service, typeof(Notejam));
  }

  [Fact]
  public async Task service_should_implement_dependency_injection_with_mocked_dependencies()
  {
    // Arrange (setup)
    IRbacService? rbacSvc = Substitute.For<IRbacService>();
    var user = Substitute.For<ClaimsPrincipal>();
    user.Claims.Returns(new[] { new Claim("tenant", "looplex.com.br") });
    var mockHttpAccessor = Substitute.For<IHttpContextAccessor>();
    var httpContext = new DefaultHttpContext() { User = user };
    mockHttpAccessor.HttpContext.Returns(httpContext);

    IList<IPlugin> plugins = [];
    Notejam notejam = new(plugins, rbacSvc, mockHttpAccessor);

    // Act
    string result = await notejam.Echo("World", CancellationToken.None);

    // Assert
    Assert.AreEqual(result, "Hello World");
  }

  [Fact]
  public async Task service_should_implement_microkernel_mocked_plugin()
  {
    // Arrange (setup)
    IRbacService? rbacSvc = Substitute.For<IRbacService>();
    var user = Substitute.For<ClaimsPrincipal>();
    var mockHttpAccessor = Substitute.For<IHttpContextAccessor>();
    var httpContext = new DefaultHttpContext() { User = user };
    mockHttpAccessor.HttpContext.Returns(httpContext);
    IPlugin? plugin = Substitute.For<IPlugin>();

    IList<IPlugin> plugins = [plugin];

    #region mockups

    plugin.ExecuteAsync<IBeforeAction>(Arg.Any<IContext>(), CancellationToken.None)
      .Returns(callinfo =>
      {
        IContext? ctx = callinfo.ArgAt<IContext>(0);
        ctx.State.Name = "John";
        ctx.State.Surname = "Doe";
        return Task.CompletedTask;
      });

    #endregion

    Notejam notejam = new(plugins, rbacSvc, mockHttpAccessor);

    // Act
    string result = await notejam.Echo("World", CancellationToken.None);

    // Assert
    Assert.AreEqual(result, "Hello John");
  }

  [Fact]
  public async Task service_should_implement_leverage_eptracker_plugin()
  {
    // Arrange (setup)
    IRbacService? rbacSvc = Substitute.For<IRbacService>();

    var user = Substitute.For<ClaimsPrincipal>();
    var mockHttpAccessor = Substitute.For<IHttpContextAccessor>();
    var httpContext = new DefaultHttpContext() { User = user };
    mockHttpAccessor.HttpContext.Returns(httpContext);

    PluginLoader loader = new();

    IEnumerable<string> dlls = Directory.GetFiles("plugins").Where(x => x.EndsWith(".dll"));
    IList<IPlugin> plugins = loader.LoadPlugins(dlls).ToList();

    Notejam notejam = new(plugins, rbacSvc, mockHttpAccessor);

    // Act
    string result = await notejam.Echo("World", CancellationToken.None);

    // Assert
    Assert.AreEqual(result, @"[""@define"",""@bind"",""@beforeAction"",""@afterAction""]");
  }

  [Fact]
  public async Task service_should_receive_rbac_as_a_dependency_and_succeed()
  {
    // Arrange (setup)
    IPlugin? plugin = Substitute.For<IPlugin>();
    var user = Substitute.For<ClaimsPrincipal>();
    user.Claims.Returns(new[]
    {
      new Claim(ClaimTypes.Email, "fabio.nagao@looplex.com.br"), new Claim("tenant", "looplex.com.br")
    });
    var mockHttpAccessor = Substitute.For<IHttpContextAccessor>();
    var httpContext = new DefaultHttpContext() { User = user };
    mockHttpAccessor.HttpContext.Returns(httpContext);
    ILogger<RbacService>? logger = Substitute.For<ILogger<RbacService>>();
    RbacService rbacSvc = new(InitRbacEnforcer(), logger);

    IList<IPlugin> plugins = [plugin];

    Notejam notejam = new(plugins, rbacSvc, mockHttpAccessor);

    plugin.ExecuteAsync<IBeforeAction>(Arg.Any<IContext>(), CancellationToken.None)
      .Returns(callInfo =>
      {
        return Task.CompletedTask;
      });

    // Act
    string result = await notejam.Echo("World", CancellationToken.None);

    // Assert
    Assert.AreEqual(result, @"Hello World");
  }

  [Fact]
  public async Task service_should_receive_rbac_as_a_dependency_and_unauthorize()
  {
    IPlugin? plugin = Substitute.For<IPlugin>();
    var user = Substitute.For<ClaimsPrincipal>();
    user.Claims.Returns(new[]
    {
      new Claim(ClaimTypes.Email, "john.doe@looplex.com.br"), new Claim("tenant", "looplex.com.br")
    });
    var mockHttpAccessor = Substitute.For<IHttpContextAccessor>();
    var httpContext = new DefaultHttpContext() { User = user };
    mockHttpAccessor.HttpContext.Returns(httpContext);
    ILogger<RbacService>? logger = Substitute.For<ILogger<RbacService>>();
    RbacService rbacSvc = new(InitRbacEnforcer(), logger);

    IList<IPlugin> plugins = [plugin];

    Notejam notejam = new(plugins, rbacSvc, mockHttpAccessor);

    plugin.ExecuteAsync<IBeforeAction>(Arg.Any<IContext>(), CancellationToken.None)
      .Returns(callInfo =>
      {
        return Task.CompletedTask;
      });

    // Act & Assert
    await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(async () =>
    {
      await notejam.Echo("World", CancellationToken.None);
    });
  }
}
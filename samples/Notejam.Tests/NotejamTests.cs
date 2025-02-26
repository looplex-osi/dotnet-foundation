using System.Data;
using System.Dynamic;
using System.Reflection;
using Casbin;
using Looplex.Foundation.Adapters.AuthZ.Casbin;
using Looplex.Foundation.OAuth2;
using Looplex.Foundation.Ports;
using Looplex.OpenForExtension.Abstractions.Commands;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Looplex.OpenForExtension.Abstractions.Plugins;
using Looplex.OpenForExtension.Loader;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Looplex.Samples.Tests;

[TestClass]
public class NotejamTests
{
    private IEnforcer InitRbacEnforcer()
	{
		var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
						?? throw new InvalidOperationException("Could not determine directory");
		var modelPath = Path.Combine(dir, "rbac", "model.ini");
		var policyPath = Path.Combine(dir, "rbac", "policy.csv");

		return new Enforcer(modelPath, policyPath);
	}

	[TestMethod]
	public void service_should_be_initialized()
	{
		var service = new Notejam();
		Assert.IsInstanceOfType(service, typeof(Notejam));
	}

	[TestMethod]
	public async Task service_should_implement_dependency_injection_with_mocked_dependencies()
	{
		// Arrange (setup)
		var rbacSvc = Substitute.For<IRbacService>();
		var userCtx = new UserContext()
		{
			Tenant = "looplex.com.br"
		};
		var db = Substitute.For<IDbConnection>();

		IList<IPlugin> plugins = [];
		var notejam = new Notejam(plugins, rbacSvc, userCtx, db);

		// Act
		var result = await notejam.Echo("World", CancellationToken.None);

		// Assert
		Assert.AreEqual(result, "Hello World");
	}

	[TestMethod]
	public async Task service_should_implement_microkernel_mocked_plugin()
	{
		// Arrange (setup)
		var rbacSvc = Substitute.For<IRbacService>();
		var userCtx = Substitute.For<IUserContext>();
		var db = Substitute.For<IDbConnection>();
		var plugin = Substitute.For<IPlugin>();

		IList<IPlugin> plugins = [plugin];

		#region mockups
		plugin.ExecuteAsync<IBeforeAction>(Arg.Any<IContext>(), CancellationToken.None)
			.Returns((callinfo) =>
			{
				var ctx = callinfo.ArgAt<IContext>(0);
				ctx.State.Name = "John";
				ctx.State.Surname = "Doe";
				return Task.CompletedTask;
			});
		#endregion

		var notejam = new Notejam(plugins, rbacSvc, userCtx, db);

		// Act
		var result = await notejam.Echo("World", CancellationToken.None);

		// Assert
		Assert.AreEqual(result, "Hello John");
	}

	[TestMethod]
	public async Task service_should_implement_leverage_eptracker_plugin()
	{
		// Arrange (setup)
		var rbacSvc = Substitute.For<IRbacService>();
		var userCtx = Substitute.For<IUserContext>();
		var db = Substitute.For<IDbConnection>();

		var loader = new PluginLoader();

		IEnumerable<string> dlls = Directory.GetFiles("plugins").Where(x => x.EndsWith(".dll"));
		IList<IPlugin> plugins = loader.LoadPlugins(dlls).ToList();

		var notejam = new Notejam(plugins, rbacSvc, userCtx, db);

		// Act
		var result = await notejam.Echo("World", CancellationToken.None);

		// Assert
		Assert.AreEqual(result, @"[""@define"",""@bind"",""@beforeAction"",""@afterAction""]");
	}

	[TestMethod]
	public async Task service_should_receive_rbac_as_a_dependency_and_succeed()
	{
		// Arrange (setup)
		var plugin = Substitute.For<IPlugin>();
		var userCtx = new UserContext()
		{
			Email = "fabio.nagao@looplex.com.br",
			Tenant = "looplex.com.br"
		};
		var db = Substitute.For<IDbConnection>();
		var logger = Substitute.For<ILogger<RbacService>>();
		var rbacSvc = new RbacService(InitRbacEnforcer(), logger);

		IList<IPlugin> plugins = [plugin];

		var notejam = new Notejam(plugins, rbacSvc, userCtx, db);

		plugin.ExecuteAsync<IBeforeAction>(Arg.Any<IContext>(), CancellationToken.None)
			.Returns((callInfo) =>
			{
				return Task.CompletedTask;
			});

		// Act
		string result = await notejam.Echo("World", CancellationToken.None);

		// Assert
		Assert.AreEqual(result, @"Hello World");
	}

	[TestMethod]
	public async Task service_should_receive_rbac_as_a_dependency_and_unauthorize()
	{
		var plugin = Substitute.For<IPlugin>();
		var userCtx = new UserContext()
		{
			Email = "john.doe@looplex.com.br",
			Tenant = "looplex.com.br"
		};
		var db = Substitute.For<IDbConnection>();
		var logger = Substitute.For<ILogger<RbacService>>();
		var rbacSvc = new RbacService(InitRbacEnforcer(), logger);

		IList<IPlugin> plugins = [plugin];

		var notejam = new Notejam(plugins, rbacSvc, userCtx, db);

		plugin.ExecuteAsync<IBeforeAction>(Arg.Any<IContext>(), CancellationToken.None)
			.Returns((callInfo) =>
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
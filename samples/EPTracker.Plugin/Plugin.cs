using Looplex.OpenForExtension.Abstractions.Commands;
using Looplex.OpenForExtension.Abstractions.Contexts;
using Looplex.OpenForExtension.Plugins;
using System.Text.Json;

namespace Looplex.Samples.EPTracker;

internal class DefineCommand : IDefineRoles
{
  public Task ExecuteAsync(IContext context, CancellationToken cancellationToken)
  {
    context.State.ExtensionPointsTracker = new List<string>();
    context.State.ExtensionPointsTracker.Add("@define");
    return Task.CompletedTask;
  }
}

internal class BindCommand : IBind
{
  public Task ExecuteAsync(IContext context, CancellationToken cancellationToken)
  {
    context.State.ExtensionPointsTracker.Add("@bind");
    return Task.CompletedTask;
  }
}

internal class BeforeActionCommand : IBeforeAction
{
  public Task ExecuteAsync(IContext context, CancellationToken cancellationToken)
  {
    context.State.ExtensionPointsTracker.Add("@beforeAction");
    return Task.CompletedTask;
  }
}

internal class AfterActionCommand : IAfterAction
{
  public Task ExecuteAsync(IContext context, CancellationToken cancellationToken)
  {
    context.State.ExtensionPointsTracker.Add("@afterAction");
    var expandoo = context.State.ExtensionPointsTracker;
    context.Result = JsonSerializer.Serialize(expandoo, expandoo.GetType());
    return Task.CompletedTask;
  }
}

internal class ReleaseCommand : IReleaseUnmanagedResources
{
  public Task ExecuteAsync(IContext context, CancellationToken cancellationToken)
  {
    context.State.ExtensionPointsTracker.Add("@release");
    return Task.CompletedTask;
  }
}

public class Plugin : AbstractPlugin
{
  public override string Name => "ExtensionPoints Tracker";

  public override string Description => "Plugin básico para demonstrar chamadas em cada um dos extension points definidos pelo DefaultContext";

  public override IEnumerable<ICommand> Commands =>
  [
      new DefineCommand(),
      new BindCommand(),
      new BeforeActionCommand(),
      new AfterActionCommand(),
      new ReleaseCommand()
  ];

  public override IEnumerable<string> GetSubscriptions() => [
    "Notejam.Echo"
  ];
}
using System.Data.Common;

using Looplex.Foundation;
using Looplex.Foundation.Entities;
using Looplex.Foundation.OAuth2;
using Looplex.Foundation.Ports;
using Looplex.OpenForExtension.Abstractions.Commands;
using Looplex.OpenForExtension.Abstractions.ExtensionMethods;
using Looplex.OpenForExtension.Abstractions.Plugins;
using Looplex.Samples.Entities;

namespace Looplex.Samples;

public class Notejam : Service
{
    private readonly IRbacService? _rbacService;
    private readonly IUserContext? _userContext;
    private readonly DbConnection? _db;

    #region Reflectivity

    // ReSharper disable once PublicConstructorInAbstractClass
    public Notejam() : base()
    {
    }

    #endregion

    public Notejam(IList<IPlugin> plugins, IRbacService rbacService, IUserContext userContext, DbConnection db) :
        base(plugins)
    {
        _rbacService = rbacService;
        _userContext = userContext;
        _db = db;
    }

    public List<Pad> Pads { get; } = [];

    public async Task CreateNote(CancellationToken cancellationToken)
    {
        var ctx = NewContext();
        _rbacService!.ThrowIfUnauthorized(_userContext!, GetType().Name, this.GetCallerName());

        await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);

        await ctx.Plugins.ExecuteAsync<IValidateInput>(ctx, cancellationToken);

        await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

        await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);

        await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

        if (!ctx.SkipDefaultAction)
        {
            // TODO construir SP/query, e colocar os parametros no new { }
        }

        await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);

        await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);
    }

    // The purpose of this method is to validate the framework functionalities
    public async Task<string> Echo(string name, CancellationToken cancellationToken)
    {
        var ctx = NewContext();
        _rbacService!.ThrowIfUnauthorized(_userContext!, GetType().Name, this.GetCallerName());

        ctx.State.Name = name;
        await ctx.Plugins.ExecuteAsync<IHandleInput>(ctx, cancellationToken);

        await ctx.Plugins.ExecuteAsync<IValidateInput>(ctx, cancellationToken);

        await ctx.Plugins.ExecuteAsync<IDefineRoles>(ctx, cancellationToken);

        await ctx.Plugins.ExecuteAsync<IBind>(ctx, cancellationToken);

        await ctx.Plugins.ExecuteAsync<IBeforeAction>(ctx, cancellationToken);

        if (!ctx.SkipDefaultAction)
        {
            ctx.Result = $"Hello {ctx.State.Name}";
        }

        await ctx.Plugins.ExecuteAsync<IAfterAction>(ctx, cancellationToken);

        await ctx.Plugins.ExecuteAsync<IReleaseUnmanagedResources>(ctx, cancellationToken);

        return (string)ctx.Result;
    }
}
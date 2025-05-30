using System.Reflection;

using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

using Casbin;

using Looplex.Foundation.Adapters;
using Looplex.Foundation.Adapters.AuthZ.Casbin;
using Looplex.Foundation.Helpers;
using Looplex.Foundation.Ports;
using Looplex.Foundation.WebApp.Middlewares;
using Looplex.OpenForExtension.Abstractions.Plugins;
using Looplex.OpenForExtension.Loader;
using Looplex.Samples.Application.Abstraction;
using Looplex.Samples.Application.Services;
using Looplex.Samples.Infra;
using Looplex.Samples.Infra.CommandHandlers;

using MediatR;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;

using Newtonsoft.Json;

using Polly;
using Polly.Extensions.Http;

namespace Looplex.Samples.WebApp;

public static class Program
{
  public static void Main(string[] args)
  {
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.Services.AddHttpClient("Default")
      .AddPolicyHandler(GetRetryPolicy());

    builder.Services.AddTransient(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Default"));

    builder.Services.AddHealthChecks()
      .AddCheck<HealthCheck>("Default");

    foreach (var item in Files.LoadEnv("config.env"))
    {
      Environment.SetEnvironmentVariable(item.Key, item.Value);
    }

    Dictionary<string, string?> inMemorySettings = new()
    {
      { "Audience", "saas.looplex.com.br" },
      { "Issuer", "dotnet.looplex.com.br" },
      { "TokenExpirationTimeInMinutes", "60" },
      { "OicdAudience", "00000003-0000-0000-c000-000000000000" },
      { "OicdIssuer", "https://sts.windows.net/3c4e959d-cffe-4864-8d09-b0d5cd9ba485" },
      { "OicdTenantId", "3c4e959d-cffe-4864-8d09-b0d5cd9ba485" },
      { "OicdUserInfoEndpoint", "https://graph.microsoft.com/oidc/userinfo" },
      {
        "PublicKey",
        "LS0tLS1CRUdJTiBQVUJMSUMgS0VZLS0tLS0KTUlJQ0lqQU5CZ2txaGtpRzl3MEJBUUVGQUFPQ0FnOEFNSUlDQ2dLQ0FnRUF6bzBKWThaeHZSbW9kMUFJNDhqZgo4eW90KzN4VjBkVDU4NzJudFAxdGxUazdOc1VUSUF2V2ZDVk5HR01SRFhucjF1dVVHZlJVZXFFOVhMRDd4RWR3Cmt3b1gzV1BGOE5SS2ZuUUxuNERRVDNxRXJYbklDL0N2M1VIcGxzRFgyUUJMUi9YcEoyMnQ2elJCdERiNm8zUE8KRml1N2FmNmZkSnNNaUR1dCtxdlFwUWpKNG5oU3VTSkhFTXFaQnRYQitNNU1KMWJESUJ5Y2FDK0luSW9kN3o1UAo3eHNmRU5rU1FMM3JCTmhSeWVOOHRndXVobmZzbkZZbnJMNnB6T3Y0Zjdic2ZRZitVMDVBaUd2WnRJdENoNEx3CitURXFlYlMrb1VaOWRxVnVwNmJ4b0p3dDdFK2c0Q2x3MXREc0tZaTRvR2VyUkM4RkdKYXR4aHg4ZTdaWjNGUmoKYk0zL21HTWw1QnkwM015b09mQ2Y0WnErb2tULzErTEZYek95dGNLYzRVcjBiZm1BaGhqZURVN01ab0hUUVNTMQp1TEhBWGtLaUxmaC9QM1JQdjM4b29FQkk0UUxDa2FoZzhmd2Z6UzIzampBQlV2RXJuTjQyWkIyckFPUkgxZTNwCjhDbnhGdmUyRXdWWTlyVlliY1lPdFF2VS9FNnVmeHhYeW4wNk9icUZWdnlDOTlHMXZXU3pJejRLQjlUN2I2emgKWjJ0UU5IdXNVUHB6bTVFUFBOSVBQRVM0eU95ZFR5VFk4TlZFOHFvb3V6aDRGNyt2VEVVa0Zpc05UZUNHZmZqTQp6czYyOTdJbkJPakxDclluVUl6UDRDYkwwdCtLc0psRll2SE9OR1NIQjNkTUM4SW5wQ1VwbFZaV1JBTnBCNEVoCmcvK1Z4SmVIYWdxT0J1eWhNZUhVdFY4Q0F3RUFBUT09Ci0tLS0tRU5EIFBVQkxJQyBLRVktLS0tLQo="
      },
      {
        "PrivateKey",
        "LS0tLS1CRUdJTiBSU0EgUFJJVkFURSBLRVktLS0tLQpNSUlKS0FJQkFBS0NBZ0VBem8wSlk4Wnh2Um1vZDFBSTQ4amY4eW90KzN4VjBkVDU4NzJudFAxdGxUazdOc1VUCklBdldmQ1ZOR0dNUkRYbnIxdXVVR2ZSVWVxRTlYTEQ3eEVkd2t3b1gzV1BGOE5SS2ZuUUxuNERRVDNxRXJYbkkKQy9DdjNVSHBsc0RYMlFCTFIvWHBKMjJ0NnpSQnREYjZvM1BPRml1N2FmNmZkSnNNaUR1dCtxdlFwUWpKNG5oUwp1U0pIRU1xWkJ0WEIrTTVNSjFiRElCeWNhQytJbklvZDd6NVA3eHNmRU5rU1FMM3JCTmhSeWVOOHRndXVobmZzCm5GWW5yTDZwek92NGY3YnNmUWYrVTA1QWlHdlp0SXRDaDRMdytURXFlYlMrb1VaOWRxVnVwNmJ4b0p3dDdFK2cKNENsdzF0RHNLWWk0b0dlclJDOEZHSmF0eGh4OGU3WlozRlJqYk0zL21HTWw1QnkwM015b09mQ2Y0WnErb2tULwoxK0xGWHpPeXRjS2M0VXIwYmZtQWhoamVEVTdNWm9IVFFTUzF1TEhBWGtLaUxmaC9QM1JQdjM4b29FQkk0UUxDCmthaGc4ZndmelMyM2pqQUJVdkVybk40MlpCMnJBT1JIMWUzcDhDbnhGdmUyRXdWWTlyVlliY1lPdFF2VS9FNnUKZnh4WHluMDZPYnFGVnZ5Qzk5RzF2V1N6SXo0S0I5VDdiNnpoWjJ0UU5IdXNVUHB6bTVFUFBOSVBQRVM0eU95ZApUeVRZOE5WRThxb291emg0RjcrdlRFVWtGaXNOVGVDR2Zmak16czYyOTdJbkJPakxDclluVUl6UDRDYkwwdCtLCnNKbEZZdkhPTkdTSEIzZE1DOElucENVcGxWWldSQU5wQjRFaGcvK1Z4SmVIYWdxT0J1eWhNZUhVdFY4Q0F3RUEKQVFLQ0FnQUozaTBUcWpobTIySDBDVXZUYmhaYzdLZnp1dFh5eDJVRm93cnZGNmh6bDU5ZmwzeTViRGRjQ1FBcwo2UmE0ZVJtdVUrVG9kSWJRc1FGWWUxQWI2WG5VWElnVldKM3RTb2Nna1hTNHN4UEFxRTdNWnVRS3hmM3c2U1E2CndvM05YVGs3ZitFYXRCKzUrKzRqcVBqQ2RGYmxNa09xNWJKQ2hPSE5aR1NFZEU3c2c0WDVudHY4NGtsWTRRVDgKa1p2SndqbGJLOGI0c3NVNktRTXl6MXBzd3FKWS93ZTE2MWoyNU52a2lGMG44d2xUUFMyaVdQcGg0YS9WamVwWQowdkUxVU16dGtFTXpRYXJObWJGMThhMUZBaGwzSGtVME9WRmVMUnJ6WHlYeE5vV1dzYjl6NmIzNjE1Ly9jMDNGCkVqWVgyN3dQN3Rza2VKWm12NWVtNVdoWG9XUjZpVFEycGJUeFB4QjQwVXJFaWVsMU9nellQTHVlQllvdXcwcVcKNndxUklHSzYzZEQzV1p6N0pRMTh1Rk96UnllazlzTU1Gc2Q1MWV2cnBLTHA1c3ZYQzFEMUs4SGZlMmp1NFFTYgpPTDU5MXA4N3daYk5VQ0xoaVdnZGo5RExUejJJMTQ1VGJpazNiMUowd0c0dExMSjZwN0pOUXpLam8zaUVTTTR2CllZWlk4Q3BFNDJZdzd2NXhPejFOYm5YUjZsR1puczhaMUt1NEZ2cXBKZitna2ZtaDBMcVBoRWp2N2ZZaHNwRWwKRmFEWEtCMVk0Z0RLUEZib25wY3pJOEZmdU9SK1Y0R1JucTBGbTdHNmVOeHdxNndCVk55QzFLaEpWb0NLaHVmRApId3VISCtLaEprQmpKZitXQTFXVURNSVdaeGNnbWJpYnNrYnBPRWRZOTlCY0JCeDJpUUtDQVFFQStpRjJDTitQCmVjOUJuWTF1dGRiZWtWZ1lHTWtzM2xGWHViS25EWit6L1hWWFM3bnNQR1JZOXk0VFg3TkVUekxPazUveVlRMXgKS29wQVRuaGgxa1VuTGt0RG5YVXM5ZXdSTDEra2srY09ZMTN2ODZWSHU4SGE5a1hTRkwrdExlYytBaGVlUGJEVgpkd3cxSDRLdWZNblowVUJRbmVFbUNOZUtFV2Z2U1I2ZStPc0wvWkg5a2szemtld3FUSDBpUFp4V0pIUUhMbjVnCjJwNDF4ZGc1dzdGZCt0cEtRU295ZlFMVW1aaXJlRlZnRkFZRlZVbGl1VTJUMmhGMC9TMnI5dVFsbFhvRDFWQ08KL0RXenZQQTlscWFiTk9hZXNsS3M3RHdiN3dza1E0eUJrZDcxaTZDeTYzQTlEbStpd3FCdzVGSGJpYUtEQVBGKwpWeHJTeDdLVXNWcjNhd0tDQVFFQTAyWEtpN0Z4cVdFVDk1OFpxcU1rN1VTcWxkeitmQTVLa0xPWGh0WEE0NHNXClRwNEljSEI4S3haMEJHa2R0dkE0MG42MDAvWlA4Z2pScEYxcHlNd2FESU1zL3ErbkpiVmRrdng2NkZTVW84QTMKMVg4NktHaDJLVTNpQ25ReTRzWEdsc25HTXJ0SEtPd3dJMTlpL0hDbkRMY1lvODlTMm1ydWQrSk5GVWJMR3N3VQo1cHlSWFlaeUFoWnpxMkszdHhqcm9lS1ZmSG1yak1NbmI3Mk1INW9sRU5sOHVGVENwSStUb2JuaEYxY3M4eTdjCjZmbWpkdm4vWDNoNTRoUVpzUnY5TWJrajYxYk9LMjU5Rzg3ZFVOem5BcFo5ditSZkJjSk9BWU1LVnVPZGt0MlcKLzkyVUVOMDg1WkxLbVA2ZTJ5Y3JkUjJUUjJPdGRyeEhselhEQWRUYTNRS0NBUUFJRkc2Z3FNQVV1am92WWJNeQowb2NNQU9GK1kzazhrVG9aT0lrbTZvTEE3RHB1cXNuVHhaWU9IZ0hvTkgwL1phL0Ftd2tVVTMvVlZQUHcxUGlzCkdEM0V4QStpRlhmblZjSFVXdTJSRFlTc2R3dGFQbnVMdUI1Zm1DL0tGY3I5VVp2eUsxc2tPUU1jUGx1MDhkNjcKRHpZbkNVSFJaOFYzd2FhbkcwbGlma2U2V0xWaGNvYlRaQXM3S25yUFQ3ZDhjQUZrV2c4bFZGWENtaHU0a1Z5RQpVc1ZyWmdQQ0NSL3FZOTRFUENkRjB0UXdzV1VZdFM4b056WlFkQUhvYjhJL1RtMWNYNzJoOVdFNUNtOE02bUhDCjdRelViNkt1dGZiNkJwTU1iTHEySitMRG1JVXNCbnZoR1JZUDBsRmFvaDRqY2ZWNmc3SmhwR3RsV3V0MklmR2wKclArVEFvSUJBQnNPZURJUTcvOUIwWkJyNkprU2NITjl5bWVMOVhaalRkL2ZYSkdCWWtLOVZvbVJhSHNicW9qSgpRdWdkbkJRQ0F3UE4rODcxUTU0eXlzSkN3bnRVeERDOXlWQi9vdUVNcVlGYWwxQ05Jc2tpblFMU1dkczZJNFY5CjFtU0lJc3NyalJOWGwrcCtWY2xERVpZMWF6SHNLVFZUYWUvdFpTbFhibFlodk1Qd1g2WjZZR0p6djVjODBmSzYKZE91R293VG9SNkJjbzgxZXRUbzY4QjA1SVdvYURJeHZpYXIrRGp1SnROZUtOdWtKMjFMMHVJbXB6ZVk0Y1JzRApESGFISkJLckJta0t6VElkMWgxdzhzcFFXN3N4eXM5bCs0cEg3SEdNUVBlb2tmWTFBOEg4WW9zMURQSnJucE96CmlnS3k5Qm9RbFZ0VUFibXRaRkpDSHRlWHBmSGlFSEVDZ2dFQkFPNDFFazBqY3BFVmh6RXcvYWZha2tOMHl6TW8KYnIzWGw1T0pvazBMdTQ5T3JPWU5wRFJVMloyNWU0SUpiQ05iMDlONnhQZWw1ZkNFR0cwNlU2anZ4YnNocmRNTQorSWtJa1lIUDdXYkJuc0R0TllISFFBcmQ4YWVmOXZ0RmRXbXNJUDVJb2E2Vm5OZDRzQ0FFdVFBNVBhcDdBYW8rClJ4V3BiRVFRd0FQaGZCaDhDSVhaeER3WSs2WVphMHVDWDJQRm9nTGRhKzhBZ2hhaTVnTkZjbFRVMHYvTjJ4U0MKYVJ1WStoT1duMWRHZ2xMOHRucEZ4Ym52SUNoVUk5ZzZGWlFjYzV1RXp4Q0hnbWY0ZVRVSzlvT09MWkx3L2hpNgpQWjFnNHgzbWx4VlBLVmZDTzFPRmlGSzRscDdjbXJoNExDMWw2R2RWWUdzcUc4OHhCek9GNWZFNzl2Yz0KLS0tLS1FTkQgUlNBIFBSSVZBVEUgS0VZLS0tLS0K"
      },
      { "ClientSecretByteLength", "72" },
      { "ClientSecretDigestCost", "4" }
    };
    builder.Configuration.AddInMemoryCollection(inMemorySettings);
    builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
    builder.Services.AddSingleton<ISecretsService>(sp =>
    {
      var keyVaultUrl = new Uri(builder.Configuration["KeyVaultUrl"]!);
      var retryPolicy = Policy.NoOpAsync<string>();
      // get credentials from ENV
      var secretClient = new SecretClient(keyVaultUrl, new DefaultAzureCredential());
      var logger = sp.GetRequiredService<ILogger<AzureSecretsService>>();

      return new AzureSecretsService(secretClient, retryPolicy, logger);
    });
    builder.Services.AddSingleton<IDbConnections, DbConnections>();
    builder.Services.AddOAuth2(builder.Configuration);
    builder.Services.AddSCIMv2();
    builder.Services.AddAuthZ(InitRbacEnforcer());

    builder.Services.AddScoped<Notes>(sp =>
    {
      PluginLoader loader = new();
      IEnumerable<string> dlls = Directory.Exists("plugins")
        ? Directory.GetFiles("plugins").Where(x => x.EndsWith(".dll"))
        : [];
      IList<IPlugin> plugins = loader.LoadPlugins(dlls).ToList();
      var rbacService = sp.GetRequiredService<IRbacService>();
      var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
      var mediator = sp.GetRequiredService<IMediator>();
      return new Notes(plugins, rbacService, httpContextAccessor, mediator);
    });

    builder.Services.AddMediatR(cfg =>
      cfg.RegisterServicesFromAssembly(typeof(UpdateNoteCommandHandler).Assembly));

    WebApplication app = builder.Build();

    app.MapHealthChecks("/health", new HealthCheckOptions
      {
        ResponseWriter = async (context, report) =>
        {
          context.Response.ContentType = "application/json; charset=utf-8";
          string result = JsonConvert.SerializeObject(new
          {
            status = report.Status.ToString(),
            results = report.Entries.Select(e => new
            {
              key = e.Key,
              status = e.Value.Status.ToString(),
              description = e.Value.Description,
              data = e.Value.Data,
              exception = e.Value.Exception?.Message // Include exception details
            })
          });
          await context.Response.WriteAsync(result);
        }
      })
      .AllowAnonymous();

    app.UseOAuth2();
    app.UseSCIMv2();

    app.Run();
  }

  private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
  {
    return HttpPolicyExtensions
      .HandleTransientHttpError() // Handle transient errors (5xx, 408, etc.)
      .WaitAndRetryAsync(3,
        retryAttempt =>
          TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))); // Retry 3 times with exponential backoff
  }

  private static IEnforcer InitRbacEnforcer()
  {
    string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                 ?? throw new InvalidOperationException("Could not determine directory");
    string modelPath = Path.Combine(dir, "rbac", "model.ini");
    string policyPath = Path.Combine(dir, "rbac", "policy.csv");

    return new Enforcer(modelPath, policyPath);
  }
}
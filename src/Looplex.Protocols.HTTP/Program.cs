using Looplex.Protocols.HTTP.Middlewares;
using Looplex.Protocols.HTTP.Adapters;
using Looplex.Protocols.HTTP.Ports;
using Looplex.SCIMv2.Helpers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();

// Register OAuth2 services
builder.Services.AddSingleton<IJwtService, JwtServiceAdapter>();
builder.Services.AddSingleton<IGrantTypeService, GrantTypeServiceAdapter>();

// Register SCIMv2 services
builder.Services.AddSCIMv2Service();
builder.Services.AddSingleton<Looplex.SCIMv2.Ports.ISCIMv2, Looplex.SCIMv2.SCIMv2>();
builder.Services.AddSingleton<ISCIMv2Service, SCIMv2ServiceAdapter>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

// Map OAuth2 endpoints
app.MapOAuth2TokenEndpoint();
app.MapOAuth2UserInfoEndpoint();

// Map SCIMv2 endpoints
app.MapSCIMv2UsersEndpoint();
app.MapSCIMv2GroupsEndpoint();
app.MapSCIMv2ServiceProviderConfigEndpoint();

app.MapControllers();

app.Run();
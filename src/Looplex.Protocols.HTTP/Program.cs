using Looplex.SCIMv2;
using Looplex.SCIMv2.Extensions;
using Looplex.Protocols.HTTP.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add SCIMv2 services
builder.Services.AddSCIMv2Service();

// Add CORS for testing
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();

// Add SCIMv2 endpoints
app.UseSCIMv2("Users", authorize: false);
app.UseSCIMv2("Groups", authorize: false);
app.UseSCIMv2("Api-Keys", authorize: false);

// Add SCIMv2 discovery endpoints
app.UseSCIMv2Discovery(authorize: false);

// Add health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Add root endpoint with API information
app.MapGet("/", () => Results.Ok(new 
{ 
    name = "Looplex Foundation SCIMv2 API",
    version = "1.0.0",
    description = "SCIMv2 Protocol Implementation",
    endpoints = new
    {
        schemas = "/Schemas",
        serviceProviderConfig = "/ServiceProviderConfig",
        users = "/Users",
        groups = "/Groups",
        apiKeys = "/Api-Keys",
        health = "/health"
    }
}));

app.Run();


using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Azure;
using AspNetCoreRateLimit;
using Microsoft.ApplicationInsights.Extensibility;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json").AddEnvironmentVariables();

builder.Services.AddControllers(options =>
{
    options.RespectBrowserAcceptHeader = true;
});

builder.Services.AddLogging(config =>
{
    config.AddDebug();
    config.AddConsole();
});

builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddSingleton<ITelemetryInitializer, CloudRoleNameTelemetryInitializer>();

builder.Services.AddOptions();
bool enableRateLimiting = builder.Configuration.GetValue<bool>("IpRateLimiting:EnableEndpointRateLimiting");
Console.WriteLine("Rate limiting is set to: " + enableRateLimiting);

if (enableRateLimiting)
{
    builder.Services.AddMemoryCache();
    builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
    builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
    builder.Services.AddInMemoryRateLimiting();
    builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
}

builder.Services.AddSingleton(
    s =>
    {
        return new Microsoft.Azure.Cosmos.Fluent.CosmosClientBuilder(builder.Configuration.GetConnectionString("CosmosApi"))
            .WithSerializerOptions(new Microsoft.Azure.Cosmos.CosmosSerializationOptions()
            {
                PropertyNamingPolicy = Microsoft.Azure.Cosmos.CosmosPropertyNamingPolicy.CamelCase
            })
            .WithBulkExecution(false)
            .WithThrottlingRetryOptions(TimeSpan.FromSeconds(1), 1)
            .Build();
    }
);

builder.Services.AddSingleton<IMessageStorageService, MessageCosmosSqlStorageService>();

var app = builder.Build();

app.MapControllers();

if (enableRateLimiting)
{
    app.UseIpRateLimiting();
}
app.Run();

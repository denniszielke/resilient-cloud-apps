using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Azure;
using Contonance.Backend.Background;
using Contonance.Backend.Clients;
using Contonance.Backend.Repositories;
using Contonance.Extensions;
using Microsoft.FeatureManagement;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

var builder = WebApplication.CreateBuilder(args);

IConfigurationRefresher configurationRefresher = null;
builder.Configuration
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .AddAzureAppConfiguration(options =>
    {
        options
            .Connect(builder.Configuration.GetValue<string>("AppConfiguration:ConnectionString"))
            .UseFeatureFlags(options =>
            {
                options.CacheExpirationInterval = TimeSpan.FromSeconds(2);
            });

        configurationRefresher = options.GetRefresher();
    });
builder.Services.AddSingleton(configurationRefresher!);

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
builder.Services.AddSingleton<ITelemetryInitializer>(_ => new CloudRoleNameTelemetryInitializer("Contonance.Backend"));

builder.Services.AddAzureClients(b =>
{
    b.AddBlobServiceClient(builder.Configuration.GetValue<string>("EventHub:BlobConnectionString"));
});

builder.Services.AddSingleton<RepairReportsRepository, RepairReportsRepository>();
builder.Services.AddSingleton<EnterpriseWarehouseClient, EnterpriseWarehouseClient>();

builder.Services.AddAzureAppConfiguration();
builder.Services.AddFeatureManagement();
builder.Services
    .AddHttpClient<EnterpriseWarehouseClient>()
    .AddPolicyConfiguration(EnterpriseWarehouseClient.SelectPolicy, builder.Configuration);

builder.Services.AddHostedService<EventConsumer>();

var app = builder.Build();

app.UseAzureAppConfiguration();

app.MapControllers();

app.Run();
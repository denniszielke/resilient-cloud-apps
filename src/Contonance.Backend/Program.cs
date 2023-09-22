using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Azure;
using Contonance.Backend.Background;
using Contonance.Backend.Clients;
using Contonance.Backend.Repositories;
using Contonance.Extensions;
using Microsoft.FeatureManagement;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .AddAzureAppConfiguration(options =>
    {
        options.Connect(builder.Configuration
            .GetValue<string>("AppConfiguration:ConnectionString"))
            .UseFeatureFlags(options =>
            {
                options.Select("Contonance.Backend:*");
                options.CacheExpirationInterval = TimeSpan.FromSeconds(2);
            });
    });

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
    // .AddPolicyConfiguration(EnterpriseWarehouseClient.SelectPolicy, builder.Configuration);
    .AddPolicyHandler((services, request) =>
            {
                var testBackendSetting = builder.Configuration.GetValue<bool>("featureManagement:Contonance.Backend:InjectRateLimitingFaults");
                var testWebPortalSetting = builder.Configuration.GetValue<bool>("featureManagement:Contonance.WebPortal.Server:EnableCircuitBreakerPolicy");

                var featureManager = services.GetService<IFeatureManager>();
                var testFFs = featureManager.GetFeatureNamesAsync().ToBlockingEnumerable().ToList();
                var testBackendSetting2 = featureManager.IsEnabledAsync("Contonance.Backend:InjectRateLimitingFaults").Result;
                var testWebPortalSetting2 = featureManager.IsEnabledAsync("Contonance.WebPortal.Server:EnableCircuitBreakerPolicy").Result;

                var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryWithLoggingAsync(new[]
                {
                    TimeSpan.FromSeconds(0.5),
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5)
                })
                .WithPolicyKey($"{nameof(EnterpriseWarehouseClient)}RetryPolicy");
                return retryPolicy;
            }
            );

builder.Services.AddHostedService<EventConsumer>();

var app = builder.Build();

app.UseAzureAppConfiguration();

var test = builder.Configuration.GetDebugView();
var testFeatures = builder.Configuration.GetSection("featureManagement").Exists();
var testBackendSetting = builder.Configuration.GetValue<bool>("featureManagement:Contonance.Backend:InjectRateLimitingFaults");
var testWebPortalSetting = builder.Configuration.GetValue<bool>("featureManagement:Contonance.WebPortal.Server:EnableCircuitBreakerPolicy");


var featureManager = app.Services.GetService<IFeatureManager>();
var testFFs = featureManager.GetFeatureNamesAsync().ToBlockingEnumerable().ToList();
var testBackendSetting2 = featureManager.IsEnabledAsync("Contonance.Backend:InjectRateLimitingFaults").Result;
var testWebPortalSetting2 = featureManager.IsEnabledAsync("Contonance.WebPortal.Server:EnableCircuitBreakerPolicy").Result;

app.MapControllers();

app.Run();
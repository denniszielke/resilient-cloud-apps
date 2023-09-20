using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Azure;
using Contonance.Backend.Background;
using Polly;
using Contonance.Backend.Clients;
using Microsoft.ApplicationInsights.Extensibility;
using Polly.Extensions.Http;
using Polly.Contrib.Simmy;
using System.Net;
using Polly.Contrib.Simmy.Outcomes;
using Contonance.Backend.Repositories;

var builder = WebApplication.CreateBuilder(args);

const string FEATURE_FLAG_PREFIX = "featureManagement:Contonance.Backend";

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

builder.Services.AddAzureClients(b =>
{
    b.AddBlobServiceClient(builder.Configuration.GetValue<string>("EventHub:BlobConnectionString"));
});

builder.Services.AddSingleton<RepairReportsRepository, RepairReportsRepository>();
builder.Services.AddSingleton<EnterpriseWarehouseClient, EnterpriseWarehouseClient>();

builder.Configuration.AddAzureAppConfiguration(options =>
{
    options.Connect(builder.Configuration.GetValue<string>("AppConfiguration:ConnectionString"))
        .UseFeatureFlags(options =>
        {
            options.CacheExpirationInterval = TimeSpan.FromSeconds(2);
        });
});

builder.Services.AddAzureAppConfiguration();

var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(new[]
    {
        TimeSpan.FromSeconds(0.5),
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(5)
    });

var breakerPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
    .CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 3,
        durationOfBreak: TimeSpan.FromSeconds(5)
    );

var result = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
var chaosPolicy = MonkeyPolicy.InjectResultAsync<HttpResponseMessage>(with =>
    with.Result(result)
        .InjectionRate(0.5)
        .Enabled()
);

var noOpPolicy = Policy.NoOpAsync().AsAsyncPolicy<HttpResponseMessage>();

builder.Services.AddHttpClient("Sink", client =>
{
    client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("ENTERPRISE_WAREHOUSE_BACKEND_URL"));
}).AddPolicyHandler(request =>
{

    var policies = new List<IAsyncPolicy<HttpResponseMessage>>();

    bool enableRetry = builder.Configuration.GetValue<bool>($"{FEATURE_FLAG_PREFIX}:EnableRetry");
    bool enableBreaker = builder.Configuration.GetValue<bool>($"{FEATURE_FLAG_PREFIX}:EnableBreaker");
    bool enableRateLimiting = builder.Configuration.GetValue<bool>($"{FEATURE_FLAG_PREFIX}:EnableRateLimiting");

    if (enableRetry)
    {
        policies.Add(retryPolicy);
    }

    if (enableBreaker)
    {
        policies.Add(breakerPolicy);
    }

    if (enableRateLimiting)
    {
        policies.Add(chaosPolicy);
    }

    if (policies.Count == 0)
    {
        policies.Add(noOpPolicy);
    }

    //Policy.WrapAsync throws an error if only one policy is passed in
    return policies.Count < 2 ? policies[0] : Policy.WrapAsync(policies.ToArray());
}
);

builder.Services.AddHostedService<EventConsumer>();

var app = builder.Build();

app.UseAzureAppConfiguration();

app.MapControllers();

app.Run();

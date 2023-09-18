using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Azure;
using Polly;
using Message.Creator.Clients;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Polly.Extensions.Http;
using Message.Creator;

const string FEATURE_FLAG_PREFIX = "featureManagement:Message.Creator";

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

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.WriteIndented = true;
});

builder.Services.AddAzureClients(b =>
{
    b.AddEventHubProducerClient(builder.Configuration.GetValue<string>("EventHub:EventHubConnectionString"), builder.Configuration.GetValue<string>("EventHub:EventHubName"));
});

builder.Services.AddSingleton<SinkClient, SinkClient>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Configuration.AddAzureAppConfiguration(options =>
{
    options.Connect(builder.Configuration.GetValue<string>("AppConfiguration:ConnectionString"))
        .UseFeatureFlags(options => {
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
    .CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 3,
        durationOfBreak: TimeSpan.FromSeconds(30)
    );

var noOpPolicy = Policy.NoOpAsync().AsAsyncPolicy<HttpResponseMessage>();

builder.Services.AddHttpClient("Sink", client =>
{
    client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("RECEIVER_URL"));
}).AddPolicyHandler(request => {

    bool enableRetry = builder.Configuration.GetValue<bool>($"{FEATURE_FLAG_PREFIX}:EnableRetry");
    bool enableBreaker = builder.Configuration.GetValue<bool>($"{FEATURE_FLAG_PREFIX}:EnableBreaker");

    Console.WriteLine("Retry is set to: " + enableRetry);
    Console.WriteLine("Breaker is set to: " + enableBreaker);

    if (!enableBreaker && enableRetry) {
        return retryPolicy;
    } else if (enableBreaker && !enableRetry) {
        return breakerPolicy;
    } else if (enableBreaker && enableRetry) {
        return Policy.WrapAsync(retryPolicy, breakerPolicy);
    } else {
       return  noOpPolicy;
    }
}
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAzureAppConfiguration();

app.MapControllers();

app.UseStaticFiles();
var options = new DefaultFilesOptions();
options.DefaultFileNames.Clear();
options.DefaultFileNames.Add("index.html");
app.UseDefaultFiles(options);

app.Run();

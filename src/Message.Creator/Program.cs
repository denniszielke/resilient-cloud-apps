using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Azure;
using Polly;
using Azure.Messaging.EventHubs;
using Message.Creator.Clients;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.FileProviders;

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

bool enableRetry = builder.Configuration.GetValue<bool>("HttpClient:EnableRetry");
bool enableBreaker = builder.Configuration.GetValue<bool>("HttpClient:EnableBreaker");

Console.WriteLine("Retry is set to: " + enableRetry);
Console.WriteLine("Breaker is set to: " + enableBreaker);

if (!enableBreaker && !enableRetry){
    builder.Services.AddHttpClient("Sink", client =>
    {
        client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("RECEIVER_URL"));
    });
}else if (!enableBreaker && enableRetry){
    builder.Services.AddHttpClient("Sink", client =>
    {
        client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("RECEIVER_URL"));
    }).AddTransientHttpErrorPolicy(b => b.WaitAndRetryAsync(new[]
    {
        TimeSpan.FromSeconds(0.5),
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(5)
    }));
}else if (enableBreaker && !enableRetry){
    builder.Services.AddHttpClient("Sink", client =>
    {
        client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("RECEIVER_URL"));
    }).AddTransientHttpErrorPolicy(builder => builder.CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 3,
        durationOfBreak: TimeSpan.FromSeconds(30)
    ));
}else if (enableBreaker && enableRetry){
    builder.Services.AddHttpClient("Sink", client =>
    {
        client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("RECEIVER_URL"));
    })
    .AddTransientHttpErrorPolicy(builder => builder.WaitAndRetryAsync(new[]
    {
        TimeSpan.FromSeconds(0.5),
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(5)
    }))
    .AddTransientHttpErrorPolicy(builder => builder.CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 3,
        durationOfBreak: TimeSpan.FromSeconds(30)
    ));
}

var app = builder.Build();

app.UseStaticFiles("/wwwroot");
var options = new DefaultFilesOptions();
options.DefaultFileNames.Clear();
options.DefaultFileNames.Add("index.html");
app.UseDefaultFiles(options);

app.MapControllers();

app.Run();

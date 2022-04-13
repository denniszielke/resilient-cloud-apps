using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Azure;
using Message.Receiver.Background;
using Polly;
using Message.Receiver.Clients;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
builder.Configuration.AddEnvironmentVariables();

// Add services to the container.

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

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.WriteIndented = true;
});

builder.Services.AddAzureClients(b =>
{
    b.AddBlobServiceClient(builder.Configuration.GetValue<string>("EventHub:BlobConnectionString"));
});

builder.Services.AddSingleton<SinkClient, SinkClient>();

builder.Services.AddHttpClient("Sink", client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
});

builder.Services.AddHttpClient("Sink_WithRetry", client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
}).AddTransientHttpErrorPolicy(b => b.WaitAndRetryAsync(new[]
{
    TimeSpan.FromSeconds(0.5),
    TimeSpan.FromSeconds(1),
    TimeSpan.FromSeconds(5)
}));

builder.Services.AddHttpClient("Sink_WithRetryANdCircuitBreaking", client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
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

builder.Services.AddHostedService<EventConsumer>();

var app = builder.Build();

app.MapControllers();

app.Run();

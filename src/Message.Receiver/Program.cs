using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Azure;
using Message.Receiver.Background;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
builder.Configuration.AddEnvironmentVariables();

// Add services to the container.

builder.Services.AddControllers(options =>
{
    options.RespectBrowserAcceptHeader = true;
});

builder.Services.AddLogging(config => {
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

builder.Services.AddAzureClients( b => { 
    b.AddEventHubConsumerClient(builder.Configuration.GetValue<string>("EventHub:EventHubName"), builder.Configuration.GetValue<string>("EventHub:EventHubConnectionString"));
    b.AddBlobServiceClient(builder.Configuration.GetValue<string>("EventHub:BlobConnectionString"));
});

builder.Services.AddHostedService<EventConsumer>();

var app = builder.Build();

app.MapControllers();

app.Run();

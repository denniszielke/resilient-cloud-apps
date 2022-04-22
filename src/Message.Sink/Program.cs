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

builder.Services.AddLogging(config => {
    config.AddDebug();
    config.AddConsole();
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddSingleton<ITelemetryInitializer, CloudRoleNameTelemetryInitializer>();

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.WriteIndented = true;
});

builder.Services.AddOptions();
bool enableRateLimiting = builder.Configuration.GetValue<bool>("IpRateLimiting:EnableEndpointRateLimiting");
Console.WriteLine("Rate limiting is set to: " + enableRateLimiting);

if (enableRateLimiting){
    builder.Services.AddMemoryCache();
    builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
    builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
    builder.Services.AddInMemoryRateLimiting();
    builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
}

builder.Services.AddSingleton( 
    s => {
        return new Microsoft.Azure.Cosmos.Fluent.CosmosClientBuilder(builder.Configuration.GetConnectionString("CosmosApi"))
        .WithSerializerOptions( new Microsoft.Azure.Cosmos.CosmosSerializationOptions( ){
            PropertyNamingPolicy = Microsoft.Azure.Cosmos.CosmosPropertyNamingPolicy.CamelCase               
        }).WithBulkExecution(false)
        .WithThrottlingRetryOptions( TimeSpan.FromSeconds(1), 1)
            .Build();
    }
);

// builder.Services.AddAzureClients( b => { 
//     b.AddTableServiceClient(builder.Configuration.GetConnectionString("CosmosTableApi"));
// });

builder.Services.AddSingleton<IMessageStorageService, MessageCosmosSqlStorageService>();
// builder.Services.AddSingleton<IMessageStorageService, MessageStorageService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

if (builder.Configuration.GetValue<bool>("IpRateLimiting:EnableEndpointRateLimiting") == true){
    app.UseIpRateLimiting();
}
app.Run();

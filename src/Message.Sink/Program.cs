using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Azure;

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

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplicationInsightsTelemetry();

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.WriteIndented = true;
});

// builder.Services.AddSingleton( 
//     s => {
//         return new Microsoft.Azure.Cosmos.Fluent.CosmosClientBuilder(builder.Configuration.GetConnectionString("CosmosApi"))
//             .Build();
//     }
// );

builder.Services.AddAzureClients( b => { 
    b.AddTableServiceClient(builder.Configuration.GetConnectionString("CosmosTableApi"));
});

// builder.Services.AddSingleton<IMessageStorageService, MessageCosmosStorageService>();
builder.Services.AddSingleton<IMessageStorageService, MessageStorageService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();

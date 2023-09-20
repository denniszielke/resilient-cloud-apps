using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Azure;
using Azure;
using Contonance.Extensions;
using Contonance.WebPortal.Server.Clients;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    // Workaround because blazorwasm debugger does not support envFile
    var root = Directory.GetCurrentDirectory();
    var dotenv = Path.Combine(root, "../../../local.env");
    DotEnv.Load(dotenv);
}

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true, true)
    .AddEnvironmentVariables()
    .AddAzureAppConfiguration(options =>
{
    options.Connect(builder.Configuration
        .GetValue<string>("AppConfiguration:ConnectionString"))
        .UseFeatureFlags(options =>
        {
            options.CacheExpirationInterval = TimeSpan.FromSeconds(2);
        });
});

builder.Services.AddLogging(config =>
{
    config.AddDebug();
    config.AddConsole();
});
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddSingleton<ITelemetryInitializer>(_ => new CloudRoleNameTelemetryInitializer("Contonance.WebPortal.Server"));


builder.Services.AddAzureAppConfiguration();
builder.Services.AddAzureClients(b =>
{
    b.AddEventHubProducerClient(builder.Configuration.GetValue<string>("EventHub:EventHubConnectionString"), builder.Configuration.GetValue<string>("EventHub:EventHubName"));
    b.AddOpenAIClient(new Uri(builder.Configuration.GetNoEmptyStringOrThrow("AzureOpenAiServiceEndpoint")), new AzureKeyCredential(builder.Configuration.GetNoEmptyStringOrThrow("AzureOpenAiKey")));
});

builder.Services
    .AddHttpClient<ContonanceBackendClient>()
    .AddPolicyConfiguration(ContonanceBackendClient.SelectPolicy, builder.Configuration);

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.WebHost.UseStaticWebAssets();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseAzureAppConfiguration();

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
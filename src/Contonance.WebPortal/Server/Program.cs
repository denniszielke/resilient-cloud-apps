using System.Net;
using System.Text.Json;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Azure;
using Azure;
using Contonance.WebPortal.Server.Clients;
using Polly;
using Polly.Contrib.Simmy;
using Polly.Contrib.Simmy.Outcomes;
using Polly.Extensions.Http;

const string FEATURE_FLAG_PREFIX = "featureManagement:Message.Creator";

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
builder.Services.AddSingleton<ITelemetryInitializer, CloudRoleNameTelemetryInitializer>();

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.WriteIndented = true;
});

builder.Services.AddAzureAppConfiguration();
builder.Services.AddAzureClients(b =>
{
    b.AddEventHubProducerClient(builder.Configuration.GetValue<string>("EventHub:EventHubConnectionString"), builder.Configuration.GetValue<string>("EventHub:EventHubName"));
    b.AddOpenAIClient(new Uri(builder.Configuration.GetNoEmptyStringOrThrow("AzureOpenAiServiceEndpoint")), new AzureKeyCredential(builder.Configuration.GetNoEmptyStringOrThrow("AzureOpenAiKey")));
});
builder.Services.AddSingleton<ContonanceBackendClient, ContonanceBackendClient>();

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
        durationOfBreak: TimeSpan.FromSeconds(30)
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
    client.BaseAddress = new Uri(builder.Configuration.GetNoEmptyStringOrThrow("CONTONANCE_BACKEND_URL"));
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

    //Policy.WrapAsync throws an error if only one policy is passed in
    return policies.Count < 2 ? noOpPolicy : Policy.WrapAsync(policies.ToArray());
}
);

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
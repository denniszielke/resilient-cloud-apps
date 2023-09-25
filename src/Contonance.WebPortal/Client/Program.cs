using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Contonance.WebPortal.Client;
using BlazorApplicationInsights;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<OnlineStatusInterop>();
builder.Services.AddScoped<AppInsightsInterop>();

builder.Services.AddBlazorApplicationInsights(async applicationInsights =>
    {
        var telemetryItem = new TelemetryItem()
        {
            Tags = new Dictionary<string, object>()
            {
                { "ai.cloud.role", "Contonance.WebPortal.Client" },
            }
        };
        await applicationInsights.AddTelemetryInitializer(telemetryItem);
    }, addILoggerProvider: true, enableAutoRouteTracking: false);

await builder.Build().RunAsync();

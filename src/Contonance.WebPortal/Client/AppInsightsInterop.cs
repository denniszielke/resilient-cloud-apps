using Microsoft.JSInterop;

public class AppInsightsInterop
{
    private readonly IJSRuntime _jsRuntime;

    public AppInsightsInterop(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<string> SetNewTraceIdAsync()
               => await _jsRuntime.InvokeAsync<string>("blazorApplicationInsights.setNewTraceId");

    public async Task<string> GetCurrentTraceIdAsync()
               => await _jsRuntime.InvokeAsync<string>("blazorApplicationInsights.getCurrentTraceId");
}
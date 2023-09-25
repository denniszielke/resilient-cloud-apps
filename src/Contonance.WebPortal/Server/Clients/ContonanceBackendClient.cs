using System.Text.Json;
using Contonance.Shared;
using Polly.Extensions.Http;
using Polly;
using System.Net;
using Polly.Contrib.Simmy;
using Polly.Contrib.Simmy.Outcomes;
using Polly.Contrib.Simmy.Latency;
using Contonance.Extensions;

namespace Contonance.WebPortal.Server.Clients;

public class ContonanceBackendClient
{
    const string FEATURE_FLAG_PREFIX = "featureManagement:Contonance.WebPortal.Server";

    private readonly HttpClient _httpClient;
    private readonly ILogger<ContonanceBackendClient> _logger;

    public ContonanceBackendClient(HttpClient httpClient, IConfiguration configuration, ILogger<ContonanceBackendClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(configuration.GetNoEmptyStringOrThrow("CONTONANCE_BACKEND_URL"));
    }

    internal static void SelectPolicy(IHttpClientBuilder builder, IConfiguration configuration)
    {
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryWithLoggingAsync(new[]
            {
                TimeSpan.FromSeconds(1.5),
                TimeSpan.FromSeconds(3),
                TimeSpan.FromSeconds(8)
            })
            .WithPolicyKey($"{nameof(ContonanceBackendClient)}RetryPolicy");


        var breakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(30)
            );

        var injectRateLimitingFaultsPolicy = MonkeyPolicy.InjectResultAsync<HttpResponseMessage>(with =>
            with.ResultAndLog(new HttpResponseMessage(HttpStatusCode.TooManyRequests), LogLevel.Error)
                .InjectionRate(0.7)
                .Enabled()
            );

        var injectLatencyFaultsPolicy = MonkeyPolicy.InjectLatencyAsync<HttpResponseMessage>(with =>
            with.Latency(TimeSpan.FromSeconds(10))
                .InjectionRate(0.7)
                .Enabled()
            );

        var noOpPolicy = Policy.NoOpAsync().AsAsyncPolicy<HttpResponseMessage>();

        builder.AddPolicyHandler((services, request) =>
            {
                var logger = services.GetService<ILogger<ContonanceBackendClient>>()!;
                request
                    .GetPolicyExecutionContext()
                    .WithLogger(logger);

                // Note: recommended way of ordering policies: https://github.com/App-vNext/Polly/wiki/PolicyWrap#ordering-the-available-policy-types-in-a-wrap
                var policies = new List<IAsyncPolicy<HttpResponseMessage>>();
                policies.AddForFeatureFlag(configuration, $"{FEATURE_FLAG_PREFIX}:EnableRetryPolicy", retryPolicy);
                policies.AddForFeatureFlag(configuration, $"{FEATURE_FLAG_PREFIX}:EnableCircuitBreakerPolicy", breakerPolicy);
                policies.AddForFeatureFlag(configuration, $"{FEATURE_FLAG_PREFIX}:InjectRateLimitingFaults", injectRateLimitingFaultsPolicy);
                policies.AddForFeatureFlag(configuration, $"{FEATURE_FLAG_PREFIX}:InjectLatencyFaults", injectLatencyFaultsPolicy);

                if (policies.Count == 0)
                {
                    return noOpPolicy;
                }
                return policies.Wrap();
            }
            );
    }

    public async Task<IList<RepairReport>> GetAllRepairReports()
    {
        var response = await _httpClient.GetAsync("/api/repairreports");
        _logger.LogDebug(response.StatusCode.ToString());

        string responseBody = await response.Content.ReadAsStringAsync();
        _logger.LogDebug(responseBody);

        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<IList<RepairReport>>(responseBody, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    }
}
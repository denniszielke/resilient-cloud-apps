using System.Text.Json;
using Contonance.Shared;
using Polly.Extensions.Http;
using Polly;
using System.Net;
using Polly.Contrib.Simmy;
using Polly.Contrib.Simmy.Outcomes;
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

        builder.AddPolicyHandler(request =>
            {
                // Note: recommended way of ordering policies: https://github.com/App-vNext/Polly/wiki/PolicyWrap#ordering-the-available-policy-types-in-a-wrap
                var policies = new List<IAsyncPolicy<HttpResponseMessage>>();
                policies.AddForFeatureFlag(configuration, $"{FEATURE_FLAG_PREFIX}:EnableRetryPolicy", retryPolicy);
                policies.AddForFeatureFlag(configuration, $"{FEATURE_FLAG_PREFIX}:EnableCircuitBreakerPolicy", breakerPolicy);
                policies.AddForFeatureFlag(configuration, $"{FEATURE_FLAG_PREFIX}:InjectRateLimitingFaults", chaosPolicy);

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
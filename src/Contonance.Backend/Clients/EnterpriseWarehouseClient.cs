using Polly.Extensions.Http;
using Polly;
using System.Net;
using Polly.Contrib.Simmy;
using Polly.Contrib.Simmy.Outcomes;
using Contonance.Extensions;

namespace Contonance.Backend.Clients
{
    public class EnterpriseWarehouseClient
    {
        const string FEATURE_FLAG_PREFIX = "featureManagement:Contonance.Backend";

        private readonly HttpClient _httpClient;
        private readonly ILogger<EnterpriseWarehouseClient> _logger;

        public EnterpriseWarehouseClient(HttpClient httpClient, IConfiguration configuration, ILogger<EnterpriseWarehouseClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            _httpClient.BaseAddress = new Uri(configuration.GetNoEmptyStringOrThrow("ENTERPRISE_WAREHOUSE_BACKEND_URL"));
        }

        internal static void SelectPolicy(IHttpClientBuilder builder, IConfiguration configuration)
        {
            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                .WaitAndRetryWithLoggingAsync(new[]
                {
                    TimeSpan.FromSeconds(0.5),
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5)
                })
                .WithPolicyKey($"{nameof(EnterpriseWarehouseClient)}RetryPolicy");

            var injectRateLimitingFaultsPolicy = MonkeyPolicy.InjectResultAsync<HttpResponseMessage>(with =>
                with.ResultAndLog(new HttpResponseMessage(HttpStatusCode.TooManyRequests), LogLevel.Error)
                    .InjectionRate(1)
                    .Enabled()
                );

            builder.AddPolicyHandler((services, request) =>
            {
                var logger = services.GetService<ILogger<EnterpriseWarehouseClient>>()!;
                request
                    .GetPolicyExecutionContext()
                    .WithLogger(logger);

                // Note: recommended way of ordering policies: https://github.com/App-vNext/Polly/wiki/PolicyWrap#ordering-the-available-policy-types-in-a-wrap
                var policies = new List<IAsyncPolicy<HttpResponseMessage>>
                {
                    retryPolicy
                };
                policies.AddForFeatureFlag(configuration, $"{FEATURE_FLAG_PREFIX}:InjectRateLimitingFaults", injectRateLimitingFaultsPolicy);

                return policies.Wrap();
            }
            );
        }

        public async Task OrderRepairPartAsync(int repairPartId)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/repairparts/order", repairPartId);
            response.EnsureSuccessStatusCode();
        }
    }
}
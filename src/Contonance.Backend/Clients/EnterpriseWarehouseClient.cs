using Polly.Extensions.Http;
using Polly;
using System.Net;
using Polly.Contrib.Simmy;
using Polly.Contrib.Simmy.Outcomes;
using Contonance.Extensions;
using Microsoft.FeatureManagement;

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
                .WaitAndRetryWithLoggingAsync(new[]
                {
                    TimeSpan.FromSeconds(0.5),
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5)
                })
                .WithPolicyKey($"{nameof(EnterpriseWarehouseClient)}RetryPolicy");

            var injectRateLimitingFaultsPolicy = MonkeyPolicy.InjectResultAsync<HttpResponseMessage>(with =>
                with.Result(new HttpResponseMessage(HttpStatusCode.TooManyRequests))
                    .InjectionRate(1)
                    .Enabled()
                );

            builder.AddPolicyHandler((services, request) =>
            {
                var logger = services.GetService<ILogger<EnterpriseWarehouseClient>>();
                var context = new Context().WithLogger(logger);
                request.SetPolicyExecutionContext(context);


                var testBackendSetting = configuration.GetValue<bool>("featureManagement:Contonance.Backend:InjectRateLimitingFaults");
                var testWebPortalSetting = configuration.GetValue<bool>("featureManagement:Contonance.WebPortal.Server:EnableCircuitBreakerPolicy");

                var featureManager = services.GetService<IFeatureManager>();
                var testFFs = featureManager.GetFeatureNamesAsync().ToBlockingEnumerable().ToList();
                var testBackendSetting2 = featureManager.IsEnabledAsync("Contonance.Backend:InjectRateLimitingFaults").Result;
                var testWebPortalSetting2 = featureManager.IsEnabledAsync("Contonance.WebPortal.Server:EnableCircuitBreakerPolicy").Result;

                var configurationNew = services.GetService<IConfiguration>();
                var testBackendSetting3 = configurationNew.GetValue<bool>("featureManagement:Contonance.Backend:InjectRateLimitingFaults");
                var testWebPortalSetting3 = configurationNew.GetValue<bool>("featureManagement:Contonance.WebPortal.Server:EnableCircuitBreakerPolicy");


                // Note: recommended way of ordering policies: https://github.com/App-vNext/Polly/wiki/PolicyWrap#ordering-the-available-policy-types-in-a-wrap
                var policies = new List<IAsyncPolicy<HttpResponseMessage>>
                {
                    retryPolicy
                };
                policies.AddForFeatureFlag(configuration, $"{FEATURE_FLAG_PREFIX}/InjectRateLimitingFaults", injectRateLimitingFaultsPolicy);

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
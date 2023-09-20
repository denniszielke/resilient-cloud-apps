using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;

namespace Contonance.Extensions;

public static class HttpClientBuilderPolicyExtensions
{
    public static IHttpClientBuilder AddPolicyConfiguration(this IHttpClientBuilder httpClientBuilder,
        Action<IHttpClientBuilder, IConfiguration> action,
        IConfiguration configuration)
    {
        action(httpClientBuilder, configuration);

        return httpClientBuilder;
    }

    public static void AddForFeatureFlag(
        this IList<IAsyncPolicy<HttpResponseMessage>> policyList,
        IConfiguration configuration,
        string settingName,
        IAsyncPolicy<HttpResponseMessage> policy)
    {
        if (configuration.GetValue<bool>(settingName))
        {
            policyList.Add(policy);
        }
    }

    public static IAsyncPolicy<HttpResponseMessage> Wrap(this IList<IAsyncPolicy<HttpResponseMessage>> policyList)
    {
        //Policy.WrapAsync throws an error if only one policy is passed in
        return policyList.Count < 2 ? policyList[0] : Policy.WrapAsync(policyList.ToArray());
    }

    public static AsyncRetryPolicy<HttpResponseMessage> WaitAndRetryWithLoggingAsync(
        this PolicyBuilder<HttpResponseMessage> policyBuilder,
        IEnumerable<TimeSpan> sleepDurations)
    {
        return policyBuilder.WaitAndRetryAsync(sleepDurations,
            onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    var requestContext = outcome.Result.RequestMessage.GetPolicyExecutionContext();
                    requestContext.GetLogger()?.LogWarning($"Retry attempt {retryAttempt} with {timespan.TotalMilliseconds}ms delay of {context.PolicyKey}, due to: {{StatusCode: {(int)outcome.Result.StatusCode}, ReasonPhrase: '{outcome.Result.ReasonPhrase}'}}");
                });
    }
}
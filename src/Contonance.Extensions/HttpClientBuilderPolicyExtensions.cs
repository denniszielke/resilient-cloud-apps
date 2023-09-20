using Polly;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;

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
}
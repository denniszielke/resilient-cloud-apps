using Microsoft.Extensions.Logging;
using Polly.Contrib.Simmy.Outcomes;

public static class MonkeyPolicyExtensions
{
    public static InjectOutcomeAsyncOptions<HttpResponseMessage> ResultAndLog(
        this InjectOutcomeAsyncOptions<HttpResponseMessage> options,
        HttpResponseMessage result,
        LogLevel logLevel)
    {
        return options.Result((context, ct) =>
        {
            context.GetLogger()?.Log(logLevel, $"Injecting result {{StatusCode: {(int)result.StatusCode}, ReasonPhrase: '{result.ReasonPhrase}'}} of {context.PolicyKey}");
            return Task.FromResult(result);
        });
    }
}
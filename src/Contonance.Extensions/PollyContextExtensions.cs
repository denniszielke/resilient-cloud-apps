using Microsoft.Extensions.Logging;

public static class PollyContextExtensions
{
    private static readonly string LoggerKey = "ILogger";

    public static Polly.Context WithLogger<T>(this Polly.Context context, ILogger<T> logger)
    {
        context[LoggerKey] = logger;
        return context;
    }

    public static ILogger GetLogger(this Polly.Context context)
    {
        if (context.TryGetValue(LoggerKey, out object logger))
        {
            return logger as ILogger;
        }

        return null;
    }
}
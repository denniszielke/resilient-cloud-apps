using Microsoft.Extensions.Configuration;

public static class ConfigurationExtensions
{
    public static string GetNoEmptyStringOrThrow(this IConfiguration configuration, string key)
    {
        var value = configuration.GetValue<string>(key);
        ArgumentException.ThrowIfNullOrEmpty(value);
        return value;
    }
}
using Microsoft.Extensions.Configuration;

namespace DongQiDB.Infrastructure.Configuration;

/// <summary>
/// Configuration builder helper
/// </summary>
public static class ConfigurationLoader
{
    public static IConfigurationRoot Build(string environment)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables(prefix: "DQ_")
            .AddUserSecrets(typeof(ConfigurationLoader).Assembly, optional: true)
            .Build();

        return config;
    }
}

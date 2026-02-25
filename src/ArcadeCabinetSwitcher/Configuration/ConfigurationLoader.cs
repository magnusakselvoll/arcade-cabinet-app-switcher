using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArcadeCabinetSwitcher.Configuration;

public sealed class ConfigurationLoader : IConfigurationLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) }
    };

    private readonly ILogger<ConfigurationLoader> _logger;
    private readonly string _configFilePath;

    public ConfigurationLoader(ILogger<ConfigurationLoader> logger, string? configFilePath = null)
    {
        _logger = logger;
        _configFilePath = configFilePath ?? Path.Combine(AppContext.BaseDirectory, "profiles.json");
    }

    public AppSwitcherConfig Load()
    {
        if (!File.Exists(_configFilePath))
        {
            _logger.LogError(LogEvents.ConfigurationMissing, "Configuration file not found: {Path}", _configFilePath);
            throw new InvalidOperationException($"Configuration file not found: {_configFilePath}");
        }

        AppSwitcherConfig? config;
        try
        {
            var json = File.ReadAllText(_configFilePath);
            config = JsonSerializer.Deserialize<AppSwitcherConfig>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(LogEvents.ConfigurationInvalid, ex, "Configuration file is not valid JSON: {Path}", _configFilePath);
            throw new InvalidOperationException($"Configuration file is not valid JSON: {_configFilePath}", ex);
        }

        if (config is null)
        {
            _logger.LogError(LogEvents.ConfigurationInvalid, "Configuration file deserialized to null: {Path}", _configFilePath);
            throw new InvalidOperationException($"Configuration file deserialized to null: {_configFilePath}");
        }

        var errors = ConfigurationValidator.Validate(config);
        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                _logger.LogError(LogEvents.ConfigurationInvalid, "Configuration validation error: {Error}", error);
            }
            throw new InvalidOperationException(
                $"Configuration is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }

        _logger.LogInformation(LogEvents.ConfigurationLoaded, "Configuration loaded with {ProfileCount} profile(s)", config.Profiles.Count);
        return config;
    }
}

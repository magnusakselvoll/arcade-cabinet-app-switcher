namespace ArcadeCabinetSwitcher;

/// <summary>
/// Structured event ID constants for all FR-7.2 log events.
/// </summary>
/// <remarks>
/// Ranges:
///   1000–1099  Service lifecycle (start, stop)
///   2000–2099  Configuration (loaded, invalid, missing)
///   3000–3099  Profile switching (switch started, completed)
///   4000–4099  Process management (launched, terminated, failed)
///   5000–5099  Input detection (combo detected, input error)
///   9000–9099  Errors (unexpected exceptions)
/// </remarks>
public static class LogEvents
{
    // Service lifecycle
    public static readonly EventId ServiceStarting = new(1000, nameof(ServiceStarting));
    public static readonly EventId ServiceStopping = new(1001, nameof(ServiceStopping));

    // Configuration
    public static readonly EventId ConfigurationLoaded = new(2000, nameof(ConfigurationLoaded));
    public static readonly EventId ConfigurationInvalid = new(2001, nameof(ConfigurationInvalid));
    public static readonly EventId ConfigurationMissing = new(2002, nameof(ConfigurationMissing));

    // Profile switching
    public static readonly EventId ProfileSwitchStarted = new(3000, nameof(ProfileSwitchStarted));
    public static readonly EventId ProfileSwitchCompleted = new(3001, nameof(ProfileSwitchCompleted));

    // Process management
    public static readonly EventId ProcessLaunched = new(4000, nameof(ProcessLaunched));
    public static readonly EventId ProcessTerminated = new(4001, nameof(ProcessTerminated));
    public static readonly EventId ProcessTerminationFailed = new(4002, nameof(ProcessTerminationFailed));

    // Input detection
    public static readonly EventId InputComboDetected = new(5000, nameof(InputComboDetected));
    public static readonly EventId InputError = new(5001, nameof(InputError));

    // Errors
    public static readonly EventId UnexpectedException = new(9000, nameof(UnexpectedException));
}

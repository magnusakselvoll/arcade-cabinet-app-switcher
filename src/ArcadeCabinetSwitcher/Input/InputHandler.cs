using ArcadeCabinetSwitcher.Configuration;
using Microsoft.Extensions.Configuration;

namespace ArcadeCabinetSwitcher.Input;

internal sealed class InputHandler : IInputHandler
{
    public event EventHandler<string>? ProfileSwitchRequested;

    private readonly ILogger<InputHandler> _logger;
    private readonly IJoystickReader _joystickReader;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _pollInterval;
    private readonly bool _debugMode;

    private Task? _pollingTask;
    private CancellationTokenSource? _linkedCts;

    public InputHandler(ILogger<InputHandler> logger, IJoystickReader joystickReader, IConfiguration configuration)
        : this(logger, joystickReader, TimeProvider.System, TimeSpan.FromMilliseconds(50),
               configuration.GetValue<bool>("InputDebugMode"))
    { }

    internal InputHandler(ILogger<InputHandler> logger, IJoystickReader joystickReader,
        TimeProvider timeProvider, TimeSpan pollInterval, bool debugMode)
    {
        _logger = logger;
        _joystickReader = joystickReader;
        _timeProvider = timeProvider;
        _pollInterval = pollInterval;
        _debugMode = debugMode;
    }

    public Task StartAsync(AppSwitcherConfig config, CancellationToken cancellationToken)
    {
        var combos = config.Profiles
            .Select(p => new ComboDefinition(
                p.Name,
                new HashSet<string>(p.SwitchCombo.Buttons, StringComparer.OrdinalIgnoreCase),
                TimeSpan.FromSeconds(p.SwitchCombo.HoldDurationSeconds)))
            .ToList();

        _linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _pollingTask = Task.Run(() => PollAsync(combos, _linkedCts.Token), CancellationToken.None);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_linkedCts is not null)
            await _linkedCts.CancelAsync();

        if (_pollingTask is not null)
        {
            try
            {
                await _pollingTask.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Shutdown timeout
            }
        }

        _linkedCts?.Dispose();
        _linkedCts = null;
    }

    private async Task PollAsync(List<ComboDefinition> combos, CancellationToken cancellationToken)
    {
        if (!_joystickReader.Initialize())
        {
            _logger.LogWarning(LogEvents.JoystickNotFound,
                "No joystick found; input monitoring disabled");
            return;
        }

        _logger.LogInformation(LogEvents.InputMonitoringStarted, "Input monitoring started");

        var comboStates = combos.Select(_ => new ComboState()).ToList();
        IReadOnlySet<string>? lastDebugButtons = null;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var pressed = _joystickReader.GetPressedButtons();

                if (_debugMode && pressed.Count >= 2)
                {
                    if (lastDebugButtons is null || !pressed.SetEquals(lastDebugButtons))
                    {
                        _logger.LogDebug(LogEvents.InputDebugButtonsPressed,
                            "Pressed buttons: {Buttons}", string.Join(", ", pressed.Order()));
                        lastDebugButtons = pressed;
                    }
                }
                else if (pressed.Count < 2)
                {
                    lastDebugButtons = null;
                }

                bool eventFired = false;
                for (int i = 0; i < combos.Count && !eventFired; i++)
                {
                    var combo = combos[i];
                    var state = comboStates[i];

                    if (combo.Buttons.IsSubsetOf(pressed))
                    {
                        if (!state.Active)
                        {
                            state.StartTimestamp = _timeProvider.GetTimestamp();
                            _logger.LogDebug(LogEvents.InputComboHoldStarted,
                                "Combo hold started for profile {ProfileName}", combo.ProfileName);
                        }
                        else
                        {
                            var elapsed = _timeProvider.GetElapsedTime(state.StartTimestamp!.Value);
                            if (elapsed >= combo.HoldDuration)
                            {
                                _logger.LogInformation(LogEvents.InputComboDetected,
                                    "Combo detected for profile {ProfileName} after {ElapsedSeconds:N2}s",
                                    combo.ProfileName, elapsed.TotalSeconds);

                                foreach (var s in comboStates)
                                    s.StartTimestamp = null;

                                ProfileSwitchRequested?.Invoke(this, combo.ProfileName);
                                eventFired = true;
                            }
                        }
                    }
                    else if (state.Active)
                    {
                        _logger.LogDebug(LogEvents.InputComboHoldReleased,
                            "Combo hold released for profile {ProfileName}", combo.ProfileName);
                        state.StartTimestamp = null;
                    }
                }

                await Task.Delay(_pollInterval, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown
        }
        finally
        {
            _logger.LogInformation(LogEvents.InputMonitoringStopped, "Input monitoring stopped");
        }
    }

    private sealed record ComboDefinition(
        string ProfileName,
        IReadOnlySet<string> Buttons,
        TimeSpan HoldDuration);

    private sealed class ComboState
    {
        public long? StartTimestamp { get; set; }
        public bool Active => StartTimestamp.HasValue;
    }
}

using System.Runtime.Versioning;
using Vortice.DirectInput;

namespace ArcadeCabinetSwitcher.Input;

internal sealed class DirectInputJoystickReader : IJoystickReader
{
    private readonly ILogger<DirectInputJoystickReader> _logger;
    private IDirectInput8? _directInput;
    private readonly List<IDirectInputDevice8> _devices = [];

    public DirectInputJoystickReader(ILogger<DirectInputJoystickReader> logger)
    {
        _logger = logger;
    }

    public bool Initialize()
    {
        if (!OperatingSystem.IsWindows())
        {
            _logger.LogWarning(LogEvents.JoystickNotFound,
                "DirectInput is only supported on Windows; input monitoring disabled");
            return false;
        }

        return InitializeWindows();
    }

    [SupportedOSPlatform("windows")]
    private bool InitializeWindows()
    {
        _directInput = DInput.DirectInput8Create();

        var deviceInstances = _directInput.GetDevices(
            DeviceClass.GameControl,
            DeviceEnumerationFlags.AttachedOnly);

        foreach (var deviceInstance in deviceInstances)
        {
            try
            {
                var device = _directInput.CreateDevice(deviceInstance.InstanceGuid);
                device.SetDataFormat<RawJoystickState>();
                device.SetCooperativeLevel(
                    IntPtr.Zero,
                    CooperativeLevel.NonExclusive | CooperativeLevel.Background);
                device.Acquire();
                _devices.Add(device);

                _logger.LogInformation(LogEvents.JoystickAcquired,
                    "Acquired joystick device: {DeviceName}", deviceInstance.InstanceName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(LogEvents.InputError, ex,
                    "Failed to acquire joystick device {DeviceName}", deviceInstance.InstanceName);
            }
        }

        if (_devices.Count == 0)
        {
            _logger.LogWarning(LogEvents.JoystickNotFound, "No joystick devices found");
            return false;
        }

        return true;
    }

    public IReadOnlySet<string> GetPressedButtons()
    {
        if (!OperatingSystem.IsWindows() || _devices.Count == 0)
            return new HashSet<string>();

        return GetPressedButtonsWindows();
    }

    [SupportedOSPlatform("windows")]
    private IReadOnlySet<string> GetPressedButtonsWindows()
    {
        var pressed = new HashSet<string>();
        bool multipleDevices = _devices.Count > 1;

        for (int i = 0; i < _devices.Count; i++)
        {
            var device = _devices[i];
            string prefix = multipleDevices ? $"Joy{i + 1}:" : "";

            try
            {
                device.Poll();
                var state = device.GetCurrentJoystickState();

                for (int b = 0; b < state.Buttons.Length; b++)
                {
                    if (state.Buttons[b])
                        pressed.Add($"{prefix}Button{b}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(LogEvents.InputError, ex,
                    "Error polling joystick Joy{DeviceIndex}", i + 1);
            }
        }

        return pressed;
    }

    public void Dispose()
    {
        if (OperatingSystem.IsWindows())
            DisposeWindows();

        _directInput?.Dispose();
        _directInput = null;
    }

    [SupportedOSPlatform("windows")]
    private void DisposeWindows()
    {
        foreach (var device in _devices)
        {
            try { device.Unacquire(); } catch { }
            device.Dispose();
        }

        _devices.Clear();
    }
}

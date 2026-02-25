using Silk.NET.SDL;

namespace ArcadeCabinetSwitcher.Input;

internal sealed class SdlJoystickReader : IJoystickReader
{
    private readonly ILogger<SdlJoystickReader> _logger;
    private Sdl? _sdl;
    private readonly List<nint> _joystickHandles = [];

    public SdlJoystickReader(ILogger<SdlJoystickReader> logger)
    {
        _logger = logger;
    }

    public unsafe bool Initialize()
    {
        _sdl = Sdl.GetApi();

        // Headless hints — must be set before SDL_Init.
        // Windows: spin up a dedicated joystick thread so no Win32 message pump is needed.
        _sdl.SetHint(Sdl.HintJoystickThread, "1");
        // Allow joystick events when the process has no foreground window.
        _sdl.SetHint(Sdl.HintJoystickAllowBackgroundEvents, "1");
        // macOS: disable the GameController (MFi) driver, which requires AppKit setup on the
        // main thread. Arcade cabinet USB encoder boards are plain HID devices handled by
        // SDL2's HIDAPI backend, which works headlessly on all platforms without SDL_INIT_VIDEO.
        _sdl.SetHint("SDL_JOYSTICK_MFI", "0");

        if (_sdl.Init(Sdl.InitJoystick) < 0)
        {
            _logger.LogWarning(LogEvents.JoystickNotFound,
                "SDL init failed: {Error}", _sdl.GetErrorS());
            _sdl.Dispose();
            _sdl = null;
            return false;
        }

        int count = _sdl.NumJoysticks();
        for (int i = 0; i < count; i++)
        {
            Joystick* joy = _sdl.JoystickOpen(i);
            if (joy == null)
            {
                _logger.LogWarning(LogEvents.InputError,
                    "Failed to open joystick {Index}: {Error}", i, _sdl.GetErrorS());
                continue;
            }

            _joystickHandles.Add((nint)joy);
            _logger.LogInformation(LogEvents.JoystickAcquired,
                "Acquired joystick {Index}: {Name} ({ButtonCount} buttons)",
                i, _sdl.JoystickNameS(joy), _sdl.JoystickNumButtons(joy));
        }

        if (_joystickHandles.Count == 0)
        {
            _logger.LogWarning(LogEvents.JoystickNotFound, "No joystick devices found");
            _sdl.Quit();
            _sdl.Dispose();
            _sdl = null;
            return false;
        }

        return true;
    }

    public unsafe IReadOnlySet<string> GetPressedButtons()
    {
        if (_sdl is null || _joystickHandles.Count == 0)
            return new HashSet<string>();

        // Pump the SDL event queue to refresh joystick state.
        Event sdlEvent;
        while (_sdl.PollEvent(&sdlEvent) != 0) { /* discard */ }

        var pressed = new HashSet<string>();
        bool multipleDevices = _joystickHandles.Count > 1;

        for (int i = 0; i < _joystickHandles.Count; i++)
        {
            var joy = (Joystick*)_joystickHandles[i];
            string prefix = multipleDevices ? $"Joy{i + 1}:" : "";
            int numButtons = _sdl.JoystickNumButtons(joy);

            for (int b = 0; b < numButtons; b++)
            {
                if (_sdl.JoystickGetButton(joy, b) != 0)
                    pressed.Add($"{prefix}Button{b}");
            }
        }

        return pressed;
    }

    public unsafe void Dispose()
    {
        if (_sdl is null)
            return;

        foreach (var handle in _joystickHandles)
            _sdl.JoystickClose((Joystick*)handle);

        _joystickHandles.Clear();
        _sdl.Quit();
        _sdl.Dispose();
        _sdl = null;
    }
}

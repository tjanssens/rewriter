using SharpHook;
using SharpHook.Native;
using TextRewriter.Core.Interfaces;

namespace TextRewriter.Services;

public class InputSimulatorService : IInputSimulator
{
    private readonly EventSimulator _simulator = new();
    private readonly bool _isMacOS;
    private readonly int _delayMs;

    public InputSimulatorService(IPlatformService platform, int delayMs = 50)
    {
        _isMacOS = platform.IsMacOS;
        _delayMs = delayMs;
    }

    private KeyCode ModifierKey => _isMacOS ? KeyCode.VcLeftMeta : KeyCode.VcLeftControl;

    public void SimulateCopy()
    {
        SimulateKeyCombo(ModifierKey, KeyCode.VcC);
    }

    public void SimulateSelectAll()
    {
        SimulateKeyCombo(ModifierKey, KeyCode.VcA);
    }

    public void SimulatePaste()
    {
        SimulateKeyCombo(ModifierKey, KeyCode.VcV);
    }

    private void SimulateKeyCombo(KeyCode modifier, KeyCode key)
    {
        _simulator.SimulateKeyPress(modifier);
        Thread.Sleep(_delayMs);
        _simulator.SimulateKeyPress(key);
        _simulator.SimulateKeyRelease(key);
        Thread.Sleep(_delayMs);
        _simulator.SimulateKeyRelease(modifier);
    }
}

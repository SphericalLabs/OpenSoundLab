using System;

public class KeyboardLatchController
{
    private int _latchedKeyIndex = -1;

    private readonly Func<int, bool> _isValidKeyIndex;
    private readonly Action<int, bool> _applyLatchVisual;
    private readonly Action<bool, int, keyboardDeviceInterface.keyInput> _hit;
    private readonly Action _retriggerPulse;

    public KeyboardLatchController(
        Func<int, bool> isValidKeyIndex,
        Action<int, bool> applyLatchVisual,
        Action<bool, int, keyboardDeviceInterface.keyInput> hit,
        Action retriggerPulse)
    {
        _isValidKeyIndex = isValidKeyIndex;
        _applyLatchVisual = applyLatchVisual;
        _hit = hit;
        _retriggerPulse = retriggerPulse;
    }

    public bool IsKeyLatched(int keyIndex)
    {
        return _isValidKeyIndex(keyIndex) && _latchedKeyIndex == keyIndex;
    }

    public void ToggleLatchState(int keyIndex)
    {
        SetLatchState(keyIndex, _latchedKeyIndex != keyIndex);
    }

    public void SetLatchState(int keyIndex, bool on)
    {
        if (!_isValidKeyIndex(keyIndex))
        {
            return;
        }

        bool alreadyLatched = _latchedKeyIndex == keyIndex;
        if (alreadyLatched == on)
        {
            return;
        }

        if (on)
        {
            // Unlatch any previous key
            if (_isValidKeyIndex(_latchedKeyIndex) && _latchedKeyIndex != keyIndex)
            {
                _applyLatchVisual(_latchedKeyIndex, false);
                _hit(false, _latchedKeyIndex, keyboardDeviceInterface.keyInput.latch);
                _latchedKeyIndex = -1;
            }

            _latchedKeyIndex = keyIndex;
            _applyLatchVisual(keyIndex, true);
            _hit(true, keyIndex, keyboardDeviceInterface.keyInput.latch);
            return;
        }

        _applyLatchVisual(keyIndex, false);
        _hit(false, keyIndex, keyboardDeviceInterface.keyInput.latch);
        if (_latchedKeyIndex == keyIndex)
        {
            _latchedKeyIndex = -1;
        }
    }

    public void OnLatchedKeyTouchedWithActiveTrigger(int keyIndex)
    {
        if (_latchedKeyIndex == keyIndex)
        {
            _retriggerPulse?.Invoke();
        }
    }
}


using System;

public static class OSLGlobalResetBus
{
    public static Action<object> resetPressedEvent;

    public static void Broadcast(object origin)
    {
        if (resetPressedEvent != null) resetPressedEvent(origin);
    }
}

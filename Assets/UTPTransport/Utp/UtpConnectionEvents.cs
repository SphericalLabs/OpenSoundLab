using Unity.Collections;

namespace Utp
{
    /// <summary>
    /// Connection events seen on a connection.
    /// </summary>
    public enum UtpConnectionEventType
    {
        OnConnected,
        OnReceivedData,
        OnDisconnected
    }

    /// <summary>
    /// Struct to store events and the related data for that event.
    /// </summary>
    public struct UtpConnectionEvent
    {
        /// <summary>
        /// The event type.
        /// </summary>
        public byte eventType;

        /// <summary>
        /// Event data, only used for OnReceived event.
        /// </summary>
        public FixedList4096Bytes<byte> eventData;

        /// <summary>
        /// The connection ID of the connection corresponding to this event.
        /// </summary>
        public int connectionId;
    }
}

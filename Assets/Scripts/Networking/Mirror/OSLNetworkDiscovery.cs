using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using Mirror;
using Mirror.Discovery;

[DisallowMultipleComponent]
[AddComponentMenu("Network/Network Discovery")]
public class OSLNetworkDiscovery : NetworkDiscovery
{
    public bool isDiscoverable = true;

    public bool ToggleIsDiscoverable()
    {
        return isDiscoverable = !isDiscoverable;
    }
    public void SetIsDiscoverable(bool b)
    {
        isDiscoverable = b;
        AdvertiseServer();
    }

    protected override void ProcessClientRequest(ServerRequest request, IPEndPoint endpoint)
    {
        if (!isDiscoverable)
        {
            return;
        }
        base.ProcessClientRequest(request, endpoint);
    }

    public override void BroadcastDiscoveryRequest()
    {
        if (clientUdpClient == null)
            return;
        //copy from the original class NetworkDiscoveryBase.cs but removed the if below otherwise a already running Host will not look for the other hosts
        /*
        if (NetworkClient.isConnected)
        {
            StopDiscovery();
            return;
        }*/

        IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, serverBroadcastListenPort);

        if (!string.IsNullOrWhiteSpace(BroadcastAddress))
        {
            try
            {
                endPoint = new IPEndPoint(IPAddress.Parse(BroadcastAddress), serverBroadcastListenPort);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        using (NetworkWriterPooled writer = NetworkWriterPool.Get())
        {
            writer.WriteLong(secretHandshake);

            try
            {
                ServerRequest request = GetRequest();

                writer.Write(request);

                ArraySegment<byte> data = writer.ToArraySegment();

                clientUdpClient.SendAsync(data.Array, data.Count, endPoint);
            }
            catch (Exception)
            {
                // It is ok if we can't broadcast to one of the addresses
            }
        }
    }
}

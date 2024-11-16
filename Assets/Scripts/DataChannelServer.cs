using System.Net.Sockets;
using System.Net;
using UnityEngine;
using WebSocketSharp.Server;

public class DataChannelServer : MonoBehaviour
{
    private WebSocketServer wssv;
    private string serverIpv4Address;
    private int serverport = 8080;

    private void Awake()
    {
        // Get server ip in network
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                serverIpv4Address = ip.ToString();
                break;
            }
        }

        wssv = new WebSocketServer($"ws://{serverIpv4Address}:{serverport}");

        wssv.AddWebSocketService<DataChannelService>($"/{nameof(DataChannelService)}");
        wssv.Start();
    }
}

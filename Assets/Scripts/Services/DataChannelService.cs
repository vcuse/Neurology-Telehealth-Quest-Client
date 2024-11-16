using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

public class DataChannelService : WebSocketBehavior
{
    protected override void OnOpen()
    {
        Debug.Log("SERVER started!");
    }

    protected override void OnMessage(MessageEventArgs e)
    {
       Debug.Log(ID + " - DataChannel SERVER got message " + e.Data);

        // Forward messages to all other clients
        foreach (var id in Sessions.ActiveIDs)
        {
            if (id != ID)
            {
                Sessions.SendTo(e.Data, id);
            }
        }
    }
}

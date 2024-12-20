using System.Collections;
using UnityEngine;
using WebSocketSharp;
using Unity.WebRTC;
using Newtonsoft.Json;

public class DataChannelReceiver : MonoBehaviour
{
    private RTCPeerConnection connection;
    private RTCDataChannel dataChannel;

    private WebSocket ws;

    private bool hasReceivedOffer = false;
    private RTCSessionDescription sessionDescription;

    float timePassed = 0f;

    private void Start()
    {   
        InitClient();
    }

    private void Update()
    {
        timePassed += Time.deltaTime;
        if (timePassed > 5f)
        {
            var message = new
            {
                type = "HEARTBEAT"
            };
            var jsonData = JsonConvert.SerializeObject(message);
            ws.Send(jsonData);

            timePassed = 0f;
        }
    }

    private void OnDestroy()
    {
        dataChannel.Close();
        connection.Close();
    }

    public void InitClient()
    {
        ws = new WebSocket("wss://videochat-signaling-app.ue.r.appspot.com:443/peerjs?id=238473289&token=67892&key=peerjs");
        ws.OnMessage += (sender, e) =>
        {
            if (e.Data.Contains("CANDIDATE"))
            {
                Debug.Log("Receiver got CANDIDATE: " + e.Data);

                // Generate candidate data
                var candidateInit = JsonConvert.DeserializeObject<CandidateMessage>(e.Data);
                RTCIceCandidateInit init = new RTCIceCandidateInit();
                init.sdpMid = candidateInit.payload.candidate.sdpMid;
                init.sdpMLineIndex = candidateInit.payload.candidate.sdpMLineIndex;
                init.candidate = candidateInit.payload.candidate.candidate;
                RTCIceCandidate candidate = new RTCIceCandidate(init);

                // Add candidate to this connection
                connection.AddIceCandidate(candidate);
            }
            else if (e.Data.Contains("OFFER"))
            {
                Debug.Log("We got an OFFER" + e.Data);
                string rSDP = JsonConvert.DeserializeObject<OfferMessage>(e.Data).payload.sdp.sdp;
                Debug.Log("Remote SDP is " + rSDP);

                sessionDescription = new RTCSessionDescription
                {
                    type = RTCSdpType.Offer,
                    sdp = rSDP
                };

                // Await the task returned by SetLocalDescription to ensure it's applied
                connection.SetLocalDescription(ref sessionDescription);

                // After this awaits successfully, you can check LocalDescription
                if (connection.LocalDescription.type == RTCSdpType.Offer &&
                    connection.LocalDescription.sdp == rSDP)
                {
                    Debug.Log("Local description successfully assigned.");
                    hasReceivedOffer = true;
                }
                else
                {
                    Debug.LogWarning("Local description was not assigned correctly.");
                }
            }
            else
            {
                Debug.Log("Receiver got message: " + e.Data);
            }

        };
        ws.OnClose += (sender, e) =>
        {
            Debug.Log("Receiver ws closed: " + e.Reason);
        };

        ws.Connect();

        RTCConfiguration config = new RTCConfiguration()
        {
            iceServers = new RTCIceServer[]
            {
                new RTCIceServer { urls = new string[]{ "stun:stun.l.google.com:19302" } }
            }
        };

        connection = new RTCPeerConnection(ref config);
        connection.OnIceCandidate = candidate =>
        {
            Candidate candidateInit = new Candidate()
            {
                type = "CANDIDATE",
                candidate = candidate.Candidate,
                sdpMLineIndex = candidate.SdpMLineIndex ?? 0,
                sdpMid = candidate.SdpMid
            };

            CandidatePayload payload = new CandidatePayload()
            {
                candidate = candidateInit,
                connectionId = "238473289"
            };

            CandidateMessage message = new CandidateMessage()
            {
                payload = payload,
                type = "CANDIDATE",
                dst = "3489534895638"
            };

            var jsonData = JsonConvert.SerializeObject(message);
            ws.Send(jsonData);
        };

        connection.OnIceConnectionChange = state =>
        {
            Debug.Log(state);
        };

        connection.OnDataChannel = channel =>
        {
            dataChannel = channel;
            dataChannel.OnMessage = bytes =>
            {
                var message = System.Text.Encoding.UTF8.GetString(bytes);
                Debug.Log("Receiver received: " + message);
            };
        };
    }
}

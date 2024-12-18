using Newtonsoft.Json;
using System.Collections;
using Unity.WebRTC;
using UnityEditor.PackageManager;
using UnityEngine;
using System;
using WebSocketSharp;

[Serializable]
public class OfferMessage
{
    public string type;
    public string src;
    public string dst;
    public OfferPayload payload;
}

[Serializable]
public class OfferPayload
{
    public SdpData sdp;
    public string type;
    public string connectionId;
    public string browser;
    public string label;
    public bool reliable;
    public string serialization;
}

[Serializable]
public class SdpData
{
    public string type;
    public string sdp;
}

public class DataChannelSender : MonoBehaviour
{
    [SerializeField] private bool sendMessageViaChannel = false;


    private RTCPeerConnection connection;
    private RTCDataChannel dataChannel;

    private WebSocket ws;

    private bool hasReceivedAnswer = false;
    private SessionDescription receivedAnswerSessionDescTemp;
    private SessionDescription remoteSDP;

    private void Start()
    {
        InitClient();
    }

    private void Update()
    {
        if (hasReceivedAnswer)
        {
            hasReceivedAnswer = !hasReceivedAnswer;
            StartCoroutine(SetRemoteDesc());
        }
        if (sendMessageViaChannel)
        {
            sendMessageViaChannel = !sendMessageViaChannel;
            dataChannel.Send("TEST! TEST! TEST!");
        }
    }

    private void OnDestroy()
    {
        dataChannel.Close();
        connection.Close();
    }

    public void InitClient()
    {
        ws = new WebSocket("wss://videochat-signaling-app.ue.r.appspot.com:443/peerjs?id=3489534895638&token=6789&key=peerjs");
        ws.OnMessage += (sender, e) =>
        {
            if (e.Data.Contains("CANDIDATE"))
            {
                Debug.Log("Sender got CANDIDATE: " + e.Data);
                
                // Generate candidate data
                var candidateInit = JsonConvert.DeserializeObject<Message>(e.Data);
                RTCIceCandidateInit init = new RTCIceCandidateInit();
                init.sdpMid = candidateInit.payload.candidate.sdpMid;
                init.sdpMLineIndex = candidateInit.payload.candidate.sdpMLineIndex;
                init.candidate = candidateInit.payload.candidate.candidate;
                RTCIceCandidate candidate = new RTCIceCandidate(init);

                // Add candidate to this connection
                connection.AddIceCandidate(candidate);
            }
            else if (e.Data.Contains("ANSWER"))
            {
                Debug.Log("Sender got ANSWER: " + e.Data);
                receivedAnswerSessionDescTemp = JsonConvert.DeserializeObject<SessionDescription>(e.Data);
                hasReceivedAnswer = true;
            }
            else if (e.Data.Contains("OFFER")){
                Debug.Log("We got an OFFER" + e.Data);
                String rSDP = JsonConvert.DeserializeObject<OfferMessage>(e.Data).payload.sdp.sdp;
                Debug.Log("Remote SDP is " + rSDP);

                RTCSessionDescription sessionDescription = new RTCSessionDescription
                {
                    type = RTCSdpType.Offer,
                    sdp = rSDP
                };

                
                // // Await the task returned by SetLocalDescription to ensure it's applied
                // connection.SetLocalDescription(ref sessionDescription);

                // // After this awaits successfully, you can check LocalDescription
                // if (connection.LocalDescription.type == RTCSdpType.Offer &&
                //     connection.LocalDescription.sdp == rSDP)
                // {
                //     Debug.Log("Local description successfully assigned.");
                // }
                // else
                // {
                //     Debug.LogWarning("Local description was not assigned correctly.");
                // }

                
            }
            else
            {
                Debug.Log("Sender got message: " + e.Data);
            }
        };
        ws.OnClose += (sender, e) =>
        {
            Debug.Log("Sender ws closed: " + e.Reason);
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
                candidate = candidate.Candidate,
                sdpMLineIndex = candidate.SdpMLineIndex ?? 0,
                sdpMid = candidate.SdpMid
            };

            Payload payload = new Payload()
            {
                candidate = candidateInit,
                connectionId = "3489534895638"
            };

            Message message = new Message()
            {
                payload = payload,
                type = "CANDIDATE",
                dst = "238473289"
            };
            
            var jsonData = JsonConvert.SerializeObject(message);
            ws.Send(jsonData);
        };
        connection.OnIceConnectionChange = state =>
        {
            Debug.Log(state);
        };

        dataChannel = connection.CreateDataChannel("sendChannel");
        dataChannel.OnOpen = () =>
        {
            Debug.Log("Sender opended channel");
        };
        dataChannel.OnClose = () =>
        {
            Debug.Log("Sender closed channel");
        };

        connection.OnNegotiationNeeded = () =>
        {
            StartCoroutine(CreateOffer());
        };
    }

    private IEnumerator CreateOffer()
    {
        var offer = connection.CreateOffer();
        yield return offer;

        var offerDesc = offer.Desc;
        var localDescOp = connection.SetLocalDescription(ref offerDesc);
        yield return localDescOp;

        // Send desc to server for receiver connection
        var offerSessionDesc = new SessionDescription()
        {
            type = "OFFER",
            sdp = offerDesc.sdp
        };

        var jsonData = JsonConvert.SerializeObject(offerSessionDesc);
        Debug.Log("Sending OFFER: " +  jsonData);
        ws.Send(jsonData);
    }

    private IEnumerator SetRemoteDesc()
    {
        RTCSessionDescription answerSessionDesc = new RTCSessionDescription();
        answerSessionDesc.type = RTCSdpType.Answer;
        answerSessionDesc.sdp = receivedAnswerSessionDescTemp.sdp;

        var remoteDescOp = connection.SetRemoteDescription(ref answerSessionDesc);
        yield return remoteDescOp;
    }
}

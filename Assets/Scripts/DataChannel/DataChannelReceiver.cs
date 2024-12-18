using System.Collections;
using UnityEngine;
using WebSocketSharp;
using Unity.WebRTC;
using Newtonsoft.Json;
using System.Collections.Generic;

public class DataChannelReceiver : MonoBehaviour
{
    private RTCPeerConnection connection;
    private RTCDataChannel dataChannel;

    private WebSocket ws;

    private bool hasReceivedOffer = false;
    private SessionDescription receivedOfferSessionDescTemp;

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
            Debug.Log("Receiver sending heartbeat");
            var message = new
            {
                type = "HEARTBEAT"
            };
            var jsonData = JsonConvert.SerializeObject(message);
            ws.Send(jsonData);

            timePassed = 0f;
        }

        if (hasReceivedOffer)
        {
            hasReceivedOffer = !hasReceivedOffer;
            StartCoroutine(CreateAnswer());
        }
    }

    private void OnDestroy()
    {
        dataChannel.Close();
        connection.Close();
    }

    public void InitClient()
    {
        ws = new WebSocket("ws://127.0.0.1:9000/peerjs?id=238473289&token=67892&key=peerjs");
        ws.OnMessage += (sender, e) =>
        {
            if (e.Data.Contains("CANDIDATE"))
            {
                Debug.Log("Receiver got CANDIDATE: " + e.Data);

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
            else if (e.Data.Contains("OFFER"))
            {
                Debug.Log("Receiver got OFFER: " + e.Data);
                receivedOfferSessionDescTemp = JsonConvert.DeserializeObject<SessionDescription>(e.Data);
                hasReceivedOffer = true;
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

            Payload payload = new Payload()
            {
                candidate = candidateInit,
                connectionId = "238473289"
            };

            Message message = new Message()
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

    private IEnumerator CreateAnswer()
    {
        RTCSessionDescription offerSessionDesc = new RTCSessionDescription();
        offerSessionDesc.type = RTCSdpType.Offer;
        offerSessionDesc.sdp = receivedOfferSessionDescTemp.sdp;

        var remoteDescOp = connection.SetRemoteDescription(ref offerSessionDesc);
        yield return remoteDescOp;

        var answer = connection.CreateAnswer();
        yield return answer;

        var answerDesc = answer.Desc;
        var localDescOp = connection.SetLocalDescription(ref answerDesc);
        yield return localDescOp;

        // Send desc to server for sender connection
        var answerSessionDesc = new SessionDescription()
        {
            type = "ANSWER",
            sdp = answerDesc.sdp
        };

        var jsonData = JsonConvert.SerializeObject(answerSessionDesc);
        ws.Send(jsonData);
    }
}

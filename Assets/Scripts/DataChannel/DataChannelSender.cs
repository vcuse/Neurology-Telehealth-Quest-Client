using Newtonsoft.Json;
using System.Collections;
using Unity.WebRTC;
using UnityEngine;
using System;
using WebSocketSharp;

public class DataChannelSender : MonoBehaviour
{
    private RTCPeerConnection connection;

    private WebSocket ws;

    private bool hasReceivedOffer = false;
    private RTCSessionDescription sessionDescription;

    private OfferMessage offer;

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

        if (hasReceivedOffer)
        {
            hasReceivedOffer = !hasReceivedOffer;
            StartCoroutine(CreateAnswer(sessionDescription));
        }
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
                var candidateInit = JsonConvert.DeserializeObject<CandidateMessage>(e.Data);
                RTCIceCandidateInit init = new RTCIceCandidateInit();
                init.sdpMid = candidateInit.payload.candidate.sdpMid;
                init.sdpMLineIndex = candidateInit.payload.candidate.sdpMLineIndex;
                init.candidate = candidateInit.payload.candidate.candidate;
                RTCIceCandidate candidate = new RTCIceCandidate(init);

                // Add candidate to this connection
                connection.AddIceCandidate(candidate);
            }
            else if (e.Data.Contains("OFFER")){
                hasReceivedOffer = true;
                Debug.Log("We got an OFFER" + e.Data);
                offer = JsonConvert.DeserializeObject<OfferMessage>(e.Data);
                String rSDP = offer.payload.sdp.sdp;
                Debug.Log("Remote SDP is " + rSDP);

                sessionDescription = new RTCSessionDescription()
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
                }
                else
                {
                    Debug.LogWarning("Local description was not assigned correctly.");
                }
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
                type = "CANDIDATE",
                candidate = candidate.Candidate,
                sdpMLineIndex = candidate.SdpMLineIndex ?? 0,
                sdpMid = candidate.SdpMid
            };

            CandidatePayload payload = new CandidatePayload()
            {
                candidate = candidateInit,
                connectionId = "3489534895638"
            };

            CandidateMessage message = new CandidateMessage()
            {
                payload = payload,
                type = "CANDIDATE",
                dst = offer.src
            };
            
            var jsonData = JsonConvert.SerializeObject(message);
            ws.Send(jsonData);
        };
        connection.OnIceConnectionChange = state =>
        {
            Debug.Log(state);
        };

        connection.OnNegotiationNeeded = () =>
        {
            Debug.Log("Negotiation needed");
            StartCoroutine(CreateOffer());
        };
    }

    private IEnumerator CreateOffer()
    {
        var newOffer = connection.CreateOffer();
        yield return newOffer;

        Debug.Log(newOffer.Desc.sdp);

        var offerDesc = newOffer.Desc;
        var localDescOp = connection.SetLocalDescription(ref offerDesc);
        yield return localDescOp;

        // Send desc to server for receiver connection
        SdpData sdpData = new SdpData()
        {
            type = "offer",
            sdp = offerDesc.sdp
        };

        OfferPayload payload = new OfferPayload()
        {
            sdp = sdpData,
            type = "data",
            connectionId = offer.payload.connectionId,
            browser = "chrome",
            label = offer.payload.label,
            reliable = false,
            serialization = "binary"
        };

        OfferMessage message = new OfferMessage()
        {
            type = "OFFER",
            src = "3489534895638",
            dst = offer.src,
            payload = payload
        };

        var jsonData = JsonConvert.SerializeObject(message);
        Debug.Log("Sending offer back - " + jsonData);
        ws.Send(jsonData);
    }

    private IEnumerator CreateAnswer(RTCSessionDescription sessionDescription)
    {
        var answer = connection.CreateAnswer();
        yield return answer;

        var answerDesc = answer.Desc;
        Debug.Log("Why are you null??: " + answerDesc.sdp);

        var localDescOp = connection.SetLocalDescription(ref answerDesc);
        yield return localDescOp;

        var remoteDescOp = connection.SetRemoteDescription(ref sessionDescription);
        yield return remoteDescOp;

        // Send desc to server for sender connection
        SdpData sdpData = new SdpData()
        {
            type = "answer",
            sdp = answerDesc.sdp
        };

        AnswerPayload payload = new AnswerPayload()
        {
            sdp = sdpData,
            type = "data",
            browser = "chrome",
            connectionId = offer.payload.connectionId
        };

        AnswerMessage answerSessionDesc = new AnswerMessage()
        {
            type = "ANSWER",
            payload = payload,
            dst = offer.src
        };

        var jsonData = JsonConvert.SerializeObject(answerSessionDesc);
        Debug.Log(jsonData);
        ws.Send(jsonData);
    }
}

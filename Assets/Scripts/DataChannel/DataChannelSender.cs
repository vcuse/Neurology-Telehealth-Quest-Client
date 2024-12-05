using Meta.WitAi.Json;
using System.Collections;
using Unity.WebRTC;
using UnityEngine;
using WebSocketSharp;

public class DataChannelSender : MonoBehaviour
{
    [SerializeField] private bool sendMessageViaChannel = false;

    private RTCPeerConnection connection;
    private RTCDataChannel dataChannel;

    private WebSocket ws;

    private bool hasReceivedAnswer = false;
    private SessionDescription receivedAnswerSessionDescTemp;

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
        ws = new WebSocket("ws://127.0.0.1:9000/peerjs?id=3489534895638&token=6789&key=peerjs");
        ws.OnMessage += (sender, e) =>
        {
            //var requestArray = e.Data.Split("!");
            //var requestType = requestArray[0];
            //var requestData = requestArray[1];

            if (e.Data.Contains("CANDIDATE"))
            {
                Debug.Log("Sender got CANDIDATE: " + e.Data);

                /*// Generate candidate data
                var candidateInit = CandidateInit.FromJSON(requestData);
                RTCIceCandidateInit init = new RTCIceCandidateInit();
                init.sdpMid = candidateInit.SdpMid;
                init.sdpMLineIndex = candidateInit.SdpMLineIndex;
                init.candidate = candidateInit.Candidate;
                RTCIceCandidate candidate = new RTCIceCandidate(init);

                // Add candidate to this connection
                connection.AddIceCandidate(candidate);*/
            }
            else
            {
                Debug.Log("Sender got message: " + e.Data);
            }

            /*switch (requestType)
            {
                case "ANSWER":
                    Debug.Log(clientId + " - Got ANSWER from Maximus: " + requestData);
                    receivedAnswerSessionDescTemp = SessionDescription.FromJSON(requestData);
                    hasReceivedAnswer = true;
                    break;
            }*/
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
            var candidateInit = new
            {
                candidate = candidate.Candidate,
                sdpMLineIndex = candidate.SdpMLineIndex ?? 0,
                sdpMid = candidate.SdpMid
            };

            var payload = new
            {
                candidate = candidateInit,
                connectionId = "3489534895638"
            };

            var message = new
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
            Type = "OFFER!",
            SessionType = offerDesc.type.ToString(),
            Sdp = offerDesc.sdp
        };
        ws.Send(offerSessionDesc.ConvertToJSON());
    }

    private IEnumerator SetRemoteDesc()
    {
        RTCSessionDescription answerSessionDesc = new RTCSessionDescription();
        answerSessionDesc.type = RTCSdpType.Answer;
        answerSessionDesc.sdp = receivedAnswerSessionDescTemp.Sdp;

        var remoteDescOp = connection.SetRemoteDescription(ref answerSessionDesc);
        yield return remoteDescOp;
    }
}

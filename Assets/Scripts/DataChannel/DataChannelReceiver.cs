using System.Collections;
using UnityEngine;
using WebSocketSharp;
using Unity.WebRTC;
using Meta.WitAi.Json;

public class DataChannelReceiver : MonoBehaviour
{
    private RTCPeerConnection connection;
    private RTCDataChannel dataChannel;

    private WebSocket ws;

    private bool hasReceivedOffer = false;
    private SessionDescription receivedOfferSessionDescTemp;

    private void Start()
    {   
        InitClient();
    }

    private void Update()
    {
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
        ws = new WebSocket("ws://127.0.0.1:9000/peerjs?id=238473289&token=67898&key=peerjs");
        ws.OnMessage += (sender, e) =>
        {
            //var requestArray = e.Data.Split("!");
            //var requestType = requestArray[0];
            //var requestData = requestArray[1];

            if (e.Data.Contains("CANDIDATE"))
            {
                Debug.Log("Receiver got CANDIDATE: " + e.Data);

                // Generate candidate data
                var candidateInit = CandidateInit.FromJSON(e.Data);
                RTCIceCandidateInit init = new RTCIceCandidateInit();
                init.sdpMid = candidateInit.sdpMid;
                init.sdpMLineIndex = candidateInit.sdpMLineIndex;
                init.candidate = candidateInit.candidate;
                RTCIceCandidate candidate = new RTCIceCandidate(init);

                // Add candidate to this connection
                connection.AddIceCandidate(candidate);
            }
            else
            {
                Debug.Log("Receiver got message: " + e.Data);
            }

            /*switch (requestType)
            {
                case "OFFER":
                    Debug.Log(clientId + " - Got OFFER from Maximus: " + requestData);
                    receivedOfferSessionDescTemp = SessionDescription.FromJSON(requestData);
                    hasReceivedOffer = true;
                    break;
                default:
                    Debug.Log(clientId + " - Maximus says: " + e.Data);
                    break;
            }*/
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
            var candidateInit = new
            {
                candidate = candidate.Candidate,
                sdpMLineIndex = candidate.SdpMLineIndex ?? 0,
                sdpMid = candidate.SdpMid
            };

            var payload = new
            {
                candidate = candidateInit,
                connectionId = "238473289"
            };

            var message = new
            {
                payload = payload,
                type = "CANDIDATE",
                dst = "3489534895638"
            };

            var jsonData = JsonConvert.SerializeObject(message);
            Debug.Log("RECEIVER sending CANDIDATE - " +  jsonData);
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
        offerSessionDesc.sdp = receivedOfferSessionDescTemp.Sdp;

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
            Type = "ANSWER!",
            SessionType = answerDesc.type.ToString(),
            Sdp = answerDesc.sdp
        };
        ws.Send(answerSessionDesc.ConvertToJSON());
    }
}

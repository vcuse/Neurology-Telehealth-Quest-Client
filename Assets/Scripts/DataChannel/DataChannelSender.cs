using Newtonsoft.Json;
using System.Collections;
using Unity.WebRTC;
using UnityEngine;
using System;
using WebSocketSharp;
using System.Threading;
using TMPro;
using UnityEngine.UI;

public class DataChannelSender : MonoBehaviour
{
    private RTCPeerConnection connection;
    private RTCPeerConnection localConnection;

    private WebSocket ws;

    private bool hasReceivedOffer = false;
    private RTCSessionDescription sessionDescription;

    private OfferMessage offer;

    float timePassed = 0f;
    private int count = 0;

    RTCDataChannel dataChannel;

    [SerializeField] private TextMeshProUGUI peerMessagesTextBox;
    [SerializeField] private bool printMessages;
    private string startText = "Peer Messages:\n";

    private MediaStream remoteStream;
    [SerializeField] private RawImage receiveImage;


    private void Start()
    {
        StartCoroutine(WebRTC.Update());

        remoteStream = new MediaStream();
        remoteStream.OnAddTrack = e =>
        {
            if (e.Track is VideoStreamTrack track)
            {
                track.OnVideoReceived += tex =>
                {
                    Debug.Log("Received video");
                    receiveImage.texture = tex;
                    receiveImage.color = Color.white;
                };
            }
            else if (e.Track is AudioStreamTrack audio)
            {

            }
        };

        RTCConfiguration config = new RTCConfiguration()
        {
            iceServers = new RTCIceServer[]
            {
                new RTCIceServer { urls = new string[]{ "stun:stun.l.google.com:19302" } }
            }
        };
        localConnection = new RTCPeerConnection(ref config);
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

        if (hasReceivedOffer && count == 0)
        {
            hasReceivedOffer = !hasReceivedOffer;
            Debug.Log("session desc" + sessionDescription.sdp);
            StartCoroutine(SetRemoteDescriptionCoroutine(sessionDescription));
            count = 1;
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

                //hasReceivedOffer = true;
                Debug.Log("We got an OFFER" + e.Data);
                offer = JsonConvert.DeserializeObject<OfferMessage>(e.Data);
                //if (offer.payload.type == "data" || offer.payload.type == "media")
                if (offer.payload.type == "media")
                {   
                    String rSDP = offer.payload.sdp.sdp;
                    Debug.Log("Remote SDP is " + rSDP);
                    Debug.Log(e.Data);

                    this.sessionDescription = new RTCSessionDescription()
                    {
                        type = RTCSdpType.Offer,
                        sdp = rSDP
                    };

                    // Await the task returned by SetLocalDescription to ensure it's applied
                    hasReceivedOffer = true;

                    // After this awaits successfully, you can check LocalDescription
                    Debug.Log("RemoteDesc Type " + connection.RemoteDescription.type);
                    Debug.Log("Remote Desc sdp" + connection.RemoteDescription.sdp);
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
            localConnection.AddIceCandidate(candidate);
            Debug.Log("CANDIDATE RECEIVED" + candidate.ToString());

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
            Debug.Log("Ice connection change: " + state);
        };

        connection.OnNegotiationNeeded = () =>
        {
            Debug.Log("Negotiation needed");
            StartCoroutine(CreateOffer());
        };

        localConnection.OnIceCandidate = candidate =>
        {
            connection.AddIceCandidate(candidate);
        };

        connection.OnDataChannel = channel =>
        {
            dataChannel = channel;
            dataChannel.OnMessage = bytes =>
            {
                var message = System.Text.Encoding.UTF8.GetString(bytes);
                if (printMessages)
                {
                    if (!peerMessagesTextBox.text.Contains(startText))
                    {
                        peerMessagesTextBox.text += startText;
                    }
                    peerMessagesTextBox.text += message + Environment.NewLine;
                }
                Debug.Log("Message received: " + message);
            };
            string text = "Adam"; // Change this to send any message you want to the other client
            dataChannel.Send(text);
        };

        connection.OnTrack = (RTCTrackEvent e) =>
        {
            Debug.Log("Connection got track");
            if (e.Track.Kind == TrackKind.Video)
            {
                remoteStream.AddTrack(e.Track);
            }
        };
    }

    private IEnumerator SetRemoteDescriptionCoroutine(RTCSessionDescription description)
    {
        var setDescOp = connection.SetRemoteDescription(ref description);

        // Wait for the operation to complete
        yield return setDescOp;

        // After setting the remote description, continue your logic
        Debug.Log("Remote description set successfully.");
        Debug.Log("RemoteDesc Type " + connection.RemoteDescription.type);
        Debug.Log("Remote Desc sdp" + connection.RemoteDescription.sdp);

        // Now, you can proceed with other actions like sending an answer, etc.
        StartCoroutine(CreateAnswer());
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

    private IEnumerator CreateAnswer()
    {
        var answer = connection.CreateAnswer();
        yield return answer;
        Debug.Log("answer variable direct" + answer.Desc.sdp);
        var answerDesc = answer.Desc;

        var localDescOp = connection.SetLocalDescription(ref answerDesc);
        yield return localDescOp;

        var remoteDescOp = localConnection.SetRemoteDescription(ref sessionDescription);
        yield return remoteDescOp;

        // Send desc to server for sender connection
        SdpData sdpData = new SdpData()
        {
            type = "answer",
            sdp = answerDesc.sdp
        };

        RTCSessionDescription sdpDesc = new RTCSessionDescription();
        sdpDesc.sdp = answerDesc.sdp; 
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
        ws.Send(jsonData);
    }
}

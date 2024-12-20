using System;

[Serializable]
public class Candidate
{
    public string type;
    public string candidate;
    public string sdpMid;
    public int sdpMLineIndex;
}
[Serializable]
public class CandidateMessage
{
    public CandidatePayload payload;
    public string type;
    public string dst;
}

[Serializable]
public class CandidatePayload
{
    public Candidate candidate;
    public string connectionId;
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

[Serializable]
public class OfferMessage
{
    public string type;
    public string src;
    public string dst;
    public OfferPayload payload;
}

[Serializable]
public class AnswerPayload
{
    public SdpData sdp;
    public string type;
    public string browser;
    public string connectionId;
}

[Serializable]
public class AnswerMessage
{
    public string type;
    public AnswerPayload payload;
    public string dst;
}
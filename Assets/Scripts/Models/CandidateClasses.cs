using System;
using UnityEngine;

[Serializable]
public class Candidate
{
    public string type;
    public string candidate;
    public string sdpMid;
    public int sdpMLineIndex;
}

public class Payload
{
    public Candidate candidate;
    public string connectionId;
}

public class Message
{
    public Payload payload;
    public string type;
    public string dst;
}
using System;
using UnityEngine;

[Serializable]
public class CandidateInit : IJSonObject<CandidateInit>
{
    public string type;
    public string candidate;
    public string sdpMid;
    public int sdpMLineIndex;

    public static CandidateInit FromJSON(string jsonString)
    {
        return JsonUtility.FromJson<CandidateInit>(jsonString);
    }

    public string ConvertToJSON()
    {
        return JsonUtility.ToJson(this);
    }
}

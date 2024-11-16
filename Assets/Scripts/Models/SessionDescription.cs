using System;
using UnityEngine;

[Serializable]
public class SessionDescription : IJSonObject<SessionDescription>
{
    public string SessionType;
    public string Sdp;

    public string ConvertToJSON()
    {
        return JsonUtility.ToJson(this);
    }

    public static SessionDescription FromJSON(string jsonString)
    {
        return JsonUtility.FromJson<SessionDescription>(jsonString);
    }
}
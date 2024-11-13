using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class FetchUsers : MonoBehaviour
{
    private string url = "https://videochat-signaling-app.ue.r.appspot.com/key=peerjs/peers";
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(FetchData());
    }

    IEnumerator FetchData()
    {
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Error: " + request.error);
        }
        else
        {
            string data = request.downloadHandler.text;
            Debug.Log("Data received: " + data);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

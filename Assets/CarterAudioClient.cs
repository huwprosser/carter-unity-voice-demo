using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System.IO;
using System;

[System.Serializable]
public struct AgentResponse
{
    public string voice;
    public string text;
    public string input;
}

[System.Serializable]
public class CarterAudioClient : MonoBehaviour {

    //target server URL
    private string server = "https://api.carterapi.com";

    //API key (Found in Dashboard > Agent > API Keys)
    public string apiKey = "API_KEY";

    //Unique user ID (Can be anything you want)
    public string uuid = "USER_ID";

    //Convert agent response json to class
    public static AgentResponse CreateFromJson(string jsonString)
    {
        return JsonUtility.FromJson<AgentResponse>(jsonString);
    }
    
    //API currently only accepts 16kHz audio
    int frameRate = 16000; 

    //Is the client currently recording audio?
    bool isRecording = false;

    //The audio clip that is being recorded or voice played back
    private AudioSource audioSource;

    List<float> tempRecording = new List<float>();

    void ResizeRecording()
    {
        if (isRecording)
        {
            //add the next second of recorded audio to temp vector
            int length = frameRate;
            float[] clipData = new float[length];
            audioSource.clip.GetData(clipData, 0);
            tempRecording.AddRange(clipData);
            Invoke("ResizeRecording", 1);
        }
    }

    // Play the agent's voice response
    IEnumerator PlayAudio(string url)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log(www.error);
            }
            else
            {
                AudioClip myClip = DownloadHandlerAudioClip.GetContent(www);
                audioSource.clip = myClip;

                // change sphere color to purple
                GameObject.Find("Sphere").GetComponent<Renderer>().material.color = new Color(128, 0, 128);

                audioSource.Play();
                www.Dispose();
            }
        }
    }

    //Uploads the audio recording to the API and returns the response
    IEnumerator Upload(byte[] data) {

       
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", data, "aud.wav");
        form.AddField("api_key", apiKey);
        form.AddField("uuid", uuid);

        UnityWebRequest www = UnityWebRequest.Post(server + "/v0/audio-chat", form);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success) {
            Debug.Log(www.error);
        }
        else {
            var response = www.downloadHandler.text;
            var output = CreateFromJson(response);

            // change color to light blue
            GameObject.Find("Sphere").GetComponent<Renderer>().material.color = new Color(0, 255, 255);
            
            //audio api returns 'text' and 'voice' fields, more coming soon!
            Debug.Log(output.input);

            //play the audio file (stream from URL)
            StartCoroutine(PlayAudio(output.voice));
        }

        www.Dispose();
    }

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = Microphone.Start(null, true, 1, 16000);
        audioSource.Play();
        Invoke("ResizeRecording", 1);
    }

    void Update()
    {
        // if key is being held down but system isn't recording, start recording
        if (Input.GetKey("space") && !isRecording)
        {
            // change color to blue
            GameObject.Find("Sphere").GetComponent<Renderer>().material.color = new Color(0, 0, 100);
            
            isRecording = true;
            Debug.Log("Listening...");
            audioSource.Stop();
            tempRecording.Clear();
            Microphone.End(null);
            audioSource.clip = Microphone.Start(null, true, 1, frameRate);
            Invoke("ResizeRecording", 1);
        } 

        if (!Input.GetKey("space") && isRecording)
        {
            isRecording = false;
            Debug.Log("Stop...");
            int length = Microphone.GetPosition(null);

            Microphone.End(null);
            float[] clipData = new float[length];
            audioSource.clip.GetData(clipData, 0);
            float[] fullClip = new float[clipData.Length + tempRecording.Count];

            for (int i = 0; i < fullClip.Length; i++)
            {
                if (i < tempRecording.Count)
                    fullClip[i] = tempRecording[i];
                else
                    fullClip[i] = clipData[i - tempRecording.Count];
            }

            tempRecording.Clear();
            audioSource.clip = AudioClip.Create("recorded samples", fullClip.Length, 1, frameRate, false);
            audioSource.clip.SetData(fullClip, 0);
            byte[] bytes = WavUtility.FromAudioClip(audioSource.clip);
            StartCoroutine(Upload(bytes));
        } 

        if(!audioSource.isPlaying && !Input.GetKey("space") && !isRecording)
        {
            // change color to white
            GameObject.Find("Sphere").GetComponent<Renderer>().material.color = new Color(255,255,255);
        }

       
    }

}
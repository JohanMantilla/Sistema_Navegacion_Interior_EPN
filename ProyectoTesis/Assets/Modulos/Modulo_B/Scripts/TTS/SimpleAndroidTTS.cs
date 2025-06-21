using UnityEngine;

public class AndroidTTS : MonoBehaviour
{
    AndroidJavaObject tts;
    AndroidJavaObject unityActivity;
    bool isInitialized = false;

    void Start()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                tts = new AndroidJavaObject("android.speech.tts.TextToSpeech", unityActivity, new TTSListener(this));
            }
        }
    }

    public void Speak(string text)
    {
        if (Application.platform == RuntimePlatform.Android && isInitialized && tts != null)
        {
            tts.Call<int>("speak", text, 0, null, null);
        }
    }

    public void OnTTSInitialized()
    {
        isInitialized = true;
        Speak("Hola, esto es una prueba de texto a voz en Unity.");
    }

    // Listener interno para recibir estado de inicializaci n del TTS
    class TTSListener : AndroidJavaProxy
    {
        AndroidTTS ttsScript;

        public TTSListener(AndroidTTS script) : base("android.speech.tts.TextToSpeech$OnInitListener")
        {
            ttsScript = script;
        }

        void onInit(int status)
        {
            Debug.Log("TTS Init Status: " + status);
            if (status == 0) // SUCCESS
            {
                ttsScript.OnTTSInitialized();
            }
        }
    }
}

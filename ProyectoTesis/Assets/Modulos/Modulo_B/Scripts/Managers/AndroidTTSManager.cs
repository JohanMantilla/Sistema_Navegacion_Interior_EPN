using UnityEngine;

public class AndroidTTSManager : MonoBehaviour
{
    private static AndroidTTSManager instance;
    private AndroidJavaObject tts;
    public bool isInitialize = false;

    public static AndroidTTSManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<AndroidTTSManager>();
                if (instance == null)
                {
                    var obj = new GameObject("AndroidTTSManager");
                    instance = obj.AddComponent<AndroidTTSManager>();
                    DontDestroyOnLoad(obj);
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        // Evitar duplicados cuando se usa DontDestroyOnLoad
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        InitializeTTS();
    }

    void InitializeTTS()
    {
        if (Application.platform != RuntimePlatform.Android)
        {
            Debug.LogWarning("TTS solo funciona en Android");
            isInitialize = false;
            return;
        }

        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                tts = new AndroidJavaObject("android.speech.tts.TextToSpeech", activity, new TTSListener(this));
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error inicializando TTS: " + e.Message);
            isInitialize = false;
        }
    }

    public void OnTTSInitialized()
    {
        isInitialize = true;
        Debug.Log("TTS inicializado correctamente");
    }

    public void Speak(string text)
    {
        if (!isInitialize || tts == null)
        {
            Debug.LogWarning("TTS no está listo");
            return;
        }

        try
        {
            // Usar la versión correcta del método speak para API level 21+
            var bundle = new AndroidJavaObject("android.os.Bundle");
            tts.Call<int>("speak", text, 0, bundle, null);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al hablar: " + e.Message);
        }
    }

    public void Stop()
    {
        if (tts != null && isInitialize)
        {
            try
            {
                tts.Call<int>("stop");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error al detener TTS: " + e.Message);
            }
        }
    }

    private void OnDestroy()
    {
        if (tts != null && isInitialize)
        {
            try
            {
                tts.Call<int>("stop");
                tts.Call("shutdown");
                tts.Dispose();
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error al cerrar TTS: " + e.Message);
            }
            finally {
                tts = null;
                isInitialize = false;
            }
        }
    }

    private class TTSListener : AndroidJavaProxy
    {
        private AndroidTTSManager androidTTSManager;

        public TTSListener(AndroidTTSManager script) : base("android.speech.tts.TextToSpeech$OnInitListener")
        {
            this.androidTTSManager = script;
        }

        public void onInit(int status)
        {
            Debug.Log($"TTS onInit called with status: {status}");
            if (status == 0) // TextToSpeech.SUCCESS
            {
                androidTTSManager.OnTTSInitialized();
            }
            else
            {
                Debug.LogError($"TTS initialization failed with status: {status}");
                androidTTSManager.isInitialize = false;
            }
        }
    }
}
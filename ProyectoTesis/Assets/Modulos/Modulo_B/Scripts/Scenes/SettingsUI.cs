using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android;
using System.Collections;

public class SettingsUI : MonoBehaviour
{
    [SerializeField] private Button btnCameraPermission;
    [SerializeField] private Button btnLocationPermission;

    [SerializeField] private Toggle tglVisualTypeSignal;
    [SerializeField] private Toggle tglAuditiveTypeSignal;
    [SerializeField] private Button btnNextSettingsUI;
    [SerializeField] private Image imgVolume;
    [SerializeField] private Sprite sprWithOutVolume;
    [SerializeField] private Sprite sprWithVolume;
    [SerializeField] private TextMeshProUGUI lblVolume;
    [SerializeField] private Image fillAreaColor;
    [SerializeField] private Color minColor;
    [SerializeField] private Color maxColor;
    [SerializeField] private Slider slider;
    [SerializeField] private string message = "Pantalla de configuración";
    [SerializeField] private bool controlSystemVolume = true;


    // Eventos estáticos
    public static event Action<bool> onVisualActive;
    public static event Action<bool> onAuditiveActive;
    public static event Action<float> onSliderVolumeChange;

    private bool isInitialized = false;
    private AndroidJavaObject audioManager;

    private bool isFirstClickCamaraPermission = false;
    private bool isFirstClickLocationPermission = false;
    private static bool hasPlayedWelcomeMessage = false;

    void Awake()
    {
        if (!isInitialized)
        {
            InitializeUIElements();
            InitializeAndroidVolumeControl();
            isInitialized = true;
        }
    }


    void InitializeAndroidVolumeControl()
    {
        if (Application.platform == RuntimePlatform.Android && controlSystemVolume)
        {
            try
            {
                AndroidJavaClass activityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject activityContext = activityClass.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaClass audioManagerClass = new AndroidJavaClass("android.media.AudioManager");
                audioManager = activityContext.Call<AndroidJavaObject>("getSystemService", "audio");
            }
            catch (Exception e)
            {
                Debug.LogWarning("Error al inicializar control de volumen Android: " + e.Message);
                controlSystemVolume = false;
            }
        }
    }


    void Start()
    {
        StartCoroutine(WaitTTS());
        LoadAndApplyPreferences();
    }

    IEnumerator WaitTTS()
    {
        float timeout = 10f; // Aumentar tiempo de espera
        float elapsed = 0f;

        // Esperar a que AndroidTTSManager esté disponible
        while (AndroidTTSManager.Instance == null && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (AndroidTTSManager.Instance == null)
        {
            Debug.LogWarning("AndroidTTSManager no está disponible");
            yield break;
        }

        // Ahora esperar a que TTS se inicialice
        elapsed = 0f;
        while (!AndroidTTSManager.Instance.isInitialize && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (AndroidTTSManager.Instance.isInitialize)
        {
            if (hasPlayedWelcomeMessage == false) {
                AndroidTTSManager.Instance.Speak(message);
                hasPlayedWelcomeMessage = true;
            }
        }
        else
        {
            Debug.LogWarning("No se pudo inicializar TTS en el tiempo esperado");
        }
    }


    void OnEnable()
    {
        if (isInitialized)
        {
            LoadAndApplyPreferences();
        }
    }

    void OnDisable()
    {
        RemoveAllListeners();
    }

    void InitializeUIElements()
    {
        AddListeners();
        LoadPlayerPreferences();
    }

    void AddListeners()
    {
        RemoveAllListeners();
        //Este ya está
        if (btnNextSettingsUI != null)
        {
            btnNextSettingsUI.onClick.AddListener(() => {
                if (AndroidTTSManager.Instance != null && AndroidTTSManager.Instance.isInitialize)
                {
                    AndroidTTSManager.Instance.Speak("Siguiente Pantalla");
                    Invoke(nameof(LoadScene), 2.5f);
                }
                else {
                    LoadScene();
                }
            });
        }

        if (tglVisualTypeSignal != null)
        {
            tglVisualTypeSignal.onValueChanged.AddListener(OnVisualToggleChanged);
        }


        if (tglAuditiveTypeSignal != null)
        {
            tglAuditiveTypeSignal.onValueChanged.AddListener(OnAuditiveToggleChanged);
        }


        if (slider != null)
        {
            slider.onValueChanged.AddListener(OnSliderVolumeChange);
        }
    }

    void RemoveAllListeners()
    {
        btnNextSettingsUI?.onClick.RemoveAllListeners();
        tglVisualTypeSignal?.onValueChanged.RemoveAllListeners();
        tglAuditiveTypeSignal?.onValueChanged.RemoveAllListeners();
        slider?.onValueChanged.RemoveAllListeners();
    }

    void LoadScene() {
        if (AndroidTTSManager.Instance != null)
        {
            AndroidTTSManager.Instance.Stop();
        }
        UIManager.Instance.LoadScene("NavigationUI");
    }

    void LoadPlayerPreferences()
    {
        // Cargar sin disparar eventos aún
        if (PlayerPrefs.HasKey("VisualTogglePreferences"))
        {
            bool visualValue = PlayerPrefs.GetInt("VisualTogglePreferences", 0) == 1;
            if (tglVisualTypeSignal != null)
                tglVisualTypeSignal.SetIsOnWithoutNotify(visualValue);
        }

        if (PlayerPrefs.HasKey("AuditiveTogglePreferences"))
        {
            bool auditiveValue = PlayerPrefs.GetInt("AuditiveTogglePreferences", 0) == 1;
            if (tglAuditiveTypeSignal != null)
                tglAuditiveTypeSignal.SetIsOnWithoutNotify(auditiveValue);
        }

        if (PlayerPrefs.HasKey("VolumePreferences"))
        {
            float volumeValue = PlayerPrefs.GetFloat("VolumePreferences", 0.5f);
            if (slider != null)
                slider.SetValueWithoutNotify(volumeValue);
            UpdateVolumeUI(volumeValue);
        }
    }

    void LoadAndApplyPreferences()
    {

        if (PlayerPrefs.HasKey("VisualTogglePreferences"))
        {
            bool visualValue = PlayerPrefs.GetInt("VisualTogglePreferences", 0) == 1;
            onVisualActive?.Invoke(visualValue);
        }

        if (PlayerPrefs.HasKey("AuditiveTogglePreferences"))
        {
            bool auditiveValue = PlayerPrefs.GetInt("AuditiveTogglePreferences", 0) == 1;
            onAuditiveActive?.Invoke(auditiveValue);
        }

        if (PlayerPrefs.HasKey("VolumePreferences"))
        {
            float volumeValue = PlayerPrefs.GetFloat("VolumePreferences", 0.5f);
            onSliderVolumeChange?.Invoke(volumeValue * 100f);
        }
    }

    void OnVisualToggleChanged(bool isOn)
    {
        if (AndroidTTSManager.Instance != null && AndroidTTSManager.Instance.isInitialize) {
            if (isOn)
            {
                AndroidTTSManager.Instance.Speak("Señales visuales activas");
            }
            else {
                AndroidTTSManager.Instance.Speak("Señales visuales desactivadas");
            }
        }

        onVisualActive?.Invoke(isOn);
        PlayerPrefs.SetInt("VisualTogglePreferences", isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    void OnAuditiveToggleChanged(bool isOn)
    {
        if (AndroidTTSManager.Instance != null && AndroidTTSManager.Instance.isInitialize)
        {
            if (isOn)
            {
                AndroidTTSManager.Instance.Speak("Señales de audio activas");
            }
            else
            {
                AndroidTTSManager.Instance.Speak("Señales de audio desactivadas");
            }
        }
        onAuditiveActive?.Invoke(isOn);
        PlayerPrefs.SetInt("AuditiveTogglePreferences", isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    void OnSliderVolumeChange(float newVolume)
    {
        UpdateVolumeUI(newVolume);

        float percent = newVolume * 100f;
        onSliderVolumeChange?.Invoke(percent);
        
        if (AndroidTTSManager.Instance != null && AndroidTTSManager.Instance.isInitialize)
        {
            AndroidTTSManager.Instance.Speak("Volumen: " + Mathf.Round(percent).ToString());
        }

        PlayerPrefs.SetFloat("VolumePreferences", newVolume);
        PlayerPrefs.Save();

        // Control del volumen del sistema
        if (controlSystemVolume && Application.platform == RuntimePlatform.Android && audioManager != null)
        {
            try
            {
                int maxVolume = audioManager.Call<int>("getStreamMaxVolume", 3); // STREAM_MUSIC
                int targetVolume = Mathf.RoundToInt(newVolume * maxVolume);
                audioManager.Call("setStreamVolume", 3, targetVolume, 0);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Error al ajustar volumen del sistema: " + e.Message);
            }
        }

    }

    private void UpdateVolumeUI(float newVolume)
    {
        float percent = Mathf.Floor(newVolume * 100);

        if (newVolume > 0)
        {
            if (imgVolume != null) imgVolume.sprite = sprWithVolume;
        }
        else
        {
            if (imgVolume != null) imgVolume.sprite = sprWithOutVolume;
        }

        if (lblVolume != null) lblVolume.text = percent.ToString();
        ChangeSliderColor(newVolume);
    }

    private void ChangeSliderColor(float newVolume)
    {
        if (fillAreaColor != null && slider != null)
        {
            fillAreaColor.color = Color.Lerp(minColor, maxColor, newVolume);
        }
    }

    private void OnDestroy()
    {
        // Detener TTS si esta UI se destruye
        if (AndroidTTSManager.Instance != null && AndroidTTSManager.Instance.isInitialize)
        {
            AndroidTTSManager.Instance.Stop();
        }
    }


}
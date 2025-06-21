using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android;

public class SettingsUI : MonoBehaviour
{
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

    [Header("Android Volume Control")]
    [Tooltip("Habilita el control del volumen del sistema en Android")]
    [SerializeField] private bool controlSystemVolume = true;


    // Eventos estáticos
    public static event Action<bool> onVisualActive;
    public static event Action<bool> onAuditiveActive;
    public static event Action<float> onSliderVolumeChange;

    private bool isInitialized = false;
    private AndroidJavaObject audioManager;

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
                /*
                // Solicitar permiso si es necesario
                if (!Permission.HasUserAuthorizedPermission(Permission.Modi))
                {
                    Permission.RequestUserPermission(Permission.ModifyAudioSettings);
                }
                */
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
        LoadAndApplyPreferences();
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

        if (btnNextSettingsUI != null)
        {
            btnNextSettingsUI.onClick.AddListener(() => UIManager.Instance.LoadScene("NavigationUI"));
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
        onVisualActive?.Invoke(isOn);
        PlayerPrefs.SetInt("VisualTogglePreferences", isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    void OnAuditiveToggleChanged(bool isOn)
    {
        onAuditiveActive?.Invoke(isOn);
        PlayerPrefs.SetInt("AuditiveTogglePreferences", isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    void OnSliderVolumeChange(float newVolume)
    {
        UpdateVolumeUI(newVolume);

        float percent = newVolume * 100f;
        onSliderVolumeChange?.Invoke(percent);

        // Guardar el volumen
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
}
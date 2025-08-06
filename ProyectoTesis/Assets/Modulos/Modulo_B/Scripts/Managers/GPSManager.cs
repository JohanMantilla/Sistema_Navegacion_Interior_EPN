using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class GPSManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI gpsText;
    public TextMeshProUGUI statusText;

    [Header("GPS Settings")]
    public float updateInterval = 2f;
    public bool autoStart = true;
    public bool showDebugInfo = true;

    // Propiedades GPS
    public float CurrentLatitude { get; private set; }
    public float CurrentLongitude { get; private set; }
    public float CurrentAltitude { get; private set; }
    public float CurrentAccuracy { get; private set; }
    public bool IsGPSEnabled { get; private set; }
    public string GPSStatus { get; private set; } = "Iniciando...";

    // Referencias JNI para Android
    private AndroidJavaClass unityPlayer;
    private AndroidJavaObject currentActivity;
    private AndroidJavaObject locationManager;
    private Coroutine gpsCoroutine;

    void Start()
    {
        // Verificar referencias UI
        if (gpsText == null)
        {
            Debug.LogError("¡Asigna el TextMeshPro para las coordenadas GPS!");
            return;
        }

        if (autoStart)
        {
            StartGPS();
        }

        UpdateUI();
    }

    public void StartGPS()
    {
        if (Application.platform != RuntimePlatform.Android)
        {
            GPSStatus = "Solo funciona en Android";
            UpdateUI();
            return;
        }

        StartCoroutine(InitializeGPS());
    }

    IEnumerator InitializeGPS()
    {
        // Inicializar JNI
        if (!InitializeJNI())
        {
            GPSStatus = "Error de inicialización";
            UpdateUI();
            yield break;
        }

        GPSStatus = "Verificando permisos...";
        UpdateUI();

        // Verificar permisos
        if (!HasLocationPermission())
        {
            RequestLocationPermission();
            yield return StartCoroutine(WaitForPermissions());

            if (!HasLocationPermission())
            {
                GPSStatus = "Permisos denegados";
                UpdateUI();
                yield break;
            }
        }

        // Verificar GPS habilitado
        if (!IsGPSProviderEnabled())
        {
            GPSStatus = "GPS deshabilitado - Abriendo configuración";
            UpdateUI();
            OpenLocationSettings();

            yield return StartCoroutine(WaitForGPSEnabled());

            if (!IsGPSProviderEnabled())
            {
                GPSStatus = "GPS no habilitado";
                UpdateUI();
                yield break;
            }
        }

        GPSStatus = "GPS activo";
        IsGPSEnabled = true;
        UpdateUI();

        // Iniciar actualizaciones
        gpsCoroutine = StartCoroutine(UpdateGPSLoop());
    }

    bool InitializeJNI()
    {
        try
        {
            unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject context = currentActivity.Call<AndroidJavaObject>("getApplicationContext");
            locationManager = context.Call<AndroidJavaObject>("getSystemService", "location");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error JNI: {e.Message}");
            return false;
        }
    }

    IEnumerator WaitForPermissions()
    {
        float timeout = 30f;
        while (timeout > 0 && !HasLocationPermission())
        {
            yield return new WaitForSeconds(1f);
            timeout -= 1f;
        }
    }

    IEnumerator WaitForGPSEnabled()
    {
        float timeout = 30f;
        while (timeout > 0 && !IsGPSProviderEnabled())
        {
            yield return new WaitForSeconds(1f);
            timeout -= 1f;
        }
    }

    bool HasLocationPermission()
    {
        try
        {
            AndroidJavaClass permissionClass = new AndroidJavaClass("android.Manifest$permission");
            string fineLocation = permissionClass.GetStatic<string>("ACCESS_FINE_LOCATION");
            AndroidJavaClass contextCompat = new AndroidJavaClass("androidx.core.content.ContextCompat");
            int result = contextCompat.CallStatic<int>("checkSelfPermission", currentActivity, fineLocation);
            return result == 0; // PERMISSION_GRANTED
        }
        catch (Exception e)
        {
            Debug.LogError($"Error verificando permisos: {e.Message}");
            return false;
        }
    }

    void RequestLocationPermission()
    {
        try
        {
            AndroidJavaClass permissionClass = new AndroidJavaClass("android.Manifest$permission");
            string fineLocation = permissionClass.GetStatic<string>("ACCESS_FINE_LOCATION");
            string coarseLocation = permissionClass.GetStatic<string>("ACCESS_COARSE_LOCATION");
            string[] permissions = { fineLocation, coarseLocation };

            AndroidJavaClass activityCompat = new AndroidJavaClass("androidx.core.app.ActivityCompat");
            activityCompat.CallStatic("requestPermissions", currentActivity, permissions, 1);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error solicitando permisos: {e.Message}");
        }
    }

    bool IsGPSProviderEnabled()
    {
        try
        {
            return locationManager.Call<bool>("isProviderEnabled", "gps");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error verificando GPS: {e.Message}");
            return false;
        }
    }

    void OpenLocationSettings()
    {
        try
        {
            AndroidJavaClass settingsClass = new AndroidJavaClass("android.provider.Settings");
            string action = settingsClass.GetStatic<string>("ACTION_LOCATION_SOURCE_SETTINGS");
            AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", action);
            currentActivity.Call("startActivity", intent);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error abriendo configuración: {e.Message}");
        }
    }

    IEnumerator UpdateGPSLoop()
    {
        while (IsGPSEnabled)
        {
            GetLastKnownLocation();
            yield return new WaitForSeconds(updateInterval);
        }
    }

    void GetLastKnownLocation()
    {
        try
        {
            AndroidJavaObject location = locationManager.Call<AndroidJavaObject>("getLastKnownLocation", "gps");

            if (location == null)
            {
                location = locationManager.Call<AndroidJavaObject>("getLastKnownLocation", "network");
            }

            if (location != null)
            {
                CurrentLatitude = (float)location.Call<double>("getLatitude");
                CurrentLongitude = (float)location.Call<double>("getLongitude");
                CurrentAltitude = (float)location.Call<double>("getAltitude");
                CurrentAccuracy = location.Call<float>("getAccuracy");

                UpdateUI();

                if (showDebugInfo)
                {
                    Debug.Log($"GPS: {CurrentLatitude:F6}, {CurrentLongitude:F6}");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error obteniendo ubicación: {e.Message}");
        }
    }

    void UpdateUI()
    {
        if (gpsText != null)
        {
            gpsText.text = $"Lat: {CurrentLatitude:F6}\nLon: {CurrentLongitude:F6}\nAlt: {CurrentAltitude:F1}m\nPrecisión: ±{CurrentAccuracy:F1}m";
        }

        if (statusText != null)
        {
            statusText.text = $"Estado: {GPSStatus}";
        }
    }

    // Métodos públicos para botones
    public void StartGPSButton()
    {
        StartGPS();
    }

    public void StopGPSButton()
    {
        StopGPS();
    }

    public void StopGPS()
    {
        IsGPSEnabled = false;

        if (gpsCoroutine != null)
        {
            StopCoroutine(gpsCoroutine);
            gpsCoroutine = null;
        }

        GPSStatus = "GPS desactivado";
        UpdateUI();
    }


    public float GetDistanceTo(float latitude, float longitude)
    {
        const float R = 6371000f; // Radio de la Tierra en metros

        float dLat = (latitude - CurrentLatitude) * Mathf.Deg2Rad;
        float dLon = (longitude - CurrentLongitude) * Mathf.Deg2Rad;

        float a = Mathf.Sin(dLat / 2) * Mathf.Sin(dLat / 2) +
                  Mathf.Cos(CurrentLatitude * Mathf.Deg2Rad) * Mathf.Cos(latitude * Mathf.Deg2Rad) *
                  Mathf.Sin(dLon / 2) * Mathf.Sin(dLon / 2);

        float c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));

        return R * c;
    }

    void OnDestroy()
    {
        StopGPS();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            if (gpsCoroutine != null)
                StopCoroutine(gpsCoroutine);
        }
        else
        {
            if (IsGPSEnabled && gpsCoroutine == null)
                gpsCoroutine = StartCoroutine(UpdateGPSLoop());
        }
    }

    void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label($"Estado: {GPSStatus}");
        GUILayout.Label($"Latitud: {CurrentLatitude:F6}");
        GUILayout.Label($"Longitud: {CurrentLongitude:F6}");
        GUILayout.Label($"Altitud: {CurrentAltitude:F1}m");

        GUILayout.EndArea();
    }
}
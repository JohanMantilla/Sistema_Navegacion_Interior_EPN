using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SimpleGPSManager : MonoBehaviour
{
    public float updateInterval = 2f;
    private Coroutine gpsCoroutine;
    [SerializeField] private GameObject dialog;
    [SerializeField] private Button acceptDialog;
    private bool isGPSEnabled = false;
    private bool shouldRestartGPS = false;

    public static event Action OnGPSReady;
    public static event Action<float, float> OnLocationUpdate; // Evento para enviar coordenadas (latitud, longitud)


    void Start()
    {
        // Asegurarse de que el diálogo está oculto al inicio
        if (dialog != null)
        {
            dialog.SetActive(false);
        }
        StartCoroutine(StartLocationService());
        if (acceptDialog != null)
        {
            acceptDialog.onClick.RemoveAllListeners();
            acceptDialog.onClick.AddListener(() => OnClickDialog());
        }
    }

    void OnClickDialog()
    {
        ShowLocationSettings();
        dialog.SetActive(false);
        shouldRestartGPS = true; // Marcar que necesitamos reiniciar el GPS
    }

    // Detectar cuando la aplicación regresa del foco
    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && shouldRestartGPS)
        {
            shouldRestartGPS = false;
            StartCoroutine(RestartLocationService());
        }
    }

    // Método para reiniciar el servicio de ubicación
    IEnumerator RestartLocationService()
    {
        yield return new WaitForSeconds(1f); // Esperar un momento antes de reiniciar

        // Detener corrutina anterior si existe
        if (gpsCoroutine != null)
        {
            StopCoroutine(gpsCoroutine);
            gpsCoroutine = null;
        }

        // Reiniciar el servicio de ubicación
        yield return StartCoroutine(StartLocationService());
    }

    IEnumerator StartLocationService()
    {
        if (!Input.location.isEnabledByUser)
        {
            isGPSEnabled = false;
            // Mostrar el diálogo solo si el GPS está desactivado
            
            if (dialog != null)
            {
                dialog.SetActive(true);
                if (AndroidTTSManager.Instance.isInitialize) {
                    AndroidTTSManager.Instance.Speak("El GPS de tu dispositivo está desactivado, activalo por favor.");
                }
            }
            
            yield break;
        }

        Input.location.Start();
        isGPSEnabled = true;
        
        // Ocultar el diálogo si el GPS está activado
        if (dialog != null)
        {
            dialog.SetActive(false);
        }
        
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            isGPSEnabled = false;
            // Mostrar diálogo si falla
            if (dialog != null)
            {
                dialog.SetActive(true);
            }
            yield break;
        }

        isGPSEnabled = true;
        OnGPSReady?.Invoke();
        gpsCoroutine = StartCoroutine(UpdateLocationLoop());
    }

    IEnumerator UpdateLocationLoop()
    {
        while (true)
        {
            if (isGPSEnabled && Input.location.status == LocationServiceStatus.Running)
            {
                LocationInfo li = Input.location.lastData;
                OnLocationUpdate?.Invoke(li.longitude, li.latitude);

            }
            yield return new WaitForSeconds(updateInterval);
        }
    }

    void OnDestroy()
    {
        Input.location.Stop();
        if (gpsCoroutine != null)
            StopCoroutine(gpsCoroutine);
    }

    public void ShowLocationSettings()
    {
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                using (AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent",
                       "android.settings.LOCATION_SOURCE_SETTINGS"))
                {
                    currentActivity.Call("startActivity", intent);
                }
            }
        }
    }
}
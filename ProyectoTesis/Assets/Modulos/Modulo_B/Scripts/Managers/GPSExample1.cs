using System.Collections;
using TMPro;
using UnityEngine;

public class GPSExample1 : MonoBehaviour
{
    public TextMeshProUGUI gpsText;
    public TextMeshProUGUI statusText;
    public float updateInterval = 2f;
    private Coroutine gpsCoroutine;

    void Start()
    {
        StartCoroutine(StartLocationService());
    }

    IEnumerator StartLocationService()
    {
        if (!Input.location.isEnabledByUser)
        {
            statusText.text = "GPS deshabilitado por el usuario";

            // Mostrar cuadro de opciones para ir a configuración
            ShowLocationSettingsDialog();

            yield break;
        }

        Input.location.Start();
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            statusText.text = "No se pudo obtener ubicación";

            // Mostrar cuadro de opciones para ir a configuración
            ShowLocationSettingsDialog();

            yield break;
        }

        statusText.text = "GPS Activo";
        gpsCoroutine = StartCoroutine(UpdateLocationLoop());
    }

    void ShowLocationSettingsDialog()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (AndroidJavaClass unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            AndroidJavaObject unityActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity");
            
            // Crear el AlertDialog
            using (AndroidJavaClass alertDialogClass = new AndroidJavaClass("android.app.AlertDialog$Builder"))
            {
                AndroidJavaObject alertDialogBuilder = alertDialogClass.CallStatic<AndroidJavaObject>("new", unityActivity);
                
                // Configurar el diálogo
                alertDialogBuilder.Call<AndroidJavaObject>("setTitle", "GPS Requerido");
                alertDialogBuilder.Call<AndroidJavaObject>("setMessage", "Para usar esta aplicación necesitas habilitar el GPS. ¿Deseas ir a la configuración?");
                
                // Botón positivo - Ir a configuración
                alertDialogBuilder.Call<AndroidJavaObject>("setPositiveButton", "Configuración", 
                    new DialogClickListener(true));
                
                // Botón negativo - Cancelar
                alertDialogBuilder.Call<AndroidJavaObject>("setNegativeButton", "Cancelar", 
                    new DialogClickListener(false));
                
                // Mostrar el diálogo
                AndroidJavaObject alertDialog = alertDialogBuilder.Call<AndroidJavaObject>("create");
                alertDialog.Call("show");
            }
        }
#endif
    }

    void OpenLocationSettings()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (AndroidJavaClass unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            AndroidJavaObject unityActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity");
            
            // Crear Intent para abrir configuración de ubicación
            using (AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", "android.settings.LOCATION_SOURCE_SETTINGS"))
            {
                unityActivity.Call("startActivity", intent);
            }
        }
#endif
    }

    IEnumerator UpdateLocationLoop()
    {
        while (true)
        {
            LocationInfo li = Input.location.lastData;
            gpsText.text = $"Lat: {li.latitude:F6}\nLon: {li.longitude:F6}\nAlt: {li.altitude:F1}m\nPrecisión: ±{li.horizontalAccuracy:F1}m";
            yield return new WaitForSeconds(updateInterval);
        }
    }

    void OnDestroy()
    {
        Input.location.Stop();
        if (gpsCoroutine != null)
            StopCoroutine(gpsCoroutine);
    }
}

// Clase para manejar los clicks del diálogo
public class DialogClickListener : AndroidJavaProxy
{
    private bool openSettings;

    public DialogClickListener(bool openSettings) : base("android.content.DialogInterface$OnClickListener")
    {
        this.openSettings = openSettings;
    }

    public void onClick(AndroidJavaObject dialog, int which)
    {
        if (openSettings)
        {
            // Abrir configuración de ubicación
#if UNITY_ANDROID && !UNITY_EDITOR
            using (AndroidJavaClass unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject unityActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity");
                
                using (AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", "android.settings.LOCATION_SOURCE_SETTINGS"))
                {
                    unityActivity.Call("startActivity", intent);
                }
            }
#endif
        }

        dialog.Call("dismiss");
    }
}
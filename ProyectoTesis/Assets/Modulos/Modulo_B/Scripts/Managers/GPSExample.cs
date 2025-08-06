using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GPSExample : MonoBehaviour
{
    [Header("UI Referencias")]
    public TextMeshProUGUI gpsText;
    public TextMeshProUGUI statusText;
    public Button enableGPSButton;
    public GameObject gpsBlockerPanel; // Panel que bloquea la interfaz

    [Header("Configuración")]
    public float updateInterval = 2f;

    private Coroutine gpsCoroutine;
    private Vector2 startCoordinates;
    private Vector2 endCoordinates;
    private bool hasStartPosition = false;
    private bool interfaceEnabled = false;

    void Start()
    {
        // Bloquear interfaz inicialmente
        SetInterfaceEnabled(false);

        // Configurar botón
        if (enableGPSButton != null)
        {
            enableGPSButton.onClick.AddListener(RequestLocationPermission);
        }

        // Verificar estado inicial
        CheckGPSStatus();
    }

    void CheckGPSStatus()
    {
        if (!Input.location.isEnabledByUser)
        {
            statusText.text = "⚠️ GPS deshabilitado. Debes activar la ubicación para continuar.";
            ShowGPSBlocker();
        }
        else
        {
            StartCoroutine(StartLocationService());
        }
    }

    void RequestLocationPermission()
    {
        // Solicitar permisos de ubicación (funciona en Android)
        if (Application.platform == RuntimePlatform.Android)
        {
            // Unity automáticamente solicita permisos cuando se inicia el servicio
            StartCoroutine(StartLocationService());
        }
        else
        {
            // Para otras plataformas, intentar iniciar directamente
            StartCoroutine(StartLocationService());
        }
    }

    IEnumerator StartLocationService()
    {
        statusText.text = "Iniciando servicio de ubicación...";

        // Verificar si el usuario tiene GPS habilitado
        if (!Input.location.isEnabledByUser)
        {
            statusText.text = "⚠️ Permisos de ubicación denegados. Ve a Configuración y activa la ubicación.";
            ShowGPSBlocker();
            yield break;
        }

        // Iniciar el servicio de ubicación
        Input.location.Start();

        // Esperar a que se inicialice
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            statusText.text = $"Obteniendo ubicación... ({maxWait}s)";
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        // Verificar si falló
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            statusText.text = "❌ Error: No se pudo obtener la ubicación. Verifica que el GPS esté activado.";
            ShowGPSBlocker();
            yield break;
        }

        // Éxito - habilitar interfaz
        statusText.text = "✅ GPS Activo - Interfaz habilitada";
        SetInterfaceEnabled(true);

        // Obtener posición inicial
        LocationInfo li = Input.location.lastData;
        startCoordinates = new Vector2(li.latitude, li.longitude);
        hasStartPosition = true;

        // Iniciar actualización continua
        gpsCoroutine = StartCoroutine(UpdateLocationLoop());
    }

    IEnumerator UpdateLocationLoop()
    {
        while (true)
        {
            LocationInfo li = Input.location.lastData;

            // Actualizar coordenadas finales
            endCoordinates = new Vector2(li.latitude, li.longitude);

            // Mostrar información simplificada
            string gpsInfo = $"📍 Ubicación Actual:\n";
            gpsInfo += $"Lat: {li.latitude:F6}\n";
            gpsInfo += $"Lon: {li.longitude:F6}\n";

            if (hasStartPosition)
            {
                gpsInfo += $"\n🚀 Posición Inicial:\n";
                gpsInfo += $"Lat: {startCoordinates.x:F6}\n";
                gpsInfo += $"Lon: {startCoordinates.y:F6}\n";

                // Calcular distancia aproximada
                float distance = CalculateDistance(startCoordinates, endCoordinates);
                gpsInfo += $"\n📏 Distancia: {distance:F1}m";
            }

            gpsText.text = gpsInfo;
            yield return new WaitForSeconds(updateInterval);
        }
    }

    void SetInterfaceEnabled(bool enabled)
    {
        interfaceEnabled = enabled;

        // Mostrar/ocultar panel bloqueador
        if (gpsBlockerPanel != null)
        {
            gpsBlockerPanel.SetActive(!enabled);
        }

        // Aquí puedes agregar más elementos de UI que quieras habilitar/deshabilitar
        // Por ejemplo: botones principales, menús, etc.
    }

    void ShowGPSBlocker()
    {
        SetInterfaceEnabled(false);

        // Mostrar botón para intentar activar GPS nuevamente
        if (enableGPSButton != null)
        {
            enableGPSButton.gameObject.SetActive(true);
        }
    }

    // Función para calcular distancia aproximada entre dos puntos
    float CalculateDistance(Vector2 pos1, Vector2 pos2)
    {
        const float earthRadius = 6371000f; // Radio de la Tierra en metros

        float lat1Rad = pos1.x * Mathf.Deg2Rad;
        float lat2Rad = pos2.x * Mathf.Deg2Rad;
        float deltaLat = (pos2.x - pos1.x) * Mathf.Deg2Rad;
        float deltaLon = (pos2.y - pos1.y) * Mathf.Deg2Rad;

        float a = Mathf.Sin(deltaLat / 2) * Mathf.Sin(deltaLat / 2) +
                  Mathf.Cos(lat1Rad) * Mathf.Cos(lat2Rad) *
                  Mathf.Sin(deltaLon / 2) * Mathf.Sin(deltaLon / 2);

        float c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));

        return earthRadius * c;
    }

    // Métodos públicos para obtener coordenadas
    public Vector2 GetStartCoordinates()
    {
        return startCoordinates;
    }

    public Vector2 GetCurrentCoordinates()
    {
        return endCoordinates;
    }

    public bool IsGPSActive()
    {
        return interfaceEnabled && Input.location.status == LocationServiceStatus.Running;
    }

    void OnDestroy()
    {
        Input.location.Stop();
        if (gpsCoroutine != null)
            StopCoroutine(gpsCoroutine);
    }

    // Método para verificar periódicamente el estado del GPS
    void Update()
    {
        // Verificar si el GPS se desactivó mientras la app está corriendo
        if (interfaceEnabled && Input.location.status != LocationServiceStatus.Running)
        {
            statusText.text = "⚠️ GPS desconectado. Reactivando...";
            SetInterfaceEnabled(false);
            CheckGPSStatus();
        }
    }
}
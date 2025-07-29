using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Extensions;

[RequireComponent(typeof(UILineRenderer))]
public class Variant2 : MonoBehaviour
{
    [Header("Referencias")]
    public MapLoader mapLoader;
    public UILineRenderer uiPathRenderer;
    public SimpleGPSManager gpsManager; // Referencia al GPS Manager

    [Header("Marcadores")]
    public RectTransform startMarker;
    public RectTransform endMarker;

    [Header("Configuración de Path")]
    public Color pathColor = Color.red;
    public float pathWidth = 5f;

    [Header("Coordenadas")]
    public double startLat;
    public double startLng;
    public double endLat;
    public double endLng;

    [Header("Path Points")]
    public List<Vector2> pathPoints = new List<Vector2>();

    [Header("Control de Marcadores")]
    public bool useGPSForStartPoint = true;
    public bool autoSetStartFromGPS = true;

    [Header("Configuración GPS")]
    public float gpsUpdateInterval = 1f; // Intervalo de actualización GPS en segundos
    public bool continuousGPSUpdate = true; // Actualización continua del GPS

    [Header("Configuración de Ruta")]
    public string targetLocationName = "Facultad de Ingeniería de Sistemas"; // Nombre de la ubicación objetivo

    private float lastGPSUpdate = 0f;
    private Vector2 lastGPSPosition = Vector2.zero;
    private bool isGPSReady = false;
    private bool showMapMarkers = true;
    private bool shouldDrawRoute = false; // Control para dibujar la ruta
    private RouteCollection currentRouteData; // Datos de la ruta actual

    void Start()
    {
        if (mapLoader == null)
        {
            Debug.LogError("❌ MapLoader no asignado!");
            return;
        }

        SetupUIPathRenderer();
        mapLoader.OnMapLoaded += OnMapLoaded;

        // Suscribirse al evento del GPS
        if (autoSetStartFromGPS)
        {
            SimpleGPSManager.OnGPSReady += OnGPSReady;
        }

        // Suscribirse a los eventos de datos JSON y selección de ubicación
        JsonDataManager.OnJsonRouteUpdated += OnRouteDataUpdated;
        ItemLocation.OnSelectLocation += OnLocationSelected;
        SettingsUI.onMapMarkersActive += OnMapMarkersActiveChanged;

        if (mapLoader.isMapLoaded)
        {
            OnMapLoaded();
        }

        disableEndMarker();
    }

    void Update()
    {
        // Actualización continua del GPS si está habilitada
        if (continuousGPSUpdate && useGPSForStartPoint && isGPSReady &&
            Time.time - lastGPSUpdate >= gpsUpdateInterval)
        {
            UpdateGPSPosition();
        }
    }

    void OnDestroy()
    {
        if (autoSetStartFromGPS)
        {
            SimpleGPSManager.OnGPSReady -= OnGPSReady;
        }

        JsonDataManager.OnJsonRouteUpdated -= OnRouteDataUpdated;
        ItemLocation.OnSelectLocation -= OnLocationSelected;
        SettingsUI.onMapMarkersActive -= OnMapMarkersActiveChanged;
    }

    // NUEVO: Manejar actualización de datos de ruta
    void OnRouteDataUpdated(RouteCollection routeData)
    {
        currentRouteData = routeData;
        Debug.Log("📊 Datos de ruta actualizados");

        // Solo procesar la ruta si debe dibujarse
        if (shouldDrawRoute)
        {
            ProcessRouteData();
        }
    }

    // NUEVO: Manejar selección de ubicación
    void OnLocationSelected(Location selectedLocation)
    {
        Debug.Log($"🎯 Ubicación seleccionada: {selectedLocation.nombre}");

        // Verificar si es la ubicación objetivo
        shouldDrawRoute = selectedLocation.nombre.Equals(targetLocationName, System.StringComparison.OrdinalIgnoreCase);

        if (shouldDrawRoute)
        {
            Debug.Log($"✅ Ubicación objetivo detectada: {targetLocationName}");
            // Si ya tenemos datos de ruta, procesarlos
            if (currentRouteData != null)
            {
                ProcessRouteData();
            }
        }
        else
        {
            Debug.Log($"❌ Ubicación no es objetivo. Ocultando ruta.");
            ClearRoute();
        }

        // Mantener la lógica original para posicionar el marcador de fin
        PositionEndMarker(selectedLocation);
    }

    // NUEVO: Procesar datos de ruta del JSON
    void ProcessRouteData()
    {
        if (currentRouteData == null || currentRouteData.features == null)
        {
            Debug.LogWarning("⚠️ No hay datos de ruta disponibles");
            return;
        }

        List<Vector2> routePoints = new List<Vector2>();

        foreach (var feature in currentRouteData.features)
        {
            if (feature.geometry.type == "LineString")
            {
                var coordinates = feature.geometry.GetLineStringCoordinates();
                if (coordinates != null)
                {
                    foreach (var coord in coordinates)
                    {
                        if (coord.Count >= 2)
                        {
                            // En GeoJSON: [longitude, latitude]
                            double longitude = coord[0];
                            double latitude = coord[1];
                            routePoints.Add(new Vector2((float)latitude, (float)longitude));
                        }
                    }
                    Debug.Log($"📍 Procesados {coordinates.Count} puntos de ruta");
                }
            }
        }

        if (routePoints.Count > 0)
        {
            SetPathPoints(routePoints);
            Debug.Log($"🛣️ Ruta dibujada con {routePoints.Count} puntos");
        }
        else
        {
            Debug.LogWarning("⚠️ No se encontraron puntos de ruta válidos");
        }
    }

    // NUEVO: Limpiar ruta solamente
    void ClearRoute()
    {
        ClearPath();
        // NO tocar los marcadores, solo limpiar la ruta
    }

    void OnMapMarkersActiveChanged(bool isActive)
    {
        showMapMarkers = isActive;
        UpdateMarkersVisibility();
    }

    void UpdateMarkersVisibility()
    {
        if (startMarker != null)
        {
            startMarker.gameObject.SetActive(showMapMarkers);
        }

        if (endMarker != null)
        {
            // Solo mostrar el marcador de fin si showMapMarkers es true Y ya se ha posicionado
            endMarker.gameObject.SetActive(showMapMarkers && endLat != 0 && endLng != 0);
        }
    }

    void OnGPSReady()
    {
        isGPSReady = true;
        if (useGPSForStartPoint && gpsManager != null)
        {
            SetStartPointFromGPS();
        }
    }

    void UpdateGPSPosition()
    {
        if (Input.location.status != LocationServiceStatus.Running)
        {
            Debug.LogWarning("⚠️ GPS no está activo");
            isGPSReady = false;
            return;
        }

        LocationInfo location = Input.location.lastData;
        Vector2 currentGPSPosition = new Vector2(location.latitude, location.longitude);

        // Solo actualizar si hay un cambio significativo en la posición
        if (Vector2.Distance(currentGPSPosition, lastGPSPosition) > 0.0001f) // ~11 metros aproximadamente
        {
            lastGPSPosition = currentGPSPosition;
            SetStartPoint(location.latitude, location.longitude);
            Debug.Log($"📍 Posición GPS actualizada: {location.latitude:F6}, {location.longitude:F6}");
        }

        lastGPSUpdate = Time.time;
    }

    void SetupUIPathRenderer()
    {
        if (uiPathRenderer == null)
            uiPathRenderer = GetComponent<UILineRenderer>();

        uiPathRenderer.color = pathColor;
        uiPathRenderer.LineThickness = pathWidth;
        uiPathRenderer.RelativeSize = false;
        uiPathRenderer.drivenExternally = true;
        uiPathRenderer.BezierSegmentsPerCurve = 10;
        uiPathRenderer.Resolution = 1f;
    }

    void OnMapLoaded()
    {
        Debug.Log("✅ Mapa cargado, posicionando marcador de inicio...");

        // Si el GPS está listo y configurado para usar GPS, actualizar punto de inicio
        if (useGPSForStartPoint && gpsManager != null && Input.location.status == LocationServiceStatus.Running)
        {
            SetStartPointFromGPS();
        }
        else
        {
            PositionStartMarker();
        }

        // Solo dibujar si debe mostrar la ruta
        if (shouldDrawRoute)
        {
            DrawUIPath();
        }
    }

    // Posicionar marcador de inicio
    void PositionStartMarker()
    {
        if (!mapLoader.isMapLoaded || startMarker == null) return;

        Vector2 startPos = mapLoader.LatLngToMapPosition(startLat, startLng);
        startMarker.anchoredPosition = startPos;
        startMarker.gameObject.SetActive(showMapMarkers);
        Debug.Log($"🎯 Marcador de inicio posicionado en: {startLat:F6}, {startLng:F6}");
    }

    // Posicionar marcador de fin (solo desde evento)
    void PositionEndMarker(Location location)
    {
        if (!mapLoader.isMapLoaded || endMarker == null) return;
        enableEndMarker();
        endLat = location.latitude;
        endLng = location.longitude;
        Vector2 endPos = mapLoader.LatLngToMapPosition(endLat, endLng);
        endMarker.anchoredPosition = endPos;

        // Aplicar visibilidad basada en la configuración
        endMarker.gameObject.SetActive(showMapMarkers);
        Debug.Log($"🏁 Marcador de fin posicionado en: {endLat:F6}, {endLng:F6}");

        // Redibujar el path cuando se posiciona el marcador de fin SOLO si debe dibujar ruta
        if (shouldDrawRoute)
        {
            DrawUIPath();
        }
    }

    // Usar GPS para punto de inicio
    public void SetStartPointFromGPS()
    {
        if (gpsManager == null)
        {
            Debug.LogWarning("⚠️ GPS Manager no asignado");
            return;
        }

        if (Input.location.status != LocationServiceStatus.Running)
        {
            Debug.LogWarning("⚠️ GPS no está activo");
            return;
        }

        LocationInfo location = Input.location.lastData;
        SetStartPoint(location.latitude, location.longitude);
        lastGPSPosition = new Vector2(location.latitude, location.longitude);
        Debug.Log($"📍 Punto de inicio actualizado desde GPS: {location.latitude:F6}, {location.longitude:F6}");
    }

    void DrawUIPath()
    {
        if (!mapLoader.isMapLoaded || pathPoints.Count < 2 || !shouldDrawRoute) return;

        Vector2[] uiPoints = new Vector2[pathPoints.Count];
        for (int i = 0; i < pathPoints.Count; i++)
        {
            uiPoints[i] = mapLoader.LatLngToMapPosition(pathPoints[i].x, pathPoints[i].y);
        }

        uiPathRenderer.Points = uiPoints;
        uiPathRenderer.SetAllDirty();
    }

    // MÉTODOS PÚBLICOS
    public void SetStartPoint(double lat, double lng)
    {
        startLat = lat;
        startLng = lng;
        PositionStartMarker();
    }

    public void SetPathPoints(List<Vector2> newPathPoints)
    {
        pathPoints = new List<Vector2>(newPathPoints);
        if (shouldDrawRoute)
        {
            DrawUIPath();
        }
    }

    public void AddPathPoint(double lat, double lng)
    {
        pathPoints.Add(new Vector2((float)lat, (float)lng));
        if (shouldDrawRoute)
        {
            DrawUIPath();
        }
    }

    public void ClearPath()
    {
        pathPoints.Clear();
        if (uiPathRenderer != null)
        {
            uiPathRenderer.Points = new Vector2[0];
        }
    }

    // NUEVO: Cambiar ubicación objetivo
    public void SetTargetLocationName(string locationName)
    {
        targetLocationName = locationName;
        Debug.Log($"🎯 Ubicación objetivo cambiada a: {locationName}");
    }

    // Método para usar coordenadas actuales del GPS como inicio
    public void UseCurrentLocationAsStart()
    {
        SetStartPointFromGPS();
    }

    // Método para obtener las coordenadas GPS actuales
    public Vector2 GetCurrentGPSCoordinates()
    {
        if (Input.location.status == LocationServiceStatus.Running)
        {
            LocationInfo location = Input.location.lastData;
            return new Vector2(location.latitude, location.longitude);
        }
        return Vector2.zero;
    }

    public void ToggleGPSForStartPoint(bool useGPS)
    {
        useGPSForStartPoint = useGPS;
        if (useGPS)
        {
            SetStartPointFromGPS();
        }
    }

    // Nuevos métodos para control de actualización GPS
    public void SetGPSUpdateInterval(float interval)
    {
        gpsUpdateInterval = interval;
    }

    public void ToggleContinuousGPSUpdate(bool continuous)
    {
        continuousGPSUpdate = continuous;
    }

    public bool IsGPSReady()
    {
        return isGPSReady && Input.location.status == LocationServiceStatus.Running;
    }

    public Vector2 GetLastGPSPosition()
    {
        return lastGPSPosition;
    }

    // NUEVO: Verificar si debe dibujar ruta
    public bool ShouldDrawRoute()
    {
        return shouldDrawRoute;
    }

    private void disableEndMarker()
    {
        if (endMarker != null)
        {
            endMarker.gameObject.SetActive(false);
        }
    }

    private void enableEndMarker()
    {
        if (endMarker != null)
        {
            endMarker.gameObject.SetActive(showMapMarkers);
        }
    }
}
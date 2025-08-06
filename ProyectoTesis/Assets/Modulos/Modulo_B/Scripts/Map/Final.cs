using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Extensions;

[RequireComponent(typeof(UILineRenderer))]
public class Final : MonoBehaviour
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
    public double startLat = 40.7128f;
    public double startLng = -74.0060f;
    public double endLat = 40.7200f;
    public double endLng = -73.9950f;

    [Header("Path Points")]
    public List<Vector2> pathPoints = new List<Vector2>();

    [Header("Control de Marcadores")]
    public bool useGPSForStartPoint = true;
    public bool autoSetStartFromGPS = true;


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

        if (mapLoader.isMapLoaded)
        {
            OnMapLoaded();
        }

        ItemLocation.OnSelectLocation += PositionEndMarker;

    }

    void OnDestroy()
    {
        if (autoSetStartFromGPS)
        {
            SimpleGPSManager.OnGPSReady -= OnGPSReady;
        }

        ItemLocation.OnSelectLocation -= PositionEndMarker;
    }

    void OnGPSReady()
    {
        if (useGPSForStartPoint && gpsManager != null)
        {
            SetStartPointFromGPS();
        }
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
        Debug.Log("✅ Mapa cargado, posicionando marcadores y path...");

        // Si el GPS está listo y configurado para usar GPS, actualizar punto de inicio
        if (useGPSForStartPoint && gpsManager != null && Input.location.status == LocationServiceStatus.Running)
        {
            SetStartPointFromGPS();
        }
        else
        {
            PositionStartMarker();
        }

        DrawUIPath();
    }

    // MÉTODO SEPARADO: Posicionar marcador de inicio
    void PositionStartMarker()
    {
        if (!mapLoader.isMapLoaded || startMarker == null) return;

        Vector2 startPos = mapLoader.LatLngToMapPosition(startLat, startLng);
        startMarker.anchoredPosition = startPos;
        Debug.Log($"🎯 Marcador de inicio posicionado en: {startLat:F6}, {startLng:F6}");
    }

    // MÉTODO SEPARADO: Posicionar marcador de fin (solo desde evento)
    void PositionEndMarker(Location location)
    {
        if (!mapLoader.isMapLoaded || endMarker == null) return;

        endLat = location.latitude;
        endLng = location.longitude;
        Vector2 endPos = mapLoader.LatLngToMapPosition(endLat, endLng);
        endMarker.anchoredPosition = endPos;
        Debug.Log($"🏁 Marcador de fin posicionado en: {endLat:F6}, {endLng:F6}");
    }

    // MÉTODO SEPARADO: Usar GPS para punto de inicio
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
        Debug.Log($"📍 Punto de inicio actualizado desde GPS: {location.latitude:F6}, {location.longitude:F6}");
    }

    // MÉTODO SEPARADO: Usar GPS para punto de fin
    public void SetEndPointFromGPS()
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
        SetEndPoint(location.latitude, location.longitude);
        Debug.Log($"📍 Punto de fin actualizado desde GPS: {location.latitude:F6}, {location.longitude:F6}");
    }

    void DrawUIPath()
    {
        if (!mapLoader.isMapLoaded || pathPoints.Count < 2) return;

        Vector2[] uiPoints = new Vector2[pathPoints.Count];
        for (int i = 0; i < pathPoints.Count; i++)
        {
            uiPoints[i] = mapLoader.LatLngToMapPosition(pathPoints[i].x, pathPoints[i].y);
        }

        uiPathRenderer.Points = uiPoints;
        uiPathRenderer.SetAllDirty();
    }

    // MÉTODOS PÚBLICOS MEJORADOS
    public void SetStartPoint(double lat, double lng)
    {
        startLat = lat;
        startLng = lng;
        PositionStartMarker();
    }

    public void SetEndPoint(double lat, double lng)
    {
        endLat = lat;
        endLng = lng;
        // No llamamos PositionEndMarker aquí, solo actualizamos las coordenadas
        if (mapLoader.isMapLoaded && endMarker != null)
        {
            Vector2 endPos = mapLoader.LatLngToMapPosition(endLat, endLng);
            endMarker.anchoredPosition = endPos;
        }
    }

    // Método para usar coordenadas actuales del GPS como inicio
    public void UseCurrentLocationAsStart()
    {
        SetStartPointFromGPS();
    }

    // Método para usar coordenadas actuales del GPS como fin
    public void UseCurrentLocationAsEnd()
    {
        SetEndPointFromGPS();
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

    public void SetPathPoints(List<Vector2> newPathPoints)
    {
        pathPoints = new List<Vector2>(newPathPoints);
        DrawUIPath();
    }

    public void AddPathPoint(double lat, double lng)
    {
        pathPoints.Add(new Vector2((float)lat, (float)lng));
        DrawUIPath();
    }

    public void ClearPath()
    {
        pathPoints.Clear();
        uiPathRenderer.Points = new Vector2[0];
    }

    // Métodos adicionales de utilidad
    public void RefreshMarkers()
    {
        PositionStartMarker();
        // No llamamos PositionEndMarker aquí ya que solo se posiciona desde el evento
    }

    public void ToggleGPSForStartPoint(bool useGPS)
    {
        useGPSForStartPoint = useGPS;
        if (useGPS)
        {
            SetStartPointFromGPS();
        }
    }
}
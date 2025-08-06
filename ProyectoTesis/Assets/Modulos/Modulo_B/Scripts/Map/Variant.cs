using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Extensions;

[RequireComponent(typeof(UILineRenderer))]
public class Variant : MonoBehaviour
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

        disableEndMarker();

        // Solo suscribirse al evento para el marcador de fin
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

        // NO posicionar el marcador de fin aquí - solo se posiciona cuando llega el evento
        DrawUIPath();
    }

    // Posicionar marcador de inicio
    void PositionStartMarker()
    {
        if (!mapLoader.isMapLoaded || startMarker == null) return;

        Vector2 startPos = mapLoader.LatLngToMapPosition(startLat, startLng);
        startMarker.anchoredPosition = startPos;
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
        Debug.Log($"🏁 Marcador de fin posicionado en: {endLat:F6}, {endLng:F6}");

        // Redibujar el path cuando se posiciona el marcador de fin
        DrawUIPath();
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
        Debug.Log($"📍 Punto de inicio actualizado desde GPS: {location.latitude:F6}, {location.longitude:F6}");
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


    private void disableEndMarker() {
        endMarker.gameObject.SetActive(false);
    }

    private void enableEndMarker() {
        endMarker.gameObject.SetActive(true);
    }

}
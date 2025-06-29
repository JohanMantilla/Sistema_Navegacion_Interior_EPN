using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Extensions;

[RequireComponent(typeof(UILineRenderer))]
public class MapMarkersAndPaths : MonoBehaviour
{
    [Header("Referencias")]
    public MapLoader mapLoader;
    public UILineRenderer uiPathRenderer; // Reemplazo para LineRenderer

    [Header("Marcadores")]
    public RectTransform startMarker; // Cambiado a RectTransform para UI
    public RectTransform endMarker;   // Cambiado a RectTransform para UI

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

    void Start()
    {
        if (mapLoader == null)
        {
            Debug.LogError("❌ MapLoader no asignado!");
            return;
        }

        // Configurar el UILineRenderer
        SetupUIPathRenderer();

        mapLoader.OnMapLoaded += OnMapLoaded;
        //mapLoader.OnMapLoadError += OnMapLoadError;

        if (mapLoader.isMapLoaded)
        {
            OnMapLoaded();
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

        // Mejorar la calidad de la línea
        uiPathRenderer.BezierSegmentsPerCurve = 10;
        uiPathRenderer.Resolution = 1f;
    }

    void OnMapLoaded()
    {
        Debug.Log("✅ Mapa cargado, posicionando marcadores y path...");
        PositionMarkers();
        DrawUIPath();
    }

    void PositionMarkers()
    {
        if (!mapLoader.isMapLoaded) return;

        if (startMarker != null)
        {
            Vector2 startPos = mapLoader.LatLngToMapPosition(startLat, startLng);
            startMarker.anchoredPosition = startPos;
        }

        if (endMarker != null)
        {
            Vector2 endPos = mapLoader.LatLngToMapPosition(endLat, endLng);
            endMarker.anchoredPosition = endPos;
        }
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
        uiPathRenderer.SetAllDirty(); // Forzar actualización visual
    }

    // Métodos públicos actualizados
    public void SetStartPoint(double lat, double lng)
    {
        startLat = lat;
        startLng = lng;
        if (startMarker != null && mapLoader.isMapLoaded)
        {
            startMarker.anchoredPosition = mapLoader.LatLngToMapPosition(lat, lng);
        }
    }

    public void SetEndPoint(double lat, double lng)
    {
        endLat = lat;
        endLng = lng;
        if (endMarker != null && mapLoader.isMapLoaded)
        {
            endMarker.anchoredPosition = mapLoader.LatLngToMapPosition(lat, lng);
        }
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

    // Resto de métodos permanecen igual...
}
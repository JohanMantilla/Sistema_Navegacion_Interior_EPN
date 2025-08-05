using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Extensions;

[RequireComponent(typeof(UILineRenderer))]
public class Variant2 : MonoBehaviour
{
    [Header("Referencias")]
    public MapLoader mapLoader;
    public UILineRenderer uiPathRenderer;
    public SimpleGPSManager gpsManager;
    public SimpleNavigationAudio audioManager;

    [Header("Marcadores")]
    public RectTransform startMarker;
    public RectTransform endMarker;
    public double startLat, startLng, endLat, endLng;
    public bool useGPSForStartPoint = true;
    public bool autoSetStartFromGPS = true;

    [Header("Ruta")]
    public string targetLocationName = "Teatro Politécnico";
    public Color pathColor = Color.red;
    public List<Vector2> pathPoints = new();
    private RouteCollection currentRouteData;
    private bool shouldDrawRoute = false;
    private bool showMapMarkers = true;

    [Header("GPS")]
    public float gpsUpdateInterval = 1f;
    public bool continuousGPSUpdate = true;
    private Vector2 lastGPSPosition = Vector2.zero;
    private float lastGPSUpdate = 0f;
    private bool isGPSReady = false;

    [Header("Audio")]
    public bool enableAudio = true;

    void Start()
    {
        if (mapLoader == null) return;

        SetupUIPathRenderer();
        mapLoader.OnMapLoaded += OnMapLoaded;
        JsonDataManager.OnJsonRouteUpdated += OnRouteDataUpdated;
        ItemLocation.OnSelectLocation += OnLocationSelected;
        SettingsUI.onMapMarkersActive += OnMapMarkersActiveChanged;

        if (autoSetStartFromGPS)
            SimpleGPSManager.OnGPSReady += OnGPSReady;

        if (audioManager == null)
            audioManager = FindFirstObjectByType<SimpleNavigationAudio>();

        if (mapLoader.isMapLoaded)
            OnMapLoaded();

        endMarker?.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!continuousGPSUpdate || !useGPSForStartPoint || !isGPSReady) return;
        if (Time.time - lastGPSUpdate < gpsUpdateInterval) return;
        UpdateGPSPosition();
    }

    void OnDestroy()
    {
        if (autoSetStartFromGPS)
            SimpleGPSManager.OnGPSReady -= OnGPSReady;

        JsonDataManager.OnJsonRouteUpdated -= OnRouteDataUpdated;
        ItemLocation.OnSelectLocation -= OnLocationSelected;
        SettingsUI.onMapMarkersActive -= OnMapMarkersActiveChanged;

        if (enableAudio)
            audioManager?.StopNavigation();
    }

    void OnRouteDataUpdated(RouteCollection routeData)
    {
        currentRouteData = routeData;
        if (shouldDrawRoute) ProcessRouteData();
    }

    void OnLocationSelected(Location location)
    {
        shouldDrawRoute = location.nombre.Equals(targetLocationName, System.StringComparison.OrdinalIgnoreCase);

        if (shouldDrawRoute)
        {
            ProcessRouteData();
            if (enableAudio) audioManager?.StartNavigation();
        }
        else
        {
            ClearPath();
            audioManager?.StopNavigation();
        }

        PositionEndMarker(location);
    }

    void OnMapLoaded()
    {
        if (useGPSForStartPoint && gpsManager != null && Input.location.status == LocationServiceStatus.Running)
            SetStartPointFromGPS();
        else
            PositionStartMarker();

        if (shouldDrawRoute)
            DrawUIPath();
    }

    void OnMapMarkersActiveChanged(bool isActive)
    {
        showMapMarkers = isActive;
        UpdateMarkersVisibility();
    }

    void OnGPSReady()
    {
        isGPSReady = true;
        if (useGPSForStartPoint)
            SetStartPointFromGPS();
    }

    void UpdateGPSPosition()
    {
        if (Input.location.status != LocationServiceStatus.Running)
        {
            isGPSReady = false;
            return;
        }

        var location = Input.location.lastData;
        Vector2 currentGPS = new(location.latitude, location.longitude);

        if (Vector2.Distance(currentGPS, lastGPSPosition) > 0.0001f)
        {
            SetStartPoint(location.latitude, location.longitude);
            lastGPSPosition = currentGPS;
        }

        lastGPSUpdate = Time.time;
    }

    void SetupUIPathRenderer()
    {
        uiPathRenderer ??= GetComponent<UILineRenderer>();
        uiPathRenderer.color = pathColor;
        uiPathRenderer.LineThickness = 5f;
        uiPathRenderer.RelativeSize = false;
        uiPathRenderer.drivenExternally = true;
        uiPathRenderer.BezierSegmentsPerCurve = 10;
        uiPathRenderer.Resolution = 1f;
    }

    void ProcessRouteData()
    {
        if (currentRouteData?.features == null) return;

        var routePoints = new List<Vector2>();

        foreach (var feature in currentRouteData.features)
        {
            if (feature.geometry.type != "LineString") continue;
            var coords = feature.geometry.GetLineStringCoordinates();
            if (coords == null) continue;

            foreach (var coord in coords)
            {
                if (coord.Count >= 2)
                    routePoints.Add(new Vector2((float)coord[1], (float)coord[0]));
            }
        }

        if (routePoints.Count > 0)
            SetPathPoints(routePoints);
    }

    void DrawUIPath()
    {
        if (!mapLoader.isMapLoaded || pathPoints.Count < 2 || !shouldDrawRoute) return;

        Vector2[] uiPoints = new Vector2[pathPoints.Count];
        for (int i = 0; i < pathPoints.Count; i++)
            uiPoints[i] = mapLoader.LatLngToMapPosition(pathPoints[i].x, pathPoints[i].y);

        uiPathRenderer.Points = uiPoints;
        uiPathRenderer.SetAllDirty();
    }

    void PositionStartMarker()
    {
        if (!mapLoader.isMapLoaded || startMarker == null) return;

        startMarker.anchoredPosition = mapLoader.LatLngToMapPosition(startLat, startLng);
        startMarker.gameObject.SetActive(showMapMarkers);
    }

    void PositionEndMarker(Location location)
    {
        if (!mapLoader.isMapLoaded || endMarker == null) return;

        endLat = location.latitude;
        endLng = location.longitude;
        endMarker.anchoredPosition = mapLoader.LatLngToMapPosition(endLat, endLng);
        endMarker.gameObject.SetActive(showMapMarkers);

        if (shouldDrawRoute)
            DrawUIPath();
    }

    void UpdateMarkersVisibility()
    {
        startMarker?.gameObject.SetActive(showMapMarkers);
        uiPathRenderer?.gameObject.SetActive(showMapMarkers);
        endMarker?.gameObject.SetActive(showMapMarkers && endLat != 0 && endLng != 0);
    }

    public void SetStartPointFromGPS()
    {
        if (Input.location.status != LocationServiceStatus.Running) return;

        var location = Input.location.lastData;
        SetStartPoint(location.latitude, location.longitude);
        lastGPSPosition = new Vector2(location.latitude, location.longitude);
    }

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
            DrawUIPath();
    }

    public void ClearPath()
    {
        pathPoints.Clear();
        if (uiPathRenderer != null)
            uiPathRenderer.Points = new Vector2[0];
    }

    public void ToggleAudio(bool enabled)
    {
        enableAudio = enabled;
        if (!enabled) audioManager?.StopNavigation();
    }

    public bool IsAudioActive() => audioManager != null && audioManager.IsNavigationActive();
    public bool IsGPSReady() => isGPSReady && Input.location.status == LocationServiceStatus.Running;
}

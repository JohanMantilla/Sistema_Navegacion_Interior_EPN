using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class MapWithPathfinding : MonoBehaviour
{
    [Header("IMPORTANTE: Configura estos campos")]
    public string apiKey = ""; // PON TU API KEY AQUÍ
    public RawImage mapImage; // ARRASTRA AQUÍ TU RAWIMAGE

    [Header("Configuración del mapa")]
    public double centerLatitude = 40.7128f;
    public double centerLongitude = -74.0060f;
    public int zoom = 15;
    public int mapWidth = 640;
    public int mapHeight = 640;

    [Header("Marcadores y Path")]
    public Transform startMarker; // GameObject para marcador de inicio
    public Transform endMarker;   // GameObject para marcador de fin
    public LineRenderer pathLine; // LineRenderer para dibujar el path
    public Color pathColor = Color.red;
    public float pathWidth = 5f;

    [Header("Coordenadas de Pathfinding")]
    public double startLat = 40.7128f;
    public double startLng = -74.0060f;
    public double endLat = 40.7200f;
    public double endLng = -73.9950f;

    // Variables para conversión de coordenadas
    private double mapCenterLat, mapCenterLng;
    private double metersPerPixelLat, metersPerPixelLng;
    private RectTransform mapRectTransform;

    // Lista de puntos del path (tus coordenadas de pathfinding)
    [Header("Tu Path de Pathfinding")]
    public List<Vector2> pathPoints = new List<Vector2>(); // En coordenadas lat/lng

    void Start()
    {
        // Verificaciones iniciales
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("❌ API KEY NO CONFIGURADA!");
            return;
        }

        if (mapImage == null)
        {
            Debug.LogError("❌ RAWIMAGE NO ASIGNADO!");
            return;
        }

        mapRectTransform = mapImage.GetComponent<RectTransform>();

        // Configurar LineRenderer si existe
        SetupLineRenderer();

        Debug.Log("✅ Iniciando carga del mapa con marcadores...");
        StartCoroutine(LoadMapWithMarkers());
    }

    void SetupLineRenderer()
    {
        if (pathLine != null)
        {
            // ✅ IGUAL QUE TU CÓDIGO AR: usar startColor y endColor
            pathLine.startColor = pathColor;
            pathLine.endColor = pathColor;
            pathLine.startWidth = pathWidth;
            pathLine.endWidth = pathWidth;
            pathLine.useWorldSpace = true; // ✅ IGUAL QUE TU AR

            // ✅ IGUAL QUE TU AR: configuraciones de calidad
            pathLine.numCornerVertices = 4;
            pathLine.numCapVertices = 4;
            pathLine.material = null; // Usar material por defecto

            Debug.Log("✅ LineRenderer configurado igual que en AR");
        }
    }

    IEnumerator LoadMapWithMarkers()
    {
        // Configurar centro del mapa
        mapCenterLat = centerLatitude;
        mapCenterLng = centerLongitude;

        // Calcular resolución del mapa (metros por pixel)
        CalculateMapResolution();

        // Construir URL con marcadores estáticos opcionales
        string url = BuildMapURL();

        Debug.Log("🔄 URL generada: " + url);

        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;

            if (texture != null)
            {
                // Asignar textura al mapa
                mapImage.texture = texture;
                mapImage.color = Color.white;
                mapImage.uvRect = new Rect(0, 0, 1, 1);

                Debug.Log($"✅ Mapa cargado: {texture.width}x{texture.height}");

                // Posicionar marcadores
                PositionMarkers();

                // Dibujar path personalizado
                DrawCustomPath();

                Canvas.ForceUpdateCanvases();
            }
        }
        else
        {
            Debug.LogError("❌ Error cargando mapa: " + request.error);
        }

        request.Dispose();
    }

    string BuildMapURL()
    {
        string url = $"https://maps.googleapis.com/maps/api/staticmap?" +
                    $"center={mapCenterLat},{mapCenterLng}" +
                    $"&zoom={zoom}" +
                    $"&size={mapWidth}x{mapHeight}" +
                    $"&maptype=roadmap";

        // ✅ OPCIÓN: Agregar marcadores estáticos
        url += $"&markers=color:green|label:S|{startLat},{startLng}";
        url += $"&markers=color:red|label:E|{endLat},{endLng}";

        // ✅ OPCIÓN: Agregar path directamente en Google Maps
        if (pathPoints.Count > 1)
        {
            url += "&path=color:0xff0000ff|weight:5"; // Rojo con alpha

            foreach (Vector2 point in pathPoints)
            {
                url += $"|{point.x},{point.y}";
            }
        }

        url += $"&key={apiKey}";

        Debug.Log("🗺️ URL con path incluido: " + url);
        return url;
    }

    void CalculateMapResolution()
    {
        // Calcular metros por pixel basado en el zoom y latitud
        double latRad = mapCenterLat * Mathf.Deg2Rad;
        double metersPerPixel = 156543.03392 * Mathf.Cos((float)latRad) / Mathf.Pow(2, zoom);

        metersPerPixelLat = metersPerPixel;
        metersPerPixelLng = metersPerPixel / Mathf.Cos((float)latRad);

        Debug.Log($"Resolución: {metersPerPixel:F2} metros por pixel");
    }

    void PositionMarkers()
    {
        // Posicionar marcador de inicio
        if (startMarker != null)
        {
            Vector2 startPos = LatLngToMapPosition(startLat, startLng);
            startMarker.localPosition = new Vector3(startPos.x, startPos.y, 0);
            Debug.Log($"Marcador inicio posicionado en: {startPos}");
        }

        // Posicionar marcador de fin
        if (endMarker != null)
        {
            Vector2 endPos = LatLngToMapPosition(endLat, endLng);
            endMarker.localPosition = new Vector3(endPos.x, endPos.y, 0);
            Debug.Log($"Marcador fin posicionado en: {endPos}");
        }
    }

    void DrawCustomPath()
    {
        if (pathLine == null || pathPoints.Count < 2)
        {
            Debug.LogError("❌ LineRenderer null o pocos puntos");
            return;
        }

        // ✅ IGUAL QUE TU AR: Configurar primero positionCount
        pathLine.positionCount = pathPoints.Count;

        // Convertir puntos de lat/lng a posiciones WORLD del mapa
        Vector3[] linePoints = new Vector3[pathPoints.Count];

        for (int i = 0; i < pathPoints.Count; i++)
        {
            Vector2 mapPos = LatLngToMapPosition(pathPoints[i].x, pathPoints[i].y);

            // ✅ Convertir a posición WORLD usando el RectTransform
            Vector3 worldPos = mapRectTransform.TransformPoint(new Vector3(mapPos.x, mapPos.y, 0));
            linePoints[i] = new Vector3(worldPos.x, worldPos.y, worldPos.z - 0.1f);

            Debug.Log($"Punto {i}: Local({mapPos.x:F2}, {mapPos.y:F2}) -> World({worldPos.x:F2}, {worldPos.y:F2})");
        }

        // ✅ IGUAL QUE TU AR: SetPositions después de configurar positionCount
        pathLine.SetPositions(linePoints);

        // ✅ APLICAR COLORES IGUAL QUE EN TU AR
        pathLine.startColor = pathColor;
        pathLine.endColor = pathColor;

        Debug.Log($"✅ Path dibujado con {linePoints.Length} puntos");
        Debug.Log($"✅ Colores aplicados: start={pathLine.startColor}, end={pathLine.endColor}");
    }

    // FUNCIÓN CLAVE: Convertir coordenadas lat/lng a posición en el mapa
    Vector2 LatLngToMapPosition(double lat, double lng)
    {
        // Calcular diferencia en grados desde el centro
        double deltaLat = lat - mapCenterLat;
        double deltaLng = lng - mapCenterLng;

        // Convertir a metros
        double metersLat = deltaLat * 111320.0; // Aproximadamente 111,320 metros por grado de latitud
        double metersLng = deltaLng * 111320.0 * Mathf.Cos((float)(mapCenterLat * Mathf.Deg2Rad));

        // Convertir metros a pixels
        float pixelX = (float)(metersLng / metersPerPixelLng);
        float pixelY = (float)(metersLat / metersPerPixelLat);

        // Convertir pixels a coordenadas locales del RectTransform
        float mapPixelWidth = mapRectTransform.rect.width;
        float mapPixelHeight = mapRectTransform.rect.height;

        // Normalizar y escalar a coordenadas locales
        float localX = (pixelX / mapWidth) * mapPixelWidth;
        float localY = (pixelY / mapHeight) * mapPixelHeight;

        return new Vector2(localX, localY);
    }

    // FUNCIÓN INVERSA: Convertir posición del mapa a lat/lng
    Vector2 MapPositionToLatLng(Vector2 localPosition)
    {
        float mapPixelWidth = mapRectTransform.rect.width;
        float mapPixelHeight = mapRectTransform.rect.height;

        // Convertir coordenadas locales a pixels
        float pixelX = (localPosition.x / mapPixelWidth) * mapWidth;
        float pixelY = (localPosition.y / mapPixelHeight) * mapHeight;

        // Convertir pixels a metros
        double metersLng = pixelX * metersPerPixelLng;
        double metersLat = pixelY * metersPerPixelLat;

        // Convertir metros a grados
        double deltaLng = metersLng / (111320.0 * Mathf.Cos((float)(mapCenterLat * Mathf.Deg2Rad)));
        double deltaLat = metersLat / 111320.0;

        // Agregar al centro del mapa
        double finalLat = mapCenterLat + deltaLat;
        double finalLng = mapCenterLng + deltaLng;

        return new Vector2((float)finalLat, (float)finalLng);
    }

    // MÉTODOS PÚBLICOS PARA TU PATHFINDING
    public void SetStartPoint(double lat, double lng)
    {
        startLat = lat;
        startLng = lng;
        if (startMarker != null)
        {
            Vector2 pos = LatLngToMapPosition(lat, lng);
            startMarker.localPosition = new Vector3(pos.x, pos.y, 0);
        }
    }

    public void SetEndPoint(double lat, double lng)
    {
        endLat = lat;
        endLng = lng;
        if (endMarker != null)
        {
            Vector2 pos = LatLngToMapPosition(lat, lng);
            endMarker.localPosition = new Vector3(pos.x, pos.y, 0);
        }
    }

    public void SetPathPoints(List<Vector2> newPathPoints)
    {
        pathPoints = new List<Vector2>(newPathPoints);
        DrawCustomPath();
    }

    public void AddPathPoint(double lat, double lng)
    {
        pathPoints.Add(new Vector2((float)lat, (float)lng));
        DrawCustomPath();
    }

    public void ClearPath()
    {
        pathPoints.Clear();
        if (pathLine != null)
        {
            pathLine.positionCount = 0;
        }
    }

    // MÉTODO PARA DETECTAR CLICKS EN EL MAPA
    public void OnMapClick(Vector2 screenPosition)
    {
        // Convertir posición de screen a local del mapa
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mapRectTransform, screenPosition, null, out localPoint);

        // Convertir a lat/lng
        Vector2 latLng = MapPositionToLatLng(localPoint);

        Debug.Log($"Click en mapa: {latLng.x}, {latLng.y}");

        // Aquí puedes usar estas coordenadas para tu pathfinding
        // Por ejemplo: RunPathfinding(startLat, startLng, latLng.x, latLng.y);
    }

    // MÉTODOS DE TESTING
    [ContextMenu("Test - Agregar Puntos de Ejemplo")]
    public void AddExamplePath()
    {
        pathPoints.Clear();
        pathPoints.Add(new Vector2(40.7128f, -74.0060f)); // Punto 1
        pathPoints.Add(new Vector2(40.7150f, -74.0040f)); // Punto 2
        pathPoints.Add(new Vector2(40.7180f, -74.0020f)); // Punto 3
        pathPoints.Add(new Vector2(40.7200f, -73.9950f)); // Punto 4

        DrawCustomPath();
        Debug.Log("✅ Path de ejemplo agregado");
    }

    [ContextMenu("Recargar Mapa")]
    public void ReloadMap()
    {
        StartCoroutine(LoadMapWithMarkers());
    }

    [ContextMenu("Debug - Comparar con AR Setup")]
    public void DebugARComparison()
    {
        Debug.Log("=== COMPARACIÓN CON SETUP AR ===");

        if (pathLine == null)
        {
            Debug.LogError("❌ pathLine es NULL");
            return;
        }

        Debug.Log($"✅ useWorldSpace: {pathLine.useWorldSpace} (debería ser true como en AR)");
        Debug.Log($"✅ startColor: {pathLine.startColor}");
        Debug.Log($"✅ endColor: {pathLine.endColor}");
        Debug.Log($"✅ startWidth: {pathLine.startWidth}");
        Debug.Log($"✅ endWidth: {pathLine.endWidth}");
        Debug.Log($"✅ positionCount: {pathLine.positionCount}");
        Debug.Log($"✅ numCornerVertices: {pathLine.numCornerVertices}");
        Debug.Log($"✅ numCapVertices: {pathLine.numCapVertices}");
        Debug.Log($"✅ GameObject activo: {pathLine.gameObject.activeInHierarchy}");
        Debug.Log($"✅ Component habilitado: {pathLine.enabled}");

        // Mostrar primeras posiciones
        for (int i = 0; i < Mathf.Min(3, pathLine.positionCount); i++)
        {
            Vector3 pos = pathLine.GetPosition(i);
            Debug.Log($"✅ Posición {i}: {pos}");
        }

        Debug.Log("=== CONFIGURACIÓN DEBE SER IGUAL A TU AR ===");
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class MapLoader : MonoBehaviour
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

    // Variables públicas para acceso externo
    [HideInInspector] public double mapCenterLat, mapCenterLng;
    [HideInInspector] public double metersPerPixelLat, metersPerPixelLng;
    [HideInInspector] public RectTransform mapRectTransform;
    [HideInInspector] public bool isMapLoaded = false;

    // Eventos para notificar cuando el mapa se carga
    public System.Action OnMapLoaded;
    public System.Action<string> OnMapLoadError;

    void Start()
    {
        // Verificaciones iniciales
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("❌ API KEY NO CONFIGURADA!");
            OnMapLoadError?.Invoke("API KEY no configurada");
            return;
        }

        if (mapImage == null)
        {
            Debug.LogError("❌ RAWIMAGE NO ASIGNADO!");
            OnMapLoadError?.Invoke("RawImage no asignado");
            return;
        }

        mapRectTransform = mapImage.GetComponent<RectTransform>();

        Debug.Log("✅ Iniciando carga del mapa...");
        LoadMap();
    }

    public void LoadMap()
    {
        StartCoroutine(LoadMapCoroutine());
    }

    IEnumerator LoadMapCoroutine()
    {
        isMapLoaded = false;

        // Configurar centro del mapa
        mapCenterLat = centerLatitude;
        mapCenterLng = centerLongitude;

        // Calcular resolución del mapa (metros por pixel)
        CalculateMapResolution();

        // Construir URL básica sin marcadores
        string url = BuildBasicMapURL();

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

                isMapLoaded = true;
                OnMapLoaded?.Invoke();

                Canvas.ForceUpdateCanvases();
            }
        }
        else
        {
            string errorMsg = "Error cargando mapa: " + request.error;
            Debug.LogError("❌ " + errorMsg);
            OnMapLoadError?.Invoke(errorMsg);
        }

        request.Dispose();
    }

    string BuildBasicMapURL()
    {
        string url = $"https://maps.googleapis.com/maps/api/staticmap?" +
                    $"center={mapCenterLat},{mapCenterLng}" +
                    $"&zoom={zoom}" +
                    $"&size={mapWidth}x{mapHeight}" +
                    $"&maptype=roadmap" +
                    $"&key={apiKey}";

        return url;
    }

    public string BuildMapURLWithMarkersAndPath(double startLat, double startLng, double endLat, double endLng, System.Collections.Generic.List<Vector2> pathPoints = null)
    {
        string url = $"https://maps.googleapis.com/maps/api/staticmap?" +
                    $"center={mapCenterLat},{mapCenterLng}" +
                    $"&zoom={zoom}" +
                    $"&size={mapWidth}x{mapHeight}" +
                    $"&maptype=roadmap";

        // Agregar marcadores estáticos
        url += $"&markers=color:green|label:S|{startLat},{startLng}";
        url += $"&markers=color:red|label:E|{endLat},{endLng}";

        // Agregar path si existe
        if (pathPoints != null && pathPoints.Count > 1)
        {
            url += "&path=color:0xff0000ff|weight:5"; // Rojo con alpha

            foreach (Vector2 point in pathPoints)
            {
                url += $"|{point.x},{point.y}";
            }
        }

        url += $"&key={apiKey}";

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

    // FUNCIÓN CLAVE: Convertir coordenadas lat/lng a posición en el mapa
    public Vector2 LatLngToMapPosition(double lat, double lng)
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
    public Vector2 MapPositionToLatLng(Vector2 localPosition)
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

    // MÉTODO PARA DETECTAR CLICKS EN EL MAPA
    public Vector2 GetLatLngFromScreenPosition(Vector2 screenPosition)
    {
        // Convertir posición de screen a local del mapa
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mapRectTransform, screenPosition, null, out localPoint);

        // Convertir a lat/lng
        return MapPositionToLatLng(localPoint);
    }

    // Método para cambiar el centro del mapa y recargar
    public void SetMapCenter(double lat, double lng)
    {
        centerLatitude = lat;
        centerLongitude = lng;
        LoadMap();
    }

    // Método para cambiar el zoom y recargar
    public void SetZoom(int newZoom)
    {
        zoom = newZoom;
        LoadMap();
    }

    [ContextMenu("Recargar Mapa")]
    public void ReloadMap()
    {
        LoadMap();
    }
}
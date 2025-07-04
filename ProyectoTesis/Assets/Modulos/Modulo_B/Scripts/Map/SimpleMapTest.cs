using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class SimpleMapTest : MonoBehaviour
{
    [Header("IMPORTANTE: Configura estos campos")]
    public string apiKey = ""; // PON TU API KEY AQUÍ
    public RawImage mapImage; // ARRASTRA AQUÍ TU RAWIMAGE

    [Header("Configuración del mapa")]
    public double latitude = 40.7128f;
    public double longitude = -74.0060f;
    public int zoom = 10;

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

        Debug.Log("✅ Iniciando carga del mapa...");
        StartCoroutine(LoadSimpleMap());
    }

    IEnumerator LoadSimpleMap()
    {
        // URL simplificada para testing
        string url = $"https://maps.googleapis.com/maps/api/staticmap?" +
                    $"center={latitude},{longitude}" +
                    $"&zoom={zoom}" +
                    $"&size=400x400" +
                    $"&maptype=roadmap" +
                    $"&key={apiKey}";

        Debug.Log("🔄 URL generada: " + url);
        Debug.Log("🔍 COPIA ESTA URL Y PRUÉBALA EN EL NAVEGADOR:");
        Debug.Log(url);

        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);

        yield return request.SendWebRequest();

        Debug.Log("📡 Respuesta recibida. Código: " + request.responseCode);

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;

            if (texture != null)
            {
                // Verificar si la imagen contiene el error de Google
                if (texture.width == 256 && texture.height == 256)
                {
                    Debug.LogError("❌ POSIBLE ERROR DE GOOGLE MAPS API!");
                    Debug.LogError("La imagen tiene 256x256 (tamaño típico de error de Google)");
                    Debug.LogError("Verifica tu API Key y configuración");
                }

                Debug.Log($"✅ Textura creada: {texture.width}x{texture.height}");

                // SOLUCION 1: Asegurar que el RawImage es visible
                mapImage.color = Color.white;
                mapImage.material = null; // Quitar cualquier material custom

                // SOLUCION 2: Asignar la textura
                mapImage.texture = texture;

                // SOLUCION 3: Ajustar UV Rect (importante!)
                mapImage.uvRect = new Rect(0, 0, 1, 1);

                // SOLUCION 4: Forzar recalculo
                mapImage.SetNativeSize();

                Debug.Log("✅ MAPA ASIGNADO AL RAWIMAGE!");
                Debug.Log($"RawImage Color: {mapImage.color}");
                Debug.Log($"RawImage UV Rect: {mapImage.uvRect}");

                // Forzar actualización de la UI
                Canvas.ForceUpdateCanvases();
                mapImage.enabled = false;
                mapImage.enabled = true;
            }
            else
            {
                Debug.LogError("❌ Textura es null");
            }
        }
        else
        {
            Debug.LogError("❌ Error en request: " + request.error);
            Debug.LogError("❌ Respuesta HTTP: " + request.responseCode);

            // Intentar leer el contenido del error
            if (request.downloadHandler != null)
            {
                Debug.LogError("❌ Contenido de error: " + request.downloadHandler.text);
            }
        }

        request.Dispose();
    }

    // Método para testing manual
    [ContextMenu("Cargar Mapa Manual")]
    public void LoadMapManual()
    {
        StartCoroutine(LoadSimpleMap());
    }

    // Verificar configuración
    [ContextMenu("Verificar Configuración")]
    public void VerifySetup()
    {
        Debug.Log("=== DIAGNÓSTICO COMPLETO ===");
        Debug.Log("API Key configurada: " + !string.IsNullOrEmpty(apiKey));
        Debug.Log("RawImage asignado: " + (mapImage != null));

        if (mapImage != null)
        {
            Debug.Log("RawImage activo: " + mapImage.gameObject.activeInHierarchy);
            Debug.Log("RawImage habilitado: " + mapImage.enabled);
            Debug.Log("RawImage tamaño: " + mapImage.rectTransform.sizeDelta);
            Debug.Log("RawImage color: " + mapImage.color);
            Debug.Log("RawImage alpha: " + mapImage.color.a);
            Debug.Log("RawImage material: " + (mapImage.material != null ? mapImage.material.name : "null"));
            Debug.Log("RawImage UV Rect: " + mapImage.uvRect);
            Debug.Log("Canvas encontrado: " + (mapImage.canvas != null));
            Debug.Log("Textura actual: " + (mapImage.texture != null ? $"{mapImage.texture.width}x{mapImage.texture.height}" : "null"));

            // Verificar jerarquía
            Transform parent = mapImage.transform.parent;
            while (parent != null)
            {
                Debug.Log($"Padre: {parent.name} - Activo: {parent.gameObject.activeInHierarchy}");
                parent = parent.parent;
            }
        }

        Debug.Log("=== FIN DIAGNÓSTICO ===");
    }

    // Método para testing con textura de prueba
    [ContextMenu("Test con Textura Básica")]
    public void TestWithBasicTexture()
    {
        if (mapImage != null)
        {
            // Crear textura de prueba
            Texture2D testTexture = new Texture2D(256, 256);
            Color[] colors = new Color[256 * 256];

            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.red;
            }

            testTexture.SetPixels(colors);
            testTexture.Apply();

            mapImage.texture = testTexture;
            Debug.Log("✅ Textura de prueba asignada (debería verse roja)");
        }
    }
}
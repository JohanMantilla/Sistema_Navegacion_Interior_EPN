using UnityEngine;
using UnityEngine.XR.ARFoundation;
using System.Collections.Generic;
using TMPro;

public class DrawBbox : MonoBehaviour
{
    [Header("Referencias")]
    public ARCameraManager arCamera;
    public GameObject bboxPrefab; // GameObject con LineRenderer + TextMeshPro hijo

    [Header("Configuraci�n de L�neas")]
    [SerializeField] private float baseLineWidth = 0.02f; // Ancho base m�s delgado
    [SerializeField] private float minLineWidth = 0.008f; // Ancho m�nimo para distancias cercanas
    [SerializeField] private float maxLineWidth = 0.04f; // Ancho m�ximo para distancias lejanas

    [Header("Configuraci�n de Texto")]
    [SerializeField] private float baseTextSize = 0.8f; // Tama�o base del texto m�s grande
    [SerializeField] private float minTextSize = 0.4f; // Tama�o m�nimo del texto
    [SerializeField] private float maxTextSize = 1.5f; // Tama�o m�ximo del texto
    [SerializeField] private float textDistanceOffset = 0.1f; // Offset del texto respecto a la caja

    [Header("Configuraci�n de Distancia")]
    [SerializeField] private float minProjectionDistance = 0.5f; // Distancia m�nima de proyecci�n
    [SerializeField] private float maxProjectionDistance = 20f; // Distancia m�xima de proyecci�n

    private List<GameObject> activeBBoxes = new List<GameObject>();

    void Start()
    {
        WebSocketClient.OnChangeObjectionDetection += OnObjectDetectionUpdated;
        Debug.Log("ARBBoxDetector iniciado, esperando primer JSON...");
    }

    void OnDestroy()
    {
        WebSocketClient.OnChangeObjectionDetection -= OnObjectDetectionUpdated;
    }

    private void OnObjectDetectionUpdated(ObjectDetection detectionData)
    {
        if (detectionData != null)
        {
            ProcessDetectionData(detectionData);
            Debug.Log($"Procesados {detectionData.objects?.Count ?? 0} objetos detectados");
        }
    }

    void ProcessDetectionData(ObjectDetection data)
    {
        ClearAllBBoxes();

        if (data?.objects == null || data.objects.Count == 0)
        {
            Debug.LogWarning("No objects detected in data.");
            return;
        }

        foreach (Objects obj in data.objects)
        {
            if (obj.bbox != null && obj.bbox.Length == 4)
            {
                DrawBoundingBox(obj);
            }

            if (obj.distance <= 5 && obj.name == "person")
            {
                // L�gica espec�fica para personas cercanas
            }
        }
    }

    void DrawBoundingBox(Objects objData)
    {
        if (bboxPrefab == null || arCamera == null) return;

        Camera cam = arCamera.GetComponent<Camera>();
        if (cam == null) return;

        // Clampar la distancia para evitar deformaciones
        float clampedDistance = Mathf.Clamp(objData.distance, minProjectionDistance, maxProjectionDistance);

        // YOLOv8 bbox format: [x_min, y_min, x_max, y_max]
        float xMin = objData.bbox[0];
        float yMin = objData.bbox[1];
        float xMax = objData.bbox[2];
        float yMax = objData.bbox[3];

        // Convertir a coordenadas de viewport
        Vector2 minViewport = new Vector2(
            xMin / Screen.width,
            1f - (yMax / Screen.height)
        );

        Vector2 maxViewport = new Vector2(
            xMax / Screen.width,
            1f - (yMin / Screen.height)
        );

        // Calcular las esquinas del bounding box
        Vector3[] corners = new Vector3[5];
        corners[0] = cam.ViewportToWorldPoint(new Vector3(minViewport.x, minViewport.y, clampedDistance));
        corners[1] = cam.ViewportToWorldPoint(new Vector3(maxViewport.x, minViewport.y, clampedDistance));
        corners[2] = cam.ViewportToWorldPoint(new Vector3(maxViewport.x, maxViewport.y, clampedDistance));
        corners[3] = cam.ViewportToWorldPoint(new Vector3(minViewport.x, maxViewport.y, clampedDistance));
        corners[4] = corners[0]; // Cerrar el rect�ngulo

        // Instanciar el prefab
        Vector3 centerPosition = (corners[0] + corners[2]) * 0.5f;
        GameObject bbox = Instantiate(bboxPrefab, centerPosition, Quaternion.identity);

        // Configurar el LineRenderer
        ConfigureLineRenderer(bbox, corners, objData);

        // Configurar el texto
        ConfigureText(bbox, corners, objData, cam);

        activeBBoxes.Add(bbox);
    }

    void ConfigureLineRenderer(GameObject bbox, Vector3[] corners, Objects objData)
    {
        LineRenderer line = bbox.GetComponent<LineRenderer>();
        if (line == null) return;

        line.positionCount = corners.Length;
        line.SetPositions(corners);

        Color classColor = GetClassColor(objData.name);
        line.startColor = classColor;
        line.endColor = classColor;

        // Calcular ancho de l�nea basado en distancia
        float lineWidth = CalculateLineWidth(objData.distance);
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;

        line.useWorldSpace = true;

        // Mejorar la calidad visual de las l�neas
        line.numCornerVertices = 4;
        line.numCapVertices = 4;
    }

    void ConfigureText(GameObject bbox, Vector3[] corners, Objects objData, Camera cam)
    {
        TextMeshPro textInfo = bbox.GetComponentInChildren<TextMeshPro>();
        if (textInfo == null) return;

        // Formato del texto con saltos de l�nea
        textInfo.text = $"{objData.name} ({objData.confidence:F2})\n" +
                       $"Speed: {objData.speed:F1}m/s\n" +
                       $"Distance: {objData.distance:F1}m";

        // Posicionar el texto arriba del bounding box con mejor offset
        Vector3 topLeft = corners[3] + new Vector3(0.05f,-0.1f,0f); // Centro del borde superior
        Vector3 cameraDirection = (cam.transform.position - topLeft).normalized;
        Vector3 textPosition = topLeft + cameraDirection * textDistanceOffset;

        textInfo.transform.position = textPosition;

        // Orientar el texto hacia la c�mara
        Vector3 directionToCamera = cam.transform.position - textInfo.transform.position;
        textInfo.transform.rotation = Quaternion.LookRotation(-directionToCamera);

        // Configurar color y escala
        Color classColor = GetClassColor(objData.name);
        textInfo.color = classColor;

        float textScale = CalculateTextScale(objData.distance);
        textInfo.transform.localScale = Vector3.one * textScale;

        // Configurar propiedades del texto
        textInfo.fontSize = 10;
        textInfo.fontSizeMin = 6;
        textInfo.fontSizeMax = 14;
        textInfo.enableAutoSizing = true;
        textInfo.overflowMode = TextOverflowModes.Overflow;
        textInfo.enableWordWrapping = false;
        textInfo.alignment = TextAlignmentOptions.Center;

        // Mejorar legibilidad
        textInfo.fontStyle = FontStyles.Bold;
        textInfo.outlineWidth = 0.1f;
        textInfo.outlineColor = Color.black;
    }

    // Calcular ancho de l�nea basado en distancia
    float CalculateLineWidth(float distance)
    {
        if (distance <= 1f)
        {
            // Para distancias muy cercanas, usar l�neas m�s delgadas
            float factor = distance / 1f; // 0 a 1
            return Mathf.Lerp(minLineWidth, baseLineWidth * 0.7f, factor);
        }
        else if (distance <= 5f)
        {
            // Transici�n gradual de 1m a 5m
            float factor = (distance - 1f) / 4f; // 0 a 1
            return Mathf.Lerp(baseLineWidth * 0.7f, baseLineWidth, factor);
        }
        else
        {
            // Para distancias lejanas, l�neas m�s gruesas para visibilidad
            float factor = Mathf.Clamp01((distance - 5f) / 15f); // 0 a 1 para 5m-20m
            return Mathf.Lerp(baseLineWidth, maxLineWidth, factor);
        }
    }

    // Calcular escala de texto mejorada
    float CalculateTextScale(float distance)
    {
        if (distance <= 1f)
        {
            // Para objetos muy cercanos, escala m�s conservadora
            return minTextSize * (0.8f + 0.2f * distance); // 80% a 100% del m�nimo
        }
        else if (distance <= 3f)
        {
            // Transici�n suave de 1m a 3m
            float factor = (distance - 1f) / 2f;
            return Mathf.Lerp(minTextSize, baseTextSize, factor);
        }
        else if (distance <= 10f)
        {
            // Rango normal de 3m a 10m
            float factor = (distance - 3f) / 7f;
            return Mathf.Lerp(baseTextSize, baseTextSize * 1.2f, factor);
        }
        else
        {
            // Distancias lejanas, texto m�s grande para visibilidad
            float factor = Mathf.Clamp01((distance - 10f) / 10f);
            return Mathf.Lerp(baseTextSize * 1.2f, maxTextSize, factor);
        }
    }

    Color GetClassColor(string className)
    {
        switch (className?.ToLower())
        {
            case "person": return new Color(1f, 1f, 0f, 0.9f);        // Amarillo
            case "car": return new Color(1f, 0f, 0f, 0.9f);           // Rojo
            case "bicycle": return new Color(0f, 1f, 0f, 0.9f);       // Verde
            case "motorcycle": return new Color(1f, 0.5f, 0f, 0.9f);  // Naranja
            case "bus": return new Color(0f, 0f, 1f, 0.9f);           // Azul
            case "truck": return new Color(0.5f, 0f, 0.5f, 0.9f);     // P�rpura
            case "dog": return new Color(0f, 1f, 1f, 0.9f);           // Cian
            case "cat": return new Color(1f, 0f, 1f, 0.9f);           // Magenta
            case "bird": return new Color(1f, 1f, 0.5f, 0.9f);        // Amarillo claro
            case "traffic light": return new Color(0.5f, 1f, 0.5f, 0.9f); // Verde claro
            case "stop sign": return new Color(0.8f, 0.2f, 0.2f, 0.9f);   // Rojo oscuro
            default: return new Color(1f, 1f, 1f, 0.9f);              // Blanco por defecto
        }
    }

    void ClearAllBBoxes()
    {
        foreach (GameObject bbox in activeBBoxes)
        {
            if (bbox != null)
            {
                Destroy(bbox);
            }
        }
        activeBBoxes.Clear();
    }

    public void UpdateDetectionData(ObjectDetection newData)
    {
        OnObjectDetectionUpdated(newData);
    }

    public int GetActiveBBoxCount()
    {
        return activeBBoxes.Count;
    }
}
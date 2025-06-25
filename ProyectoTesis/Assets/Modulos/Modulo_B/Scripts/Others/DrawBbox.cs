using UnityEngine;
using UnityEngine.XR.ARFoundation;
using System.Collections.Generic;
using TMPro;

public class DrawBbox : MonoBehaviour
{
    [Header("Referencias")]
    public ARCameraManager arCamera;
    public GameObject bboxPrefab; // GameObject con LineRenderer + TextMeshPro hijo

    [Header("Configuración")]
    [SerializeField] private float baseTextSize = 0.03f; // Tamaño base del texto (más pequeño)
    [SerializeField] private float minTextSize = 0.02f; // Tamaño mínimo del texto
    [SerializeField] private float maxTextSize = 0.08f; // Tamaño máximo del texto (más controlado)
    [SerializeField] private float distanceScaleFactor = 1.5f; // Factor de escala basado en distancia
    [SerializeField] private float maxDistance = 20f; // Distancia máxima para el cálculo de escala

    private List<GameObject> activeBBoxes = new List<GameObject>();

    void Start()
    {
        // Suscribirse al evento del JsonDataManager
        JsonDataManager.OnChangeObjectionDetection += OnObjectDetectionUpdated;
        Debug.Log("ARBBoxDetector iniciado, esperando primer JSON...");
    }

    void OnDestroy()
    {
        // Desuscribirse para evitar memory leaks
        JsonDataManager.OnChangeObjectionDetection -= OnObjectDetectionUpdated;
    }

    // Método llamado cuando se actualiza la detección de objetos
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
        ClearAllBBoxes(); // Limpiar bounding boxes anteriores

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

            // Lógica adicional para personas cercanas
            if (obj.distance <= 5 && obj.name == "person")
            {
                // Aquí puedes agregar lógica específica para personas cercanas
            }
        }
    }

    void DrawBoundingBox(Objects objData)
    {
        if (bboxPrefab == null || arCamera == null) return;

        Camera cam = arCamera.GetComponent<Camera>();
        if (cam == null) return;

        // YOLOv8 bbox format: [x_min, y_min, x_max, y_max] en coordenadas de píxeles
        float xMin = objData.bbox[0];
        float yMin = objData.bbox[1];
        float xMax = objData.bbox[2];
        float yMax = objData.bbox[3];

        // Convertir coordenadas de píxeles a coordenadas de viewport (0-1)
        // En Unity, el viewport tiene (0,0) en bottom-left, pero YOLOv8 tiene (0,0) en top-left
        Vector2 minViewport = new Vector2(
            xMin / Screen.width,
            1f - (yMax / Screen.height) // Invertir Y y usar yMax para bottom
        );

        Vector2 maxViewport = new Vector2(
            xMax / Screen.width,
            1f - (yMin / Screen.height) // Invertir Y y usar yMin para top
        );

        // Definir la distancia de proyección en el mundo 3D
        float projectionDistance = objData.distance;

        // Calcular las 4 esquinas del bounding box en coordenadas del mundo
        Vector3[] corners = new Vector3[5];

        // Esquinas en orden: bottom-left, bottom-right, top-right, top-left, bottom-left (para cerrar)
        corners[0] = cam.ViewportToWorldPoint(new Vector3(minViewport.x, minViewport.y, projectionDistance)); // Bottom-left
        corners[1] = cam.ViewportToWorldPoint(new Vector3(maxViewport.x, minViewport.y, projectionDistance)); // Bottom-right
        corners[2] = cam.ViewportToWorldPoint(new Vector3(maxViewport.x, maxViewport.y, projectionDistance)); // Top-right
        corners[3] = cam.ViewportToWorldPoint(new Vector3(minViewport.x, maxViewport.y, projectionDistance)); // Top-left
        corners[4] = corners[0]; // Cerrar el rectángulo

        // Instanciar el prefab de la caja
        Vector3 centerPosition = (corners[0] + corners[2]) * 0.5f; // Centro del bounding box
        GameObject bbox = Instantiate(bboxPrefab, centerPosition, Quaternion.identity);

        // Configurar el LineRenderer
        LineRenderer line = bbox.GetComponent<LineRenderer>();
        if (line != null)
        {
            line.positionCount = corners.Length;
            line.SetPositions(corners);

            Color classColor = GetClassColor(objData.name);
            line.startColor = classColor;
            line.endColor = classColor;

            // Configurar propiedades del LineRenderer para mejor visualización
            line.useWorldSpace = true;
            line.material = line.material; // Asegurar que tiene un material
        }

        // Configurar el TextMeshPro hijo
        TextMeshPro textInfo = bbox.GetComponentInChildren<TextMeshPro>();
        if (textInfo != null)
        {
            // Formatear el texto en una sola línea con separadores
            textInfo.text = $"{objData.name} ({objData.confidence:F2})\n" +
                $"Speed: {objData.speed:F1}m/s\n" +
                $"Distance: {objData.distance:F1}m";

            // Posicionar el texto ligeramente arriba del bounding box
            Vector3 textOffset = new Vector3(0.01f, -0.02f, 0); // Pequeño offset hacia arriba
            Vector3 textPosition = corners[3] + textOffset; // Top-left corner + offset
            textInfo.transform.position = textPosition;

            // Hacer que el texto mire hacia la cámara
            Vector3 directionToCamera = arCamera.transform.position - textInfo.transform.position;
            textInfo.transform.rotation = Quaternion.LookRotation(-directionToCamera);

            // Configurar el color del texto igual que la clase
            Color classColor = GetClassColor(objData.name);
            textInfo.color = classColor;

            // Escalar el texto basado en la distancia para que sea proporcional
            float textScale = CalculateTextScale(objData.distance);
            textInfo.transform.localScale = Vector3.one * textScale;

            // Configurar propiedades del texto para una sola línea
            textInfo.fontSize = 5; // Tamaño base de fuente
            textInfo.fontSizeMin = 2;
            textInfo.fontSizeMax = 10;
            textInfo.enableAutoSizing = true;
            textInfo.overflowMode = TextOverflowModes.Overflow; // Permitir que el texto se extienda
            textInfo.enableWordWrapping = false; // Desactivar wrap para mantener en una línea
            textInfo.alignment = TextAlignmentOptions.Left; // Alinear a la izquierda
        }

        activeBBoxes.Add(bbox);
    }

    // Calcular el tamaño del texto basado en la distancia
    float CalculateTextScale(float distance)
    {
        // Normalizar la distancia entre 0 y 1 basado en maxDistance
        float normalizedDistance = Mathf.Clamp01(distance / maxDistance);

        // Aplicar una curva logarítmica para evitar que sea demasiado grande cuando está cerca
        float scaleFactor = Mathf.Log(1 + normalizedDistance * distanceScaleFactor) / Mathf.Log(1 + distanceScaleFactor);

        // Interpolar entre el tamaño mínimo y máximo
        float scale = Mathf.Lerp(minTextSize, maxTextSize, scaleFactor);

        // Para objetos muy cercanos (menos de 2m), aplicar una reducción adicional
        if (distance < 2f)
        {
            float closeReduction = Mathf.Clamp01(distance / 2f); // 0 cuando está a 0m, 1 cuando está a 2m
            scale *= (0.5f + 0.5f * closeReduction); // Reducir entre 50% y 100%
        }

        return scale;
    }

    // Obtener color basado en la clase del objeto (igual que YOLOv8)
    Color GetClassColor(string className)
    {
        switch (className?.ToLower())
        {
            case "person": return new Color(1f, 1f, 0f, 0.8f);        // Amarillo
            case "car": return new Color(1f, 0f, 0f, 0.8f);           // Rojo
            case "bicycle": return new Color(0f, 1f, 0f, 0.8f);       // Verde
            case "motorcycle": return new Color(1f, 0.5f, 0f, 0.8f);  // Naranja
            case "bus": return new Color(0f, 0f, 1f, 0.8f);           // Azul
            case "truck": return new Color(0.5f, 0f, 0.5f, 0.8f);     // Púrpura
            case "dog": return new Color(0f, 1f, 1f, 0.8f);           // Cian
            case "cat": return new Color(1f, 0f, 1f, 0.8f);           // Magenta
            case "bird": return new Color(1f, 1f, 0.5f, 0.8f);        // Amarillo claro
            case "traffic light": return new Color(0.5f, 1f, 0.5f, 0.8f); // Verde claro
            case "stop sign": return new Color(0.8f, 0.2f, 0.2f, 0.8f);   // Rojo oscuro
            default: return new Color(1f, 1f, 1f, 0.8f);              // Blanco por defecto
        }
    }

    void ClearAllBBoxes()
    {
        // Destruye todos los GameObjects de las cajas activas
        foreach (GameObject bbox in activeBBoxes)
        {
            if (bbox != null)
            {
                Destroy(bbox);
            }
        }

        // Limpia la lista
        activeBBoxes.Clear();
    }

    // Método público para actualizar datos manualmente (opcional)
    public void UpdateDetectionData(ObjectDetection newData)
    {
        OnObjectDetectionUpdated(newData);
    }

    // Método para obtener estadísticas de rendimiento
    public int GetActiveBBoxCount()
    {
        return activeBBoxes.Count;
    }

}
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
    //[SerializeField] private float textOffset = 0.8f; // Offset del texto respecto a la caja
    //[SerializeField] private float textMargin = 0.05f; // Margen dentro del bbox
    //[SerializeField] private bool textOutsideBox = false; // Opción para texto fuera de la caja

    private List<GameObject> activeBBoxes = new List<GameObject>();
    //private bool hasProcessedFirstJson = false;
    public AudioManager audioManager;

    void Start()
    {
        // Suscribirse al evento del JsonDataManager
        JsonDataManager.OnChangeObjectionDetection += OnObjectDetectionUpdated;

        // NO procesar datos embebidos aquí, esperar al primer JSON
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
            //hasProcessedFirstJson = true;
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
                DrawBoundingBox(
                    new Vector2(obj.bbox[0], obj.bbox[1]), // min
                    new Vector2(obj.bbox[2], obj.bbox[3]), // max
                    obj.distance,
                    GetClassColor(obj.classes),
                    obj
                );
            }

            if (obj.distance <= 5 && obj.classes == "person") {
                audioManager.PlayAudio();
            }


        }
    }

    void DrawBoundingBox(Vector2 bboxMin, Vector2 bboxMax, float distance, Color color, Objects objData)
    {
        if (bboxPrefab == null || arCamera == null) return;

        // Convertir coordenadas de pantalla a Viewport
        Vector2 minViewport = ScreenToViewport(bboxMin);
        Vector2 maxViewport = ScreenToViewport(bboxMax);

        // Calcular esquinas de la caja en orden correcto
        Camera cam = arCamera.GetComponent<Camera>();
        Vector3[] corners = new Vector3[5];

        // Orden: bottomLeft, bottomRight, topRight, topLeft, bottomLeft (para cerrar)
        corners[0] = cam.ViewportToWorldPoint(new Vector3(minViewport.x, minViewport.y, distance)); // TopLeft
        corners[1] = cam.ViewportToWorldPoint(new Vector3(maxViewport.x, minViewport.y, distance)); // TopRight
        corners[2] = cam.ViewportToWorldPoint(new Vector3(maxViewport.x, maxViewport.y, distance)); // ButtonRight
        corners[3] = cam.ViewportToWorldPoint(new Vector3(minViewport.x, maxViewport.y, distance)); // topLeft
        corners[4] = corners[0]; // Cerrar el rectangulo

        // Instanciar el prefab de la caja
        GameObject bbox = Instantiate(bboxPrefab, corners[0], Quaternion.identity);

        // Configurar el LineRenderer
        LineRenderer line = bbox.GetComponent<LineRenderer>();
        if (line != null)
        {
            line.positionCount = corners.Length;
            line.SetPositions(corners);
            line.startColor = color;
            line.endColor = color;
        }

        // Configurar el TextMeshPro hijo
        TextMeshPro textInfo = bbox.GetComponentInChildren<TextMeshPro>();
        if (textInfo != null)
        {
            textInfo.text = $"Class: {objData.classes}\n" +
                           $"Speed: {objData.speed:F1} m/s\n" +
                           $"Dist: {objData.distance:F1} m";

           
           
            // Calcular posición del texto
            Vector3 textPosition = corners[0];
            textInfo.transform.position = textPosition;
            


            // Hacer que el texto mire a la cámara
            textInfo.transform.LookAt(arCamera.transform);
            textInfo.transform.Rotate(0, 180, 0); // Voltear para legibilidad
        }

        activeBBoxes.Add(bbox);
    }

    Vector2 ScreenToViewport(Vector2 screenPos)
    {
        return new Vector2(
            screenPos.x / Screen.width,
            1f - (screenPos.y / Screen.height) // Invertir Y para coordenadas de pantalla
        );
    }

    Color GetClassColor(string className)
    {
        switch (className?.ToLower())
        {
            case "person": return Color.yellow;
            case "car": return Color.red;
            case "bicycle": return Color.green;
            case "dog": return Color.cyan;
            case "cat": return Color.magenta;
            default: return Color.white;
        }
    }

    void ClearAllBBoxes()
    {
        // Destruye todos los GameObjects de las cajas activas
        foreach (GameObject bbox in activeBBoxes)
        {
            if (bbox != null)
            {
                Destroy(bbox); // Esto destruirá automáticamente el LineRenderer y el TextMeshPro hijo
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

    // Método para debug - mostrar datos embebidos si no hay JSON
    [ContextMenu("Test with Embedded Data")]
    public void TestWithEmbeddedData()
    {
        ObjectDetection testData = new ObjectDetection
        {
            timestamp = System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            objects = new List<Objects>
            {
                new Objects
                {
                    object_id = 1,
                    classes = "person",
                    confidence = 0.95f,
                    bbox = new float[] {100, 200, 300, 400}, // Coordenadas en píxeles como tu JSON
                    speed = 1.2f,
                    distance = 3.5f,
                    direction = "north"
                },
                new Objects
                {
                    object_id = 2,
                    classes = "car",
                    confidence = 0.89f,
                    bbox = new float[] {150, 250, 350, 450}, // Coordenadas en píxeles como tu JSON
                    speed = 5.8f,
                    distance = 7.1f,
                    direction = "east"
                }
            }
        };

        OnObjectDetectionUpdated(testData);
    }
}
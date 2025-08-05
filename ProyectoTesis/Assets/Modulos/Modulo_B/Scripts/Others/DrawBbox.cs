using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class DrawBbox : MonoBehaviour
{
    [Header("Referencias")]
    public ARCameraManager arCamera;
    public GameObject bboxPrefab;

    [Header("Configuración de Líneas")]
    [SerializeField] private float baseLineWidth = 0.02f;
    [SerializeField] private float minLineWidth = 0.008f;
    [SerializeField] private float maxLineWidth = 0.04f;

    [Header("Configuración de Texto")]
    [SerializeField] private float baseTextSize = 0.8f;
    [SerializeField] private float minTextSize = 0.4f;
    [SerializeField] private float maxTextSize = 1.5f;
    [SerializeField] private float textDistanceOffset = 0.1f;

    [Header("Configuración de Distancia")]
    [SerializeField] private float minProjectionDistance = 0.5f;
    [SerializeField] private float maxProjectionDistance = 20f;

    private List<GameObject> activeBBoxes = new List<GameObject>();
    private bool showDrawBbox = true;
    private bool showObjectInformation = true;

    void Start()
    {
        // CRÍTICO: Cargar las preferencias guardadas al iniciar
        LoadDrawBboxPreference();
        LoadObjectInformationPreference();

        Debug.Log($"[DrawBbox NavigationUI] START - showDrawBbox: {showDrawBbox}, showObjectInformation: {showObjectInformation}");

        WebSocketClient.OnChangeObjectionDetection += OnObjectDetectionUpdated;
        SettingsUI.onDrawBboxActive += OnShowDrawBboxActiveChanged;
        SettingsUI.onObjectInformationActive += OnShowObjectInformationActiveChanged;

        Debug.Log("[DrawBbox NavigationUI] Eventos suscritos correctamente");
    }

    void LoadDrawBboxPreference()
    {
        if (PlayerPrefs.HasKey("DrawBbox"))
        {
            bool savedValue = PlayerPrefs.GetInt("DrawBbox", 1) == 1;
            showDrawBbox = savedValue;
            Debug.Log($"[DrawBbox NavigationUI] Preferencia DrawBbox cargada: {savedValue}");
        }
        else
        {
            // Valor por defecto si no hay preferencia
            showDrawBbox = true;
            Debug.Log("[DrawBbox NavigationUI] Sin preferencia DrawBbox guardada, usando valor por defecto: true");
        }
    }

    void LoadObjectInformationPreference()
    {
        if (PlayerPrefs.HasKey("ShowObjectInformationChanged"))
        {
            bool savedValue = PlayerPrefs.GetInt("ShowObjectInformationChanged", 1) == 1;
            showObjectInformation = savedValue;
            Debug.Log($"[DrawBbox NavigationUI] Preferencia ObjectInformation cargada: {savedValue}");
        }
        else
        {
            // Valor por defecto si no hay preferencia
            showObjectInformation = true;
            Debug.Log("[DrawBbox NavigationUI] Sin preferencia ObjectInformation guardada, usando valor por defecto: true");
        }
    }

    void OnDestroy()
    {
        WebSocketClient.OnChangeObjectionDetection -= OnObjectDetectionUpdated;
        SettingsUI.onDrawBboxActive -= OnShowDrawBboxActiveChanged;
        SettingsUI.onObjectInformationActive -= OnShowObjectInformationActiveChanged;
        Debug.Log("[DrawBbox NavigationUI] Eventos desuscritos");
    }

    private void OnObjectDetectionUpdated(ObjectDetection detectionData)
    {
        if (detectionData != null)
        {
            ProcessDetectionData(detectionData);
        }
    }

    void OnShowDrawBboxActiveChanged(bool isActive)
    {
        Debug.Log($"[DrawBbox NavigationUI] ===== DRAWBBOX TOGGLE CHANGED =====");
        Debug.Log($"[DrawBbox NavigationUI] Valor recibido: {isActive}");
        Debug.Log($"[DrawBbox NavigationUI] Estado anterior: {showDrawBbox}");

        showDrawBbox = isActive;

        // Actualizar la visibilidad de las líneas en las cajas existentes
        UpdateLineVisibilityInActiveBBoxes();

        // Si ambos están desactivados, limpiar todas las cajas
        if (!showDrawBbox && !showObjectInformation)
        {
            Debug.Log("[DrawBbox NavigationUI] Ambos desactivados - limpiando todas las cajas");
            ClearAllBBoxes();
        }

        Debug.Log($"[DrawBbox NavigationUI] Estado después: {showDrawBbox}");
        Debug.Log($"[DrawBbox NavigationUI] ===== FIN DRAWBBOX TOGGLE =====");
    }

    void OnShowObjectInformationActiveChanged(bool isActive)
    {
        Debug.Log($"[DrawBbox NavigationUI] ===== OBJECT INFO TOGGLE CHANGED =====");
        Debug.Log($"[DrawBbox NavigationUI] Valor recibido: {isActive}");
        Debug.Log($"[DrawBbox NavigationUI] Estado anterior: {showObjectInformation}");

        showObjectInformation = isActive;

        // Actualizar la visibilidad del texto en las cajas existentes
        UpdateTextVisibilityInActiveBBoxes();

        // Si ambos están desactivados, limpiar todas las cajas
        if (!showDrawBbox && !showObjectInformation)
        {
            Debug.Log("[DrawBbox NavigationUI] Ambos desactivados - limpiando todas las cajas");
            ClearAllBBoxes();
        }

        Debug.Log($"[DrawBbox NavigationUI] Estado después: {showObjectInformation}");
        Debug.Log($"[DrawBbox NavigationUI] ===== FIN OBJECT INFO TOGGLE =====");
    }

    void ProcessDetectionData(ObjectDetection data)
    {
        // CAMBIO: Solo limpiar si AL MENOS UNO de los dos está activo
        // Si ambos están desactivados, no procesamos nada
        if (!showDrawBbox && !showObjectInformation)
        {
            if (activeBBoxes.Count > 0)
            {
                Debug.Log($"[DrawBbox NavigationUI] Ambos desactivados, limpiando {activeBBoxes.Count} cajas");
                ClearAllBBoxes();
            }
            return; // SALIR AQUÍ
        }

        // Limpiar cajas anteriores solo si vamos a dibujar algo nuevo
        ClearAllBBoxes();

        if (data?.objects == null || data.objects.Count == 0)
        {
            return;
        }

        foreach (Objects obj in data.objects)
        {
            if (obj.bbox != null && obj.bbox.Length == 4)
            {
                DrawBoundingBox(obj);
            }
        }
    }

    void DrawBoundingBox(Objects objData)
    {
        // CAMBIO: Solo crear si AL MENOS UNO está activo
        if (!showDrawBbox && !showObjectInformation)
        {
            Debug.LogWarning("[DrawBbox NavigationUI] DrawBoundingBox llamado pero ambos están desactivados!");
            return;
        }

        if (bboxPrefab == null || arCamera == null) return;

        Camera cam = arCamera.GetComponent<Camera>();
        if (cam == null) return;

        float clampedDistance = Mathf.Clamp(objData.distance, minProjectionDistance, maxProjectionDistance);

        float xMin = objData.bbox[0];
        float yMin = objData.bbox[1];
        float xMax = objData.bbox[2];
        float yMax = objData.bbox[3];

        Vector2 minViewport = new Vector2(
            xMin / Screen.width,
            1f - (yMax / Screen.height)
        );

        Vector2 maxViewport = new Vector2(
            xMax / Screen.width,
            1f - (yMin / Screen.height)
        );

        Vector3[] corners = new Vector3[5];
        corners[0] = cam.ViewportToWorldPoint(new Vector3(minViewport.x, minViewport.y, clampedDistance));
        corners[1] = cam.ViewportToWorldPoint(new Vector3(maxViewport.x, minViewport.y, clampedDistance));
        corners[2] = cam.ViewportToWorldPoint(new Vector3(maxViewport.x, maxViewport.y, clampedDistance));
        corners[3] = cam.ViewportToWorldPoint(new Vector3(minViewport.x, maxViewport.y, clampedDistance));
        corners[4] = corners[0];

        Vector3 centerPosition = (corners[0] + corners[2]) * 0.5f;
        GameObject bbox = Instantiate(bboxPrefab, centerPosition, Quaternion.identity);

        if (bbox != null)
        {
            // CAMBIO: Configurar líneas solo si showDrawBbox está activo
            if (showDrawBbox)
            {
                ConfigureLineRenderer(bbox, corners, objData);
            }
            else
            {
                // Si no queremos líneas, desactivar el LineRenderer
                LineRenderer line = bbox.GetComponent<LineRenderer>();
                if (line != null)
                {
                    line.enabled = false;
                }
            }

            // CAMBIO: Configurar texto solo si showObjectInformation está activo
            if (showObjectInformation)
            {
                ConfigureText(bbox, corners, objData, cam);
            }
            else
            {
                // Si no queremos texto, desactivar el TextMeshPro
                TextMeshPro textInfo = bbox.GetComponentInChildren<TextMeshPro>();
                if (textInfo != null)
                {
                    textInfo.gameObject.SetActive(false);
                }
            }

            activeBBoxes.Add(bbox);
        }
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

        float lineWidth = CalculateLineWidth(objData.distance);
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;

        line.useWorldSpace = true;
        line.numCornerVertices = 4;
        line.numCapVertices = 4;
    }

    void ConfigureText(GameObject bbox, Vector3[] corners, Objects objData, Camera cam)
    {
        TextMeshPro textInfo = bbox.GetComponentInChildren<TextMeshPro>();
        if (textInfo == null) return;

        // Configurar el contenido del texto
        textInfo.text = $"{objData.name} ({objData.confidence:F2})\n" +
                       $"Speed: {objData.speed:F1}m/s\n" +
                       $"Distance: {objData.distance:F1}m";

        // Posicionar el texto arriba del bounding box con mejor offset
        Vector3 topLeft = corners[3] + new Vector3(0.11f, -0.1f, 0f);
        Vector3 cameraDirection = (cam.transform.position - topLeft).normalized;
        Vector3 textPosition = topLeft + cameraDirection * textDistanceOffset;

        textInfo.transform.position = textPosition;

        // Orientar el texto hacia la cámara
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
        //textInfo.enableWordWrapping = false;
        textInfo.textWrappingMode = TextWrappingModes.NoWrap;
        textInfo.alignment = TextAlignmentOptions.Center;
        textInfo.fontStyle = FontStyles.Bold;
        textInfo.outlineWidth = 0.1f;
        textInfo.outlineColor = Color.black;

        // CRÍTICO: Aplicar la visibilidad según showObjectInformation
        textInfo.gameObject.SetActive(showObjectInformation);

        Debug.Log($"[DrawBbox NavigationUI] Texto configurado para {objData.name} - Visible: {showObjectInformation}");
    }

    float CalculateLineWidth(float distance)
    {
        if (distance <= 1f)
        {
            float factor = distance / 1f;
            return Mathf.Lerp(minLineWidth, baseLineWidth * 0.7f, factor);
        }
        else if (distance <= 5f)
        {
            float factor = (distance - 1f) / 4f;
            return Mathf.Lerp(baseLineWidth * 0.7f, baseLineWidth, factor);
        }
        else
        {
            float factor = Mathf.Clamp01((distance - 5f) / 15f);
            return Mathf.Lerp(baseLineWidth, maxLineWidth, factor);
        }
    }

    float CalculateTextScale(float distance)
    {
        if (distance <= 1f)
        {
            return minTextSize * (0.8f + 0.2f * distance);
        }
        else if (distance <= 3f)
        {
            float factor = (distance - 1f) / 2f;
            return Mathf.Lerp(minTextSize, baseTextSize, factor);
        }
        else if (distance <= 10f)
        {
            float factor = (distance - 3f) / 7f;
            return Mathf.Lerp(baseTextSize, baseTextSize * 1.2f, factor);
        }
        else
        {
            float factor = Mathf.Clamp01((distance - 10f) / 10f);
            return Mathf.Lerp(baseTextSize * 1.2f, maxTextSize, factor);
        }
    }

    Color GetClassColor(string className)
    {
        switch (className?.ToLower())
        {
            case "person": return new Color(1f, 1f, 0f, 0.9f);
            case "car": return new Color(1f, 0f, 0f, 0.9f);
            case "bicycle": return new Color(0f, 1f, 0f, 0.9f);
            case "motorcycle": return new Color(1f, 0.5f, 0f, 0.9f);
            case "bus": return new Color(0f, 0f, 1f, 0.9f);
            case "truck": return new Color(0.5f, 0f, 0.5f, 0.9f);
            case "dog": return new Color(0f, 1f, 1f, 0.9f);
            case "cat": return new Color(1f, 0f, 1f, 0.9f);
            case "bird": return new Color(1f, 1f, 0.5f, 0.9f);
            case "traffic light": return new Color(0.5f, 1f, 0.5f, 0.9f);
            case "stop sign": return new Color(0.8f, 0.2f, 0.2f, 0.9f);
            default: return new Color(1f, 1f, 1f, 0.9f);
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

    void UpdateTextVisibilityInActiveBBoxes()
    {
        Debug.Log($"[DrawBbox NavigationUI] Actualizando visibilidad del texto en {activeBBoxes.Count} cajas - showObjectInformation: {showObjectInformation}");

        foreach (GameObject bbox in activeBBoxes)
        {
            if (bbox != null)
            {
                TextMeshPro textInfo = bbox.GetComponentInChildren<TextMeshPro>();
                if (textInfo != null)
                {
                    textInfo.gameObject.SetActive(showObjectInformation);
                }
            }
        }
    }

    void UpdateLineVisibilityInActiveBBoxes()
    {
        Debug.Log($"[DrawBbox NavigationUI] Actualizando visibilidad de líneas en {activeBBoxes.Count} cajas - showDrawBbox: {showDrawBbox}");

        foreach (GameObject bbox in activeBBoxes)
        {
            if (bbox != null)
            {
                LineRenderer line = bbox.GetComponent<LineRenderer>();
                if (line != null)
                {
                    line.enabled = showDrawBbox;
                }
            }
        }
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
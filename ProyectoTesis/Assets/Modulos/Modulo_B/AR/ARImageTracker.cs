using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;
using UnityEngine.UI;

public class ARImageTracker : MonoBehaviour
{
    [Header("AR Components")]
    public ARTrackedImageManager trackedImageManager;

    [Header("Content Configuration")]
    public GameObject contentPrefab; // Prefab que contendrá el texto e imagen

    [Header("Content Data")]
    public ImageContent[] imageContents; // Array con el contenido para cada imagen

    [Header("Position Settings")]
    [Range(0f, 2f)]
    public float heightOffset = 0.3f; // Altura sobre la imagen detectada

    // Diccionario para mantener track de los objetos instanciados
    private Dictionary<TrackableId, GameObject> instantiatedObjects = new Dictionary<TrackableId, GameObject>();

    [System.Serializable]
    public class ImageContent
    {
        public string imageName; // Debe coincidir con el nombre en Reference Library
        public string displayText;
        public Sprite displayImage;
        public Color textColor = Color.white;
    }

    void Start()
    {
        // Suscribirse al evento de cambios en imágenes rastreadas
        if (trackedImageManager != null)
        {
            trackedImageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);
        }
        else
        {
            Debug.LogError("TrackedImageManager no está asignado!");
        }
    }

    void OnDestroy()
    {
        // Desuscribirse del evento para evitar memory leaks
        if (trackedImageManager != null)
        {
            trackedImageManager.trackablesChanged.RemoveListener(OnTrackedImagesChanged);
        }
    }

    void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        // Manejar nuevas imágenes detectadas
        foreach (var trackedImage in eventArgs.added)
        {
            CreateTrackedImage(trackedImage);
        }

        // Manejar imágenes actualizadas
        foreach (var trackedImage in eventArgs.updated)
        {
            UpdateTrackedImage(trackedImage);
        }

        // Manejar imágenes removidas
        foreach (var trackedImage in eventArgs.removed)
        {
            RemoveTrackedImage(trackedImage.Key);
        }
    }

    void CreateTrackedImage(ARTrackedImage trackedImage)
    {
        // Buscar el contenido correspondiente a esta imagen
        ImageContent content = GetContentForImage(trackedImage.referenceImage.name);

        if (content == null)
        {
            Debug.LogWarning($"No se encontró contenido para la imagen: {trackedImage.referenceImage.name}");
            return;
        }

        // Instanciar el prefab de contenido
        GameObject contentObject = Instantiate(contentPrefab);

        // Configurar la posición y rotación
        SetupContentTransform(contentObject, trackedImage);

        // Configurar el contenido (texto e imagen)
        SetupContentData(contentObject, content);

        // Guardar referencia en el diccionario
        instantiatedObjects[trackedImage.trackableId] = contentObject;

        Debug.Log($"Imagen detectada: {trackedImage.referenceImage.name}");
    }

    void UpdateTrackedImage(ARTrackedImage trackedImage)
    {
        if (instantiatedObjects.TryGetValue(trackedImage.trackableId, out GameObject contentObject))
        {
            // Actualizar posición y rotación
            SetupContentTransform(contentObject, trackedImage);

            // Mostrar/ocultar según el estado de tracking
            contentObject.SetActive(trackedImage.trackingState == TrackingState.Tracking);
        }
    }

    void RemoveTrackedImage(TrackableId trackableId)
    {
        if (instantiatedObjects.TryGetValue(trackableId, out GameObject contentObject))
        {
            Destroy(contentObject);
            instantiatedObjects.Remove(trackableId);
            Debug.Log("Imagen removida del tracking");
        }
    }

    void SetupContentTransform(GameObject contentObject, ARTrackedImage trackedImage)
    {
        // Posicionar el objeto sobre la imagen detectada
        Vector3 position = trackedImage.transform.position;
        position.y += heightOffset; // Aplicar offset de altura

        contentObject.transform.position = position;
        contentObject.transform.rotation = trackedImage.transform.rotation;

        // Hacer que el objeto mire hacia la cámara (billboard effect)
        Vector3 lookDirection = Camera.main.transform.position - contentObject.transform.position;
        lookDirection.y = 0; // Mantener solo rotación horizontal
        if (lookDirection != Vector3.zero)
        {
            contentObject.transform.rotation = Quaternion.LookRotation(-lookDirection);
        }
    }

    void SetupContentData(GameObject contentObject, ImageContent content)
    {
        // Buscar y configurar el componente de texto
        TextMeshProUGUI textComponent = contentObject.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent != null)
        {
            textComponent.text = content.displayText;
            textComponent.color = content.textColor;
        }
        else
        {
            Debug.LogWarning("No se encontró componente TextMeshProUGUI en el prefab");
        }

        // Buscar y configurar el componente de imagen
        Image imageComponent = contentObject.GetComponentInChildren<Image>();
        if (imageComponent != null && content.displayImage != null)
        {
            imageComponent.sprite = content.displayImage;
        }
        else if (imageComponent == null)
        {
            Debug.LogWarning("No se encontró componente Image en el prefab");
        }
    }

    ImageContent GetContentForImage(string imageName)
    {
        foreach (var content in imageContents)
        {
            if (content.imageName == imageName)
            {
                return content;
            }
        }
        return null;
    }

    // Método público para cambiar la altura desde el inspector o código
    public void SetHeightOffset(float newHeight)
    {
        heightOffset = newHeight;

        // Actualizar todos los objetos existentes
        foreach (var kvp in instantiatedObjects)
        {
            var trackedImage = trackedImageManager.trackables[kvp.Key];
            if (trackedImage != null)
            {
                SetupContentTransform(kvp.Value, trackedImage);
            }
        }
    }
}
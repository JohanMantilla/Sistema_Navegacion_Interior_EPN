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
    public GameObject contentPrefab; // Prefab que contendr� el texto e imagen

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
        // Suscribirse al evento de cambios en im�genes rastreadas
        if (trackedImageManager != null)
        {
            trackedImageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);
        }
        else
        {
            Debug.LogError("TrackedImageManager no est� asignado!");
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
        // Manejar nuevas im�genes detectadas
        foreach (var trackedImage in eventArgs.added)
        {
            CreateTrackedImage(trackedImage);
        }

        // Manejar im�genes actualizadas
        foreach (var trackedImage in eventArgs.updated)
        {
            UpdateTrackedImage(trackedImage);
        }

        // Manejar im�genes removidas
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
            Debug.LogWarning($"No se encontr� contenido para la imagen: {trackedImage.referenceImage.name}");
            return;
        }

        // Instanciar el prefab de contenido
        GameObject contentObject = Instantiate(contentPrefab);

        // Configurar la posici�n y rotaci�n
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
            // Actualizar posici�n y rotaci�n
            SetupContentTransform(contentObject, trackedImage);

            // Mostrar/ocultar seg�n el estado de tracking
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

        // Hacer que el objeto mire hacia la c�mara (billboard effect)
        Vector3 lookDirection = Camera.main.transform.position - contentObject.transform.position;
        lookDirection.y = 0; // Mantener solo rotaci�n horizontal
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
            Debug.LogWarning("No se encontr� componente TextMeshProUGUI en el prefab");
        }

        // Buscar y configurar el componente de imagen
        Image imageComponent = contentObject.GetComponentInChildren<Image>();
        if (imageComponent != null && content.displayImage != null)
        {
            imageComponent.sprite = content.displayImage;
        }
        else if (imageComponent == null)
        {
            Debug.LogWarning("No se encontr� componente Image en el prefab");
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

    // M�todo p�blico para cambiar la altura desde el inspector o c�digo
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
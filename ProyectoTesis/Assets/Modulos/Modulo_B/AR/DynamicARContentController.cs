using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;
using System.Collections.Generic;

public class DynamicARContentController : MonoBehaviour
{
    [Header("AR Components")]
    public ARTrackedImageManager trackedImageManager;

    [Header("UI Components")]
    public Image targetImage;
    public TextMeshProUGUI targetText;

    [Header("Content for Image 'p'")]
    public Sprite imageForP;
    public string textForP = "Contenido para imagen P";

    [Header("Content for Image 'p1'")]
    public Sprite imageForP1;
    public string textForP1 = "Contenido para imagen P1";

    // Dictionary para mapear nombres de imágenes con su contenido
    private Dictionary<string, ContentData> contentMapping;

    [System.Serializable]
    public struct ContentData
    {
        public Sprite sprite;
        public string text;
    }

    void Start()
    {
        // Inicializar el diccionario de contenido
        InitializeContentMapping();

        // Verificar que todos los componentes estén asignados
        ValidateComponents();

        // Suscribirse al evento de imágenes detectadas
        if (trackedImageManager != null)
        {
            trackedImageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);
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

    void InitializeContentMapping()
    {
        contentMapping = new Dictionary<string, ContentData>
        {
            ["p"] = new ContentData { sprite = imageForP, text = textForP },
            ["p1"] = new ContentData { sprite = imageForP1, text = textForP1 }
        };
    }

    void ValidateComponents()
    {
        if (trackedImageManager == null)
        {
            Debug.LogError("ARTrackedImageManager no está asignado en " + gameObject.name);
        }

        if (targetImage == null)
        {
            Debug.LogError("Target Image no está asignado en " + gameObject.name);
        }

        if (targetText == null)
        {
            Debug.LogError("Target Text no está asignado en " + gameObject.name);
        }
    }

    void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        // Procesar imágenes recién detectadas
        foreach (var trackedImage in eventArgs.added)
        {
            UpdateContent(trackedImage);
        }

        // Procesar imágenes actualizadas
        foreach (var trackedImage in eventArgs.updated)
        {
            if (trackedImage.trackingState == TrackingState.Tracking)
            {
                UpdateContent(trackedImage);
            }
        }
    }

    void UpdateContent(ARTrackedImage trackedImage)
    {
        // Obtener el nombre de la imagen detectada
        string imageName = trackedImage.referenceImage.name;

        // Verificar si tenemos contenido para esta imagen
        if (contentMapping.ContainsKey(imageName))
        {
            ContentData content = contentMapping[imageName];

            // Actualizar la imagen
            if (targetImage != null && content.sprite != null)
            {
                targetImage.sprite = content.sprite;
                targetImage.gameObject.SetActive(true);
                Debug.Log($"Imagen actualizada para: {imageName}");
            }

            // Actualizar el texto
            if (targetText != null)
            {
                targetText.text = content.text;
                targetText.gameObject.SetActive(true);
                Debug.Log($"Texto actualizado para: {imageName}");
            }

            Vector3 offsetPosition = trackedImage.transform.position;
            transform.position = offsetPosition;
            transform.rotation = trackedImage.transform.rotation;

            // Asegurar que el canvas esté activo
            gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"No se encontró contenido para la imagen: {imageName}");
        }
    }

    // Método público para cambiar contenido manualmente (opcional)
    public void SetContentForImage(string imageName, Sprite newSprite, string newText)
    {
        if (contentMapping.ContainsKey(imageName))
        {
            contentMapping[imageName] = new ContentData { sprite = newSprite, text = newText };
            Debug.Log($"Contenido actualizado para imagen: {imageName}");
        }
        else
        {
            Debug.LogWarning($"Imagen no encontrada en el mapping: {imageName}");
        }
    }

    // Método para ocultar el contenido cuando no se detecta ninguna imagen
    public void HideContent()
    {
        if (targetImage != null)
            targetImage.gameObject.SetActive(false);

        if (targetText != null)
            targetText.gameObject.SetActive(false);

        gameObject.SetActive(false);
    }
}
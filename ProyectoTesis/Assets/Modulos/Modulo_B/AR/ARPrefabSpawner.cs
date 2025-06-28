using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

public class ARPrefabSpawner : MonoBehaviour
{
    [Header("AR Components")]
    public ARTrackedImageManager trackedImageManager;

    [Header("Prefab to Spawn")]
    public GameObject dynamicContentPrefab;

    // Dictionary para rastrear los prefabs instanciados por cada imagen
    private Dictionary<string, GameObject> spawnedPrefabs = new Dictionary<string, GameObject>();

    void Start()
    {
        if (trackedImageManager == null)
        {
            trackedImageManager = FindObjectOfType<ARTrackedImageManager>();
        }

        if (trackedImageManager != null)
        {
            trackedImageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);
        }
        else
        {
            Debug.LogError("ARTrackedImageManager no encontrado!");
        }
    }

    void OnDestroy()
    {
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
            SpawnPrefabForImage(trackedImage);
        }

        // Manejar imágenes actualizadas
        foreach (var trackedImage in eventArgs.updated)
        {
            UpdatePrefabForImage(trackedImage);
        }

        // Manejar imágenes removidas
        foreach (var trackedImage in eventArgs.removed)
        {
            RemovePrefabForImage(trackedImage.Key);
        }
    }

    void SpawnPrefabForImage(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;

        // Solo procesar imágenes "p" y "p1"
        if (imageName != "p" && imageName != "p1")
        {
            return;
        }

        // Verificar si ya existe un prefab para esta imagen
        if (spawnedPrefabs.ContainsKey(imageName))
        {
            return;
        }

        if (dynamicContentPrefab != null)
        {
            // Instanciar el prefab
            GameObject spawnedPrefab = Instantiate(dynamicContentPrefab, trackedImage.transform);

            // Configurar el controlador de contenido dinámico
            DynamicARContentController contentController = spawnedPrefab.GetComponent<DynamicARContentController>();
            if (contentController != null)
            {
                contentController.trackedImageManager = trackedImageManager;
            }

            // Agregar al diccionario
            spawnedPrefabs[imageName] = spawnedPrefab;

            Debug.Log($"Prefab spawneado para imagen: {imageName}");
        }
    }

    void UpdatePrefabForImage(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;

        if (spawnedPrefabs.ContainsKey(imageName))
        {
            GameObject prefab = spawnedPrefabs[imageName];

            if (trackedImage.trackingState == TrackingState.Tracking)
            {
                // Mostrar y actualizar posición del prefab
                prefab.SetActive(true);
                Vector3 offsetPosition = trackedImage.transform.position;
                prefab.transform.position = offsetPosition;
                prefab.transform.rotation = trackedImage.transform.rotation;
            }
            else
            {
                // Ocultar prefab si no se está rastreando
                prefab.SetActive(false);
            }
        }
    }

    void RemovePrefabForImage(TrackableId trackableId)
    {

        foreach (var trackedImage in trackedImageManager.trackables) {
            if (trackedImage.trackableId == trackableId) {
                if (trackedImage.referenceImage != null) {
                    string nameImage = trackedImage.referenceImage.name;
                    if (spawnedPrefabs.ContainsKey(nameImage))
                    {
                        GameObject prefab = spawnedPrefabs[nameImage];
                        if (prefab != null)
                        {
                            Destroy(prefab);
                        }
                        spawnedPrefabs.Remove(nameImage);
                    }
                }
                break;
            }
        }
    }


}
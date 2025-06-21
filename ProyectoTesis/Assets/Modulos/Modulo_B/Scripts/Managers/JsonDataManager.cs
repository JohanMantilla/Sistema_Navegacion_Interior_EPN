using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using System.IO;
using System.Collections;
using UnityEngine.UIElements;
using UnityEngine.Networking;
public class JsonDataManager : MonoBehaviour
{
    private string routePath;
    private string lastJsonRoute;
    public static event Action<Route> OnJsonRouteUpdated;
    private string objectDetectionPath;
    private string lastJsonObjectDetection;
    public static event Action<ObjectDetection>OnChangeObjectionDetection;
    void Start()
    {
        StartCoroutine(CheckJsonRouteChanges());
        StartCoroutine(CheckJsonObjectDetection());
    }
    void Update()
    {
    }
    IEnumerator CheckJsonRouteChanges()
    {
        while (true)
        {
            yield return StartCoroutine(LoadRouteData());
            yield return new WaitForSecondsRealtime(3f);
        }
    }
    IEnumerator LoadRouteData()
    {
        routePath = Path.Combine(Application.streamingAssetsPath,"route.json");
        string fileRoutePath = routePath;
        if (!fileRoutePath.Contains("://"))
        {
            Debug.Log("Error, no existe el archivo route.json");
            fileRoutePath = "file://" + fileRoutePath;
        }
        using (UnityWebRequest request =
       UnityWebRequest.Get(fileRoutePath))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                string newJsonData = request.downloadHandler.text;
                if (newJsonData != null && newJsonData != lastJsonRoute)
                {
                    lastJsonRoute = newJsonData;
                    DeserializeJsonRoute(newJsonData);
                }
            }
            else
            {
                Debug.Log("Error");
            }
        }
    }
    void DeserializeJsonRoute(string routeJsonSerialized)
    {
        Route routeJson = JsonConvert.DeserializeObject<Route>(routeJsonSerialized);
        OnJsonRouteUpdated?.Invoke(routeJson);
    }
    IEnumerator CheckJsonObjectDetection()
    {
        while (true)
        {
            yield return StartCoroutine(LoadObjectDetection());
            yield return new WaitForSecondsRealtime(1f);
        }
    }
    IEnumerator LoadObjectDetection()
    {
        objectDetectionPath = Path.Combine(Application.streamingAssetsPath, "objects.json");
        string filePath = objectDetectionPath;
        // En Android, necesitamos el prefijo jar:file://
        if (!filePath.Contains("://"))
        {
            filePath = "file://" + filePath;
        }
        using (UnityWebRequest request = UnityWebRequest.Get(filePath))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                string newJsonData = request.downloadHandler.text;
                if (newJsonData != null && newJsonData != lastJsonObjectDetection)
                {
                    lastJsonObjectDetection = newJsonData;
                    DeserializeJsonObjectDetection(newJsonData);
                }
            }
            else
            {
                Debug.Log("Error al cargar objects.json: " + request.error);
            }
        }
    }
    void DeserializeJsonObjectDetection(string objectDetectionSerialized)
    {
        ObjectDetection objectDetection = JsonConvert.DeserializeObject<ObjectDetection>(objectDetectionSerialized);
        OnChangeObjectionDetection?.Invoke(objectDetection);
    }
}
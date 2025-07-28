using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class JsonDataManager : MonoBehaviour
{
    private string routePath;
    private string lastJsonRoute;
    public static event Action<RouteCollection> OnJsonRouteUpdated;
    public static event Action<List<AlertPoint>> OnAlertPointsUpdated; // Nuevo evento para puntos de alerta

    void Start()
    {
        StartCoroutine(CheckJsonRouteChanges());
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
        routePath = Path.Combine(Application.streamingAssetsPath, "route.json");
        string fileRoutePath = routePath;

        if (!fileRoutePath.Contains("://"))
        {
            fileRoutePath = "file://" + fileRoutePath;
        }

        using (UnityWebRequest request = UnityWebRequest.Get(fileRoutePath))
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
                Debug.LogError("Error loading route.json: " + request.error);
            }
        }
    }

    void DeserializeJsonRoute(string routeJsonSerialized)
    {
        try
        {
            RouteCollection routeJson = JsonConvert.DeserializeObject<RouteCollection>(routeJsonSerialized);
            OnJsonRouteUpdated?.Invoke(routeJson);

            // Extraer puntos de alerta
            List<AlertPoint> alertPoints = ExtractAlertPoints(routeJson);
            OnAlertPointsUpdated?.Invoke(alertPoints);
        }
        catch (Exception e)
        {
            Debug.LogError("Error deserializing route JSON: " + e.Message);
        }
    }

    private List<AlertPoint> ExtractAlertPoints(RouteCollection route)
    {
        List<AlertPoint> alertPoints = new List<AlertPoint>();

        foreach (var feature in route.features)
        {
            // Solo procesar Features de tipo "Point"
            if (feature.geometry.type == "Point")
            {
                var coords = feature.geometry.GetPointCoordinates();
                if (coords != null && coords.Count >= 2)
                {
                    // En GeoJSON las coordenadas están como [longitude, latitude]
                    double longitude = coords[0];
                    double latitude = coords[1];

                    AlertPoint alertPoint = new AlertPoint(latitude, longitude, feature.properties);
                    alertPoints.Add(alertPoint);

                    Debug.Log($"Alert point found: Lat={latitude}, Lon={longitude}, Type={feature.properties.type}");
                }
                else
                {
                    Debug.LogWarning("Invalid point coordinates in feature");
                }
            }
        }

        return alertPoints;
    }

}
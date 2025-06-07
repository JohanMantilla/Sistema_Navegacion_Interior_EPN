using System;
using System.Collections.Generic;
using Mono.Cecil;
using UnityEngine;

public class JsonDataManager : MonoBehaviour
{
    //public static event Action<Route> onLoadRouteData;
    public static event Action<string> onFail;

    //Tiempo de verificación 
    //public float verificationTime = 3f;

    //public Route route = new Route();
   
    //private DateTime lastUpdateTime;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
        LoadRouteData();
        //LoadObjects();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void LoadRouteData() {

        TextAsset archivoRoute = Resources.Load<TextAsset>("Data/route");

        if (archivoRoute == null)
        {
            Debug.LogError("No se encontró el archivo JSON");
            return;
        }

        Route routeJson = JsonUtility.FromJson<Route>(archivoRoute.text);
        Debug.Log("Distancia: " + routeJson.route.total_distance_meters);


    }

}

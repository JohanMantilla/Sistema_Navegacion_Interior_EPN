using System.Collections.Generic;
using UnityEngine;

public class SimpleNavigationAudio : MonoBehaviour
{
    [Header("Configuración Básica")]
    public float maxDistanceFromRoute = 20f; // Metros para considerar fuera de ruta
    public float instructionCooldown = 15f; // Segundos entre instrucciones

    private List<Vector2> routePoints = new List<Vector2>();
    private Vector2 currentUserPosition;
    private float lastInstructionTime = 0f;
    private bool isNavigationActive = false;
    private bool hasRoute = false;

    void Start()
    {
        // Suscribirse a eventos GPS y de ruta
        SimpleGPSManager.OnLocationUpdate += OnLocationUpdated;
        JsonDataManager.OnJsonRouteUpdated += OnRouteUpdated;
    }

    void OnDestroy()
    {
        SimpleGPSManager.OnLocationUpdate -= OnLocationUpdated;
        JsonDataManager.OnJsonRouteUpdated -= OnRouteUpdated;
    }

    void OnLocationUpdated(float longitude, float latitude)
    {
        currentUserPosition = new Vector2(latitude, longitude);

        if (isNavigationActive && hasRoute && Time.time - lastInstructionTime >= instructionCooldown)
        {
            CheckNavigation();
        }
    }

    void OnRouteUpdated(RouteCollection routeData)
    {
        if (routeData == null || routeData.features == null) return;

        routePoints.Clear();

        foreach (var feature in routeData.features)
        {
            if (feature.geometry.type == "LineString")
            {
                var coordinates = feature.geometry.GetLineStringCoordinates();
                if (coordinates != null)
                {
                    foreach (var coord in coordinates)
                    {
                        if (coord.Count >= 2)
                        {
                            double longitude = coord[0];
                            double latitude = coord[1];
                            routePoints.Add(new Vector2((float)latitude, (float)longitude));
                        }
                    }
                }
            }
        }

        hasRoute = routePoints.Count > 0;
    }

    void CheckNavigation()
    {
        if (routePoints.Count < 2) return;

        // Encontrar punto más cercano en la ruta
        float minDistance = float.MaxValue;
        int closestIndex = 0;

        for (int i = 0; i < routePoints.Count; i++)
        {
            float distance = CalculateDistance(currentUserPosition, routePoints[i]);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestIndex = i;
            }
        }

        // Verificar si está fuera de ruta
        if (minDistance > maxDistanceFromRoute)
        {
            SpeakInstruction("Te has salido de la ruta, regresa al camino");
            lastInstructionTime = Time.time;
            return;
        }

        // Instrucciones básicas de navegación
        float distanceToEnd = CalculateDistance(currentUserPosition, routePoints[routePoints.Count - 1]);

        if (distanceToEnd < 15f)
        {
            SpeakInstruction("Has llegado a tu destino");
            lastInstructionTime = Time.time;
        }
        else if (distanceToEnd < 50f)
        {
            SpeakInstruction("Te acercas al destino");
            lastInstructionTime = Time.time;
        }
        else
        {
            // Dar instrucción de seguir ruta cada cierto tiempo
            SpeakInstruction("Continúa por la ruta");
            lastInstructionTime = Time.time;
        }
    }

    void SpeakInstruction(string instruction)
    {
        Debug.Log($"🔊 Instrucción: {instruction}");

        if (AndroidTTSManager.Instance != null && AndroidTTSManager.Instance.isInitialize)
        {
            AndroidTTSManager.Instance.Speak(instruction);
        }
    }

    float CalculateDistance(Vector2 pos1, Vector2 pos2)
    {
        float lat1 = pos1.x * Mathf.Deg2Rad;
        float lon1 = pos1.y * Mathf.Deg2Rad;
        float lat2 = pos2.x * Mathf.Deg2Rad;
        float lon2 = pos2.y * Mathf.Deg2Rad;

        float dlat = lat2 - lat1;
        float dlon = lon2 - lon1;

        float a = Mathf.Sin(dlat / 2) * Mathf.Sin(dlat / 2) +
                  Mathf.Cos(lat1) * Mathf.Cos(lat2) *
                  Mathf.Sin(dlon / 2) * Mathf.Sin(dlon / 2);

        float c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));

        return 6371000 * c; // Radio de la Tierra en metros
    }

    // MÉTODOS PÚBLICOS BÁSICOS
    public void StartNavigation()
    {
        isNavigationActive = true;
        //lastInstructionTime = 0f;
    }

    public void StopNavigation()
    {
        isNavigationActive = false;
    }

    public bool IsNavigationActive()
    {
        return isNavigationActive;
    }
}
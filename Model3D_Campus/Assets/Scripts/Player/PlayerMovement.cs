using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.IO;

public class PlayerMovement : MonoBehaviour
{
    [Header("Navigation")]
    public Transform target;

    [Header("Geo Reference")]
    public double referenceLatitude = 0.0;
    public double referenceLongitude = 0.0;
    public float metersPerUnityUnit = 1.0f;

    [Header("Logging")]
    public bool exportGeoJSON = true;
    public float logInterval = 1.0f;

    private NavMeshAgent agent;
    private string logFilePath;
    private string geoJsonFilePath;
    private float timer = 0f;
    private bool hasArrived = false;
    private List<WaypointData> collectedWaypoints = new List<WaypointData>();
    private Vector3 startPosition;
    private float totalTraveledDistance = 0f;
    private Vector3 lastPosition;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        startPosition = transform.position;
        lastPosition = transform.position;

        agent.SetDestination(target.position);

        string logDirectory = Application.dataPath + "/Logs/";
        Directory.CreateDirectory(logDirectory);

        logFilePath = logDirectory + "PlayerLog.txt";
        geoJsonFilePath = logDirectory + "RouteGeoJSON.json";

        File.WriteAllText(logFilePath, "=== Log de Movimiento del Jugador ===\n");
        File.AppendAllText(logFilePath, "Destino inicial: " + target.position + "\n");

        collectedWaypoints = WaypointDetector.DetectWaypoints(agent.path.corners, metersPerUnityUnit, referenceLatitude, referenceLongitude);
    }

    void Update()
    {
        float frameDistance = Vector3.Distance(transform.position, lastPosition);
        totalTraveledDistance += frameDistance;
        lastPosition = transform.position;

        timer += Time.deltaTime;
        if (timer >= logInterval)
        {
            timer = 0f;
            GeoLogger.LogPlayerPosition(logFilePath, transform.position, agent, totalTraveledDistance);
        }

        if (!hasArrived && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
            {
                File.AppendAllText(logFilePath, $"[{Time.time:F2}s] Llegó al destino. Distancia total: {totalTraveledDistance:F2}m\n");
                hasArrived = true;

                if (exportGeoJSON)
                {
                    GeoRouteData routeData = new GeoRouteData
                    {
                        startPoint = WaypointDetector.DetectWaypoints(new Vector3[] { startPosition }, metersPerUnityUnit, referenceLatitude, referenceLongitude)[0].geoPosition,
                        endPoint = WaypointDetector.DetectWaypoints(new Vector3[] { target.position }, metersPerUnityUnit, referenceLatitude, referenceLongitude)[0].geoPosition,
                        totalDistance = totalTraveledDistance,
                        waypoints = collectedWaypoints
                    };
                    GeoLogger.ExportGeoJSON(geoJsonFilePath, routeData);
                }
            }
        }
    }
}

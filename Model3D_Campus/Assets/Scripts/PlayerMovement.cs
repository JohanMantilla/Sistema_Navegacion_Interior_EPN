using UnityEngine;
using UnityEngine.AI;
using System.IO;
using System.Collections.Generic;
using System.Text;

[System.Serializable]
public class WaypointData
{
    public string type; // "surface" o "obstacle"
    public string name_of_surface;
    public float elevation_from_position_standard;
    public int stair_count = 0; // Para gradas
    public Vector3 worldPosition;
    public Vector2 geoPosition; // lat, lon
}

[System.Serializable]
public class GeoRouteData
{
    public Vector2 startPoint; // lat, lon
    public Vector2 endPoint; // lat, lon
    public float totalDistance;
    public List<WaypointData> waypoints;
}

public class PlayerMovement : MonoBehaviour
{
    [Header("Navigation")]
    public Transform target;

    [Header("Geo Reference")]
    public double referenceLatitude = 0.0; // Latitud de referencia del mundo
    public double referenceLongitude = 0.0; // Longitud de referencia del mundo
    public float metersPerUnityUnit = 1.0f; // Conversión metros por unidad de Unity

    [Header("Logging")]
    public bool exportGeoJSON = true;

    private NavMeshAgent agent;
    private string logFilePath;
    private string geoJsonFilePath;
    private float logInterval = 1.0f;
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

        // Configurar archivos de log
        string logDirectory = Application.dataPath + "/Logs/";
        Directory.CreateDirectory(logDirectory);

        logFilePath = logDirectory + "PlayerLog.txt";
        geoJsonFilePath = logDirectory + "RouteGeoJSON.json";

        File.WriteAllText(logFilePath, "=== Log de Movimiento del Jugador ===\n");
        File.AppendAllText(logFilePath, "Destino inicial: " + target.position + "\n");

        // Detectar waypoints en la ruta calculada
        DetectWaypointsInPath();
    }

    void Update()
    {
        // Calcular distancia recorrida
        float frameDistance = Vector3.Distance(transform.position, lastPosition);
        totalTraveledDistance += frameDistance;
        lastPosition = transform.position;

        timer += Time.deltaTime;
        if (timer >= logInterval)
        {
            timer = 0f;
            string log = $"[{Time.time:F2}s] Posición: {transform.position}, Distancia restante: {agent.remainingDistance}, Distancia total: {totalTraveledDistance:F2}m\n";
            File.AppendAllText(logFilePath, log);
        }

        if (!hasArrived && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
            {
                string arrivalLog = $"[{Time.time:F2}s] Llegó al destino. Distancia total recorrida: {totalTraveledDistance:F2}m\n";
                File.AppendAllText(logFilePath, arrivalLog);
                hasArrived = true;

                if (exportGeoJSON)
                {
                    ExportGeoJSON();
                }
            }
        }
    }

    void DetectWaypointsInPath()
    {
        if (agent.path.corners.Length > 0)
        {
            for (int i = 0; i < agent.path.corners.Length; i++)
            {
                Vector3 corner = agent.path.corners[i];
                WaypointData waypoint = AnalyzePositionForWaypoint(corner);
                if (waypoint != null)
                {
                    collectedWaypoints.Add(waypoint);
                }
            }
        }
    }

    WaypointData AnalyzePositionForWaypoint(Vector3 position)
    {
        WaypointData waypoint = new WaypointData();
        waypoint.worldPosition = position;
        waypoint.geoPosition = WorldToGeoCoordinates(position);
        waypoint.elevation_from_position_standard = position.y - 1.6f; // Asumiendo 1.6m como altura estándar

        // Detectar objetos en la posición usando raycast y overlap
        Collider[] nearbyObjects = Physics.OverlapSphere(position, 2.0f);

        foreach (Collider col in nearbyObjects)
        {
            GameObject obj = col.gameObject;

            // Detectar escaleras/gradas
            if (obj.name.ToLower().Contains("escalera") || obj.name.ToLower().Contains("stairs") ||
                obj.name.ToLower().Contains("gradas") || obj.tag == "Stairs")
            {
                waypoint.type = "obstacle";
                waypoint.name_of_surface = obj.name;

                // Intentar obtener el componente de escaleras personalizado
                StairComponent stairComp = obj.GetComponent<StairComponent>();
                if (stairComp != null)
                {
                    waypoint.stair_count = stairComp.stepCount;
                }
                else
                {
                    // Estimar cantidad de escalones basado en la altura del collider o renderer
                    float stairHeight = 0f;

                    // Usar Collider si está disponible
                    Collider stairCollider = obj.GetComponent<Collider>();
                    if (stairCollider != null)
                    {
                        stairHeight = stairCollider.bounds.size.y;
                    }
                    else
                    {
                        Renderer stairRenderer = obj.GetComponent<Renderer>();
                        if (stairRenderer != null)
                        {
                            stairHeight = stairRenderer.bounds.size.y;
                        }
                        else
                        {
                            stairHeight = 1.0f; // Valor por defecto si no se puede determinar
                        }
                    }

                    waypoint.stair_count = Mathf.RoundToInt(stairHeight / 0.2f); // Asume 20cm por escalón
                }
            }

            // Detectar superficies
            if (obj.name.ToLower().Contains("piso") || obj.name.ToLower().Contains("ground") ||
                obj.name.ToLower().Contains("floor") || obj.tag == "Ground")
            {
                waypoint.type = "surface";
                waypoint.name_of_surface = obj.name;
                return waypoint;
            }

            // Detectar obstáculos
            if (obj.name.ToLower().Contains("obstacle") || obj.name.ToLower().Contains("obstaculo") ||
                obj.tag == "Obstacle")
            {
                waypoint.type = "obstacle";
                waypoint.name_of_surface = obj.name;
                return waypoint;
            }
        }

        // Si no se detecta nada específico, marcarlo como superficie genérica
        waypoint.type = "surface";
        waypoint.name_of_surface = "generic_surface";

        return waypoint;
    }

    Bounds GetObjectBounds(GameObject obj)
    {
        Bounds bounds = new Bounds();
        bool hasBounds = false;

        // Intentar obtener bounds del Renderer
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            bounds = renderer.bounds;
            hasBounds = true;
        }
        else
        {
            // Si no hay Renderer, intentar con Collider
            Collider collider = obj.GetComponent<Collider>();
            if (collider != null)
            {
                bounds = collider.bounds;
                hasBounds = true;
            }
        }

        // Si el objeto no tiene bounds directos, buscar en sus hijos
        if (!hasBounds)
        {
            Renderer[] childRenderers = obj.GetComponentsInChildren<Renderer>();
            if (childRenderers.Length > 0)
            {
                bounds = childRenderers[0].bounds;
                for (int i = 1; i < childRenderers.Length; i++)
                {
                    bounds.Encapsulate(childRenderers[i].bounds);
                }
                hasBounds = true;
            }
        }

        return hasBounds ? bounds : new Bounds();
    }

    Vector2 WorldToGeoCoordinates(Vector3 worldPos)
    {
        // Conversión simplificada de coordenadas del mundo a lat/lon
        // Esto asume un sistema de coordenadas plano local
        float deltaX = worldPos.x * metersPerUnityUnit;
        float deltaZ = worldPos.z * metersPerUnityUnit;

        // Aproximación: 1 grado = ~111,320 metros
        double lat = referenceLatitude + (deltaZ / 111320.0);
        double lon = referenceLongitude + (deltaX / (111320.0 * System.Math.Cos(referenceLatitude * System.Math.PI / 180.0)));

        return new Vector2((float)lat, (float)lon);
    }

    void ExportGeoJSON()
    {
        GeoRouteData routeData = new GeoRouteData();
        routeData.startPoint = WorldToGeoCoordinates(startPosition);
        routeData.endPoint = WorldToGeoCoordinates(target.position);
        routeData.totalDistance = totalTraveledDistance;
        routeData.waypoints = collectedWaypoints;

        StringBuilder geoJson = new StringBuilder();
        geoJson.AppendLine("{");
        geoJson.AppendLine("  \"type\": \"FeatureCollection\",");
        geoJson.AppendLine("  \"route_info\": {");
        geoJson.AppendLine($"    \"start_point\": {{\"latitude\": {routeData.startPoint.x}, \"longitude\": {routeData.startPoint.y}}},");
        geoJson.AppendLine($"    \"end_point\": {{\"latitude\": {routeData.endPoint.x}, \"longitude\": {routeData.endPoint.y}}},");
        geoJson.AppendLine($"    \"total_distance_meters\": {routeData.totalDistance:F2}");
        geoJson.AppendLine("  },");
        geoJson.AppendLine("  \"features\": [");

        // Agregar ruta como LineString
        geoJson.AppendLine("    {");
        geoJson.AppendLine("      \"type\": \"Feature\",");
        geoJson.AppendLine("      \"geometry\": {");
        geoJson.AppendLine("        \"type\": \"LineString\",");
        geoJson.AppendLine("        \"coordinates\": [");
        geoJson.AppendLine($"          [{routeData.startPoint.y}, {routeData.startPoint.x}],");

        foreach (WaypointData wp in routeData.waypoints)
        {
            geoJson.AppendLine($"          [{wp.geoPosition.y}, {wp.geoPosition.x}],");
        }

        geoJson.AppendLine($"          [{routeData.endPoint.y}, {routeData.endPoint.x}]");
        geoJson.AppendLine("        ]");
        geoJson.AppendLine("      },");
        geoJson.AppendLine("      \"properties\": {");
        geoJson.AppendLine("        \"name\": \"Navigation Route\"");
        geoJson.AppendLine("      }");
        geoJson.AppendLine("    },");

        // Agregar waypoints como puntos
        for (int i = 0; i < routeData.waypoints.Count; i++)
        {
            WaypointData wp = routeData.waypoints[i];
            geoJson.AppendLine("    {");
            geoJson.AppendLine("      \"type\": \"Feature\",");
            geoJson.AppendLine("      \"geometry\": {");
            geoJson.AppendLine("        \"type\": \"Point\",");
            geoJson.AppendLine($"        \"coordinates\": [{wp.geoPosition.y}, {wp.geoPosition.x}]");
            geoJson.AppendLine("      },");
            geoJson.AppendLine("      \"properties\": {");
            geoJson.AppendLine($"        \"waypoint_index\": {i},");
            geoJson.AppendLine($"        \"type\": \"{wp.type}\",");
            geoJson.AppendLine($"        \"name_of_surface\": \"{wp.name_of_surface}\",");
            geoJson.AppendLine($"        \"elevation_from_position_standard\": {wp.elevation_from_position_standard:F2}");

            if (wp.stair_count > 0)
            {
                geoJson.AppendLine($"        ,\"stair_count\": {wp.stair_count}");
            }

            geoJson.AppendLine("      }");
            geoJson.Append("    }");

            if (i < routeData.waypoints.Count - 1)
                geoJson.AppendLine(",");
            else
                geoJson.AppendLine();
        }

        geoJson.AppendLine("  ]");
        geoJson.AppendLine("}");

        File.WriteAllText(geoJsonFilePath, geoJson.ToString());
        Debug.Log($"GeoJSON exportado a: {geoJsonFilePath}");
    }

    void OnDrawGizmos()
    {
        if (target != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(target.position, 0.3f);
        }

        if (agent != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(transform.position, 0.2f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, target.position);

            // Dibujar waypoints detectados
            Gizmos.color = Color.blue;
            foreach (WaypointData wp in collectedWaypoints)
            {
                Gizmos.DrawSphere(wp.worldPosition, 0.15f);
            }
        }
    }
}

// Componente auxiliar para escaleras
[System.Serializable]
public class StairComponent : MonoBehaviour
{
    public int stepCount = 10;
    public float stepHeight = 0.2f;
    public string stairMaterial = "concrete";
}

/*
 public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public float jumpForce = 10f;
    private Rigidbody rb;
    private bool isGrounded = true;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(moveX, 0, moveZ) * speed;
        rb.MovePosition(rb.position + movement * Time.deltaTime);

        // Saltar si está en el suelo y se presiona la barra espaciadora
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }
    }

    // Detectar si está en el suelo usando colisiones
    void OnCollisionEnter(Collision collision)
    {
        // Solo lo marcamos en el suelo si colisiona con algo debajo
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                isGrounded = true;
                break;
            }
        }
    }
}*/

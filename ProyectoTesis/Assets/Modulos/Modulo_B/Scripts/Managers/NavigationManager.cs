using UnityEngine;
using System.Collections.Generic;

public class NavigationManager : MonoBehaviour
{
    [Header("Referencias")]
    public AudioManager audioManager;

    [Header("Configuraci�n")]
    [SerializeField] private float tiempoEntreInstrucciones = 3f;

    private RouteData currentRoute;
    private int currentWaypointIndex = 0;
    private float lastInstructionTime = 0f;

    void Start()
    {
        // Suscribirse si tienes un manager de rutas
        // RouteManager.OnRouteUpdated += OnRouteUpdated;
    }

    public void ProcessRoute(Route route)
    {
        if (route?.route == null) return;

        currentRoute = route.route;
        currentWaypointIndex = 0;

        // Dar instrucci�n inicial
        GiveRouteOverview();
    }

    private void GiveRouteOverview()
    {
        if (currentRoute == null) return;

        string overview = $"Ruta calculada. Distancia total: {currentRoute.total_distance_meters} metros. " +
                         $"Pasos totales: {currentRoute.total_steps}. Comenzando navegaci�n.";

        audioManager.SpeakInstruction(overview);
    }

    public void ProcessNextWaypoint()
    {
        if (currentRoute == null || currentWaypointIndex >= currentRoute.waypoints.Count)
            return;

        // Verificar cooldown
        if (Time.time - lastInstructionTime < tiempoEntreInstrucciones)
            return;

        Waitpoints waypoint = currentRoute.waypoints[currentWaypointIndex];
        string instruction = GenerateInstruction(waypoint);

        if (!string.IsNullOrEmpty(instruction))
        {
            audioManager.SpeakInstruction(instruction);
            lastInstructionTime = Time.time;
        }

        currentWaypointIndex++;

        // Verificar si llegamos al final
        if (currentWaypointIndex >= currentRoute.waypoints.Count)
        {
            audioManager.SpeakInstruction("Has llegado a tu destino.");
        }
    }

    private string GenerateInstruction(Waitpoints waypoint)
    {
        switch (waypoint.type.ToLower())
        {
            case "walk":
                return $"Camina {waypoint.distance_meters} metros" +
                       (waypoint.steps.HasValue ? $", aproximadamente {waypoint.steps} pasos." : ".");

            case "stairs_down":
                return $"Escalones hacia abajo" +
                       (waypoint.num_steps.HasValue ? $". {waypoint.num_steps} escalones." : ".") +
                       " Ten cuidado.";

            case "stairs_up":
                return $"Escalones hacia arriba" +
                       (waypoint.num_steps.HasValue ? $". {waypoint.num_steps} escalones." : ".");

            case "turn":
                return $"Gira a la {TranslateDirection(waypoint.direction)}.";

            default:
                return $"Contin�a {waypoint.distance_meters} metros.";
        }
    }

    private string TranslateDirection(string direction)
    {
        switch (direction?.ToLower())
        {
            case "left": return "izquierda";
            case "right": return "derecha";
            case "north": return "norte";
            case "south": return "sur";
            case "east": return "este";
            case "west": return "oeste";
            default: return direction;
        }
    }

    // M�todo para dar instrucciones manuales
    public void GiveManualInstruction(string instruction)
    {
        audioManager.SpeakInstruction(instruction);
    }

    // M�todo para reiniciar navegaci�n
    public void ResetNavigation()
    {
        currentWaypointIndex = 0;
        lastInstructionTime = 0f;
    }

    // M�todo p�blico para avanzar manualmente
    [ContextMenu("Next Waypoint")]
    public void ManualNextWaypoint()
    {
        ProcessNextWaypoint();
    }
}
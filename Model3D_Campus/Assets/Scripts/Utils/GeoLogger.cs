using UnityEngine;
using System.IO;
using System.Text;
using System.Collections.Generic;

public static class GeoLogger
{
    public static void LogPlayerPosition(string logFilePath, Vector3 position, UnityEngine.AI.NavMeshAgent agent, float totalDistance)
    {
        StringBuilder debugLog = new StringBuilder();
        debugLog.AppendLine($"[{Time.time:F2}s] Posición actual: {position}");
        debugLog.AppendLine($"- Distancia restante: {(agent.hasPath ? agent.remainingDistance.ToString("F2") : "sin ruta")}");
        debugLog.AppendLine($"- Velocidad actual: {agent.velocity.magnitude:F2} m/s");
        debugLog.AppendLine($"- ¿Tiene ruta?: {agent.hasPath}");
        debugLog.AppendLine($"- ¿Está en NavMesh?: {agent.isOnNavMesh}");
        debugLog.AppendLine($"- ¿Path pendiente?: {agent.pathPending}");
        debugLog.AppendLine($"- ¿Está atascado?: {(agent.velocity.sqrMagnitude < 0.01f ? "posible" : "no")}");
        debugLog.AppendLine($"- Total recorrido: {totalDistance:F2} m\n");

        File.AppendAllText(logFilePath, debugLog.ToString());
    }

    public static void ExportGeoJSON(string geoJsonFilePath, GeoRouteData routeData)
    {
        StringBuilder geoJson = new StringBuilder();
        geoJson.AppendLine("{");
        geoJson.AppendLine("  \"type\": \"FeatureCollection\",");
        geoJson.AppendLine("  \"route_info\": {");
        geoJson.AppendLine($"    \"start_point\": {{\"latitude\": {routeData.startPoint.x}, \"longitude\": {routeData.startPoint.y}}},");
        geoJson.AppendLine($"    \"end_point\": {{\"latitude\": {routeData.endPoint.x}, \"longitude\": {routeData.endPoint.y}}},");
        geoJson.AppendLine($"    \"total_distance_meters\": {routeData.totalDistance:F2}");
        geoJson.AppendLine("  },");
        geoJson.AppendLine("  \"features\": [");

        // LineString con ruta
        geoJson.AppendLine("    {");
        geoJson.AppendLine("      \"type\": \"Feature\",");
        geoJson.AppendLine("      \"geometry\": {");
        geoJson.AppendLine("        \"type\": \"LineString\",");
        geoJson.AppendLine("        \"coordinates\": [");
        geoJson.AppendLine($"          [{routeData.startPoint.y}, {routeData.startPoint.x}],");

        foreach (WaypointData wp in routeData.waypoints)
            geoJson.AppendLine($"          [{wp.geoPosition.y}, {wp.geoPosition.x}],");

        geoJson.AppendLine($"          [{routeData.endPoint.y}, {routeData.endPoint.x}]");
        geoJson.AppendLine("        ]");
        geoJson.AppendLine("      },");
        geoJson.AppendLine("      \"properties\": { \"name\": \"Navigation Route\" }");
        geoJson.AppendLine("    },");

        // Puntos
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
                geoJson.AppendLine($",        \"stair_count\": {wp.stair_count}");

            geoJson.AppendLine("      }");
            geoJson.Append(i < routeData.waypoints.Count - 1 ? "    }," : "    }");
        }

        geoJson.AppendLine("  ]");
        geoJson.AppendLine("}");

        File.WriteAllText(geoJsonFilePath, geoJson.ToString());
        Debug.Log($"GeoJSON exportado a: {geoJsonFilePath}");
    }
}

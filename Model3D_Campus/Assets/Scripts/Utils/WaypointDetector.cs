using UnityEngine;
using System.Collections.Generic;

public static class WaypointDetector
{
    /// <summary>
    /// Analiza las esquinas (corners) del NavMesh para detectar Waypoints.
    /// </summary>
    public static List<WaypointData> DetectWaypoints(Vector3[] pathCorners, float metersPerUnityUnit, double refLat, double refLon)
    {
        List<WaypointData> waypoints = new List<WaypointData>();

        foreach (var corner in pathCorners)
        {
            WaypointData wp = AnalyzePositionForWaypoint(corner, metersPerUnityUnit, refLat, refLon);
            if (wp != null)
            {
                waypoints.Add(wp);
            }
        }

        return waypoints;
    }

    /// <summary>
    /// Analiza una posición en el mundo para detectar superficies, obstáculos y escaleras.
    /// </summary>
    private static WaypointData AnalyzePositionForWaypoint(Vector3 position, float metersPerUnityUnit, double refLat, double refLon)
    {
        WaypointData waypoint = new WaypointData
        {
            worldPosition = position,
            geoPosition = WorldToGeoCoordinates(position, metersPerUnityUnit, refLat, refLon),
            elevation_from_position_standard = position.y - 1.6f
        };

        Collider[] nearbyObjects = Physics.OverlapSphere(position, 2.0f);

        foreach (Collider col in nearbyObjects)
        {
            GameObject obj = col.gameObject;

            // Escaleras
            if (obj.name.ToLower().Contains("escalera") || obj.name.ToLower().Contains("stairs") ||
                obj.name.ToLower().Contains("gradas") || obj.CompareTag("Stairs"))
            {
                waypoint.type = "obstacle";
                waypoint.name_of_surface = obj.name;

                StairComponent stairComp = obj.GetComponent<StairComponent>();
                waypoint.stair_count = stairComp != null ? stairComp.stepCount : EstimateStairCount(obj);
            }

            // Superficies
            if (obj.name.ToLower().Contains("piso") || obj.name.ToLower().Contains("ground") ||
                obj.name.ToLower().Contains("floor") || obj.CompareTag("Ground"))
            {
                waypoint.type = "surface";
                waypoint.name_of_surface = obj.name;
                return waypoint;
            }

            // Obstáculos
            if (obj.name.ToLower().Contains("obstacle") || obj.name.ToLower().Contains("obstaculo") ||
                obj.CompareTag("Obstacle"))
            {
                waypoint.type = "obstacle";
                waypoint.name_of_surface = obj.name;
                return waypoint;
            }
        }

        waypoint.type = "surface";
        waypoint.name_of_surface = "generic_surface";

        return waypoint;
    }

    /// <summary>
    /// Convierte coordenadas del mundo de Unity a coordenadas geográficas (lat/lon).
    /// </summary>
    private static Vector2 WorldToGeoCoordinates(Vector3 worldPos, float metersPerUnityUnit, double refLat, double refLon)
    {
        float deltaX = worldPos.x * metersPerUnityUnit;
        float deltaZ = worldPos.z * metersPerUnityUnit;

        double lat = refLat + (deltaZ / 111320.0);
        double lon = refLon + (deltaX / (111320.0 * Mathf.Cos((float)(refLat * Mathf.Deg2Rad))));

        return new Vector2((float)lat, (float)lon);
    }

    private static int EstimateStairCount(GameObject obj)
    {
        float stairHeight = 1.0f;
        Collider stairCollider = obj.GetComponent<Collider>();
        if (stairCollider != null)
            stairHeight = stairCollider.bounds.size.y;
        else
        {
            Renderer stairRenderer = obj.GetComponent<Renderer>();
            if (stairRenderer != null)
                stairHeight = stairRenderer.bounds.size.y;
        }

        return Mathf.RoundToInt(stairHeight / 0.2f);
    }
}

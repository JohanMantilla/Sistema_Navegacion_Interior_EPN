using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GeoRouteData
{
    public Vector2 startPoint;  // lat, lon
    public Vector2 endPoint;    // lat, lon
    public float totalDistance; // en metros
    public List<WaypointData> waypoints;
}

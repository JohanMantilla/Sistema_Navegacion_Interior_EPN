using UnityEngine;

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

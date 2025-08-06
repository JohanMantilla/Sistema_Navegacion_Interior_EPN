using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RouteCollection
{
    public string type;
    public RouteInfo route_info;
    public List<Feature> features;
}

[System.Serializable]
public class RouteInfo
{
    public RoutePoint start_point;
    public RoutePoint end_point;
    public double total_distance_meters;
}

[System.Serializable]
public class RoutePoint
{
    public double latitude;
    public double longitude;
}

[System.Serializable]
public class Feature
{
    public string type;
    public Geometry geometry;
    public Properties properties;
}

[System.Serializable]
public class Geometry
{
    public string type;
    // Usar object para manejar tanto arrays simples como arrays de arrays
    public object coordinates;

    // Métodos auxiliares para obtener coordenadas
    public List<double> GetPointCoordinates()
    {
        if (coordinates is Newtonsoft.Json.Linq.JArray jArray)
        {
            return jArray.ToObject<List<double>>();
        }
        return null;
    }

    public List<List<double>> GetLineStringCoordinates()
    {
        if (coordinates is Newtonsoft.Json.Linq.JArray jArray)
        {
            return jArray.ToObject<List<List<double>>>();
        }
        return null;
    }
}

[System.Serializable]
public class Properties
{
    public string name;
    public int? waypoint_index;
    public string type;
    public string name_of_surface;
    public double? elevation_from_position_standard;
    public int? stair_count;
}

// Clase auxiliar para trabajar con puntos de coordenadas
[System.Serializable]
public class AlertPoint
{
    public double latitude;
    public double longitude;
    public string alertType;
    public string surfaceName;
    public int? stairCount;
    public double? elevation;

    public AlertPoint(double lat, double lon, Properties props)
    {
        latitude = lat;
        longitude = lon;
        alertType = props.type;
        surfaceName = props.name_of_surface;
        stairCount = props.stair_count;
        elevation = props.elevation_from_position_standard;
    }
}
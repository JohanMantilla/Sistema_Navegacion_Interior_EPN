using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Route
{
    public RouteData route;    
}

[System.Serializable]
public class RouteData {
    public Start start;
    public End end;
    public List<Waitpoints> waypoints;
    public double total_distance_meters;
    public int total_steps;
}

[System.Serializable]
public class Start {
    public double latitude;
    public double longitude;
    public double altitude;
}

[System.Serializable]
public class End { 
    public double latitude;
    public double longitude;
    public double altitude;
}

[System.Serializable]
public class Waitpoints {
    public string type;
    public double distance_meters;
    public int? steps;
    public double elevation_change;
    
    public int? num_steps;
    public double? step_height_meters;
    public double? step_width_meters;
    public double? step_length_meters;

    public string direction;
}


//TODO: VALIDAR LOS SERIALICE, BORRARLOS  SI NECESITO 

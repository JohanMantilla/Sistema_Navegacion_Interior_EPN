using UnityEngine;
public class Location
{
    public string nombre;
    public double longitude;
    public double latitude;
    public Location(string nombre, double longitude, double latitude)
    {
        this.nombre = nombre;
        this.longitude = longitude;
        this.latitude = latitude;
    }
}
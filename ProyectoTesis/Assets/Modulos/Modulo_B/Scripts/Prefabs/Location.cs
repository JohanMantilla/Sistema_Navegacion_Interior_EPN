using UnityEngine;
public class Location
{
    public string nombre;
    public float longitude;
    public float latitude;
    public Location(string nombre, float longitude, float latitude)
    {
        this.nombre = nombre;
        this.longitude = longitude;
        this.latitude = latitude;
    }
}
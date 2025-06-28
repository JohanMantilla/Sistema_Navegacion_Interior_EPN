using System.Collections;
using UnityEngine;

public class GPSManager : MonoBehaviour
{
    public static GPSManager Instance;

    public float latitude;
    public float longitude;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persistente
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        StartCoroutine(StartGPS());
    }

    IEnumerator StartGPS()
    {
        if (!Input.location.isEnabledByUser)
        {
            Debug.Log("GPS est� deshabilitado por el usuario.");
            yield break;
        }

        Input.location.Start();

        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.Log("Error al obtener ubicaci�n GPS.");
            yield break;
        }

        latitude = Input.location.lastData.latitude;
        longitude = Input.location.lastData.longitude;

        Debug.Log($"Ubicaci�n GPS obtenida: Lat: {latitude}, Lon: {longitude}");
    }
}


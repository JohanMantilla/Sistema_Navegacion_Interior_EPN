using UnityEngine;

public class CameraZoom : MonoBehaviour
{
    public float zoomSpeed = 10f;        // Velocidad del zoom
    public float minZoom = 5f;           // Distancia m�nima permitida
    public float maxZoom = 40f;          // Distancia m�xima permitida

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        // Si es una c�mara en perspectiva
        if (!cam.orthographic)
        {
            cam.fieldOfView -= scroll * zoomSpeed;
            cam.fieldOfView = Mathf.Clamp(cam.fieldOfView, minZoom, maxZoom);
        }
        else
        {
            // Si es una c�mara ortogr�fica (2D o estilo isom�trico)
            cam.orthographicSize -= scroll * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
    }
}


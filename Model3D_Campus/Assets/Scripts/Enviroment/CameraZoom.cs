using UnityEngine;

public class CameraZoom : MonoBehaviour
{
    public float zoomSpeed = 10f;        // Velocidad del zoom
    public float minZoom = 5f;           // Distancia mínima permitida
    public float maxZoom = 40f;          // Distancia máxima permitida

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        // Si es una cámara en perspectiva
        if (!cam.orthographic)
        {
            cam.fieldOfView -= scroll * zoomSpeed;
            cam.fieldOfView = Mathf.Clamp(cam.fieldOfView, minZoom, maxZoom);
        }
        else
        {
            // Si es una cámara ortográfica (2D o estilo isométrico)
            cam.orthographicSize -= scroll * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
    }
}


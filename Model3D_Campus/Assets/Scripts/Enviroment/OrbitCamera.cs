using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    public Transform target;          // A qué objeto orbita la cámara
    public float distance = 10f;      // Distancia al objetivo
    public float xSpeed = 120f;       // Velocidad rotación horizontal
    public float ySpeed = 120f;       // Velocidad rotación vertical
    public float zoomSpeed = 5f;      // Velocidad del zoom

    public float yMinLimit = -20f;    // Límite inferior de rotación vertical
    public float yMaxLimit = 80f;     // Límite superior de rotación vertical
    public float minDistance = 3f;    // Zoom mínimo
    public float maxDistance = 40f;   // Zoom máximo

    private float x = 0f;
    private float y = 0f;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;

        if (GetComponent<Rigidbody>())
            GetComponent<Rigidbody>().freezeRotation = true;
    }

    void Update()
    {
        if (target)
        {
            if (Input.GetMouseButton(1)) // botón derecho del mouse
            {
                x += Input.GetAxis("Mouse X") * xSpeed * Time.deltaTime;
                y -= Input.GetAxis("Mouse Y") * ySpeed * Time.deltaTime;
                y = Mathf.Clamp(y, yMinLimit, yMaxLimit);
            }

            // Zoom con la rueda del mouse
            distance -= Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);

            Quaternion rotation = Quaternion.Euler(y, x, 0);
            Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
            Vector3 position = rotation * negDistance + target.position;

            transform.rotation = rotation;
            transform.position = position;
        }
    }
}

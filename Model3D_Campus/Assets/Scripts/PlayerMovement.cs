using UnityEngine;
using UnityEngine.AI;
using System.IO;

public class PlayerMovement : MonoBehaviour
{
    public Transform target;
    private NavMeshAgent agent;
    private string logFilePath;
    private float logInterval = 1.0f;
    private float timer = 0f;
    private bool hasArrived = false;

    void Start()
    {

    }

    void Update()
    {


    }

}




/*
 public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public float jumpForce = 10f;
    private Rigidbody rb;
    private bool isGrounded = true;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(moveX, 0, moveZ) * speed;
        rb.MovePosition(rb.position + movement * Time.deltaTime);

        // Saltar si está en el suelo y se presiona la barra espaciadora
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }
    }

    // Detectar si está en el suelo usando colisiones
    void OnCollisionEnter(Collision collision)
    {
        // Solo lo marcamos en el suelo si colisiona con algo debajo
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                isGrounded = true;
                break;
            }
        }
    }
}*/

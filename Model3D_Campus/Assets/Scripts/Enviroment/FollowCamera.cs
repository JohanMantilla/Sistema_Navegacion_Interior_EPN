using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public Transform target; // arrastra aquí el cubo
    public Vector3 offset = new Vector3(0, 5, -10);
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (target != null)
            transform.position = target.position + offset;
        transform.LookAt(target);
    }
}

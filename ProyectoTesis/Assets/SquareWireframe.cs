using UnityEngine;

public class SquareWireframe : MonoBehaviour
{
    public Material lineMaterial;
    public float lineWidth = 0.02f;
    public Color lineColor = Color.red;

    void Start()
    {
        CreateSquare();
    }

    void CreateSquare()
    {
        Vector3[] corners = {
            new Vector3(-0.5f, -0.5f, 0), // Esquina inferior izquierda
            new Vector3(0.5f, -0.5f, 0),  // Esquina inferior derecha
            new Vector3(0.5f, 0.5f, 0),   // Esquina superior derecha
            new Vector3(-0.5f, 0.5f, 0)   // Esquina superior izquierda
        };

        string[] sideNames = { "Bottom", "Right", "Top", "Left" };

        for (int i = 0; i < 4; i++)
        {
            GameObject line = new GameObject(sideNames[i]);
            line.transform.parent = transform;

            LineRenderer lr = line.AddComponent<LineRenderer>();
            lr.material = lineMaterial;
            lr.endColor = lineColor;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.positionCount = 2;
            lr.useWorldSpace = false;

            // Conectar cada esquina con la siguiente (y la última con la primera)
            lr.SetPosition(0, corners[i]);
            lr.SetPosition(1, corners[(i + 1) % 4]);
        }
    }
}
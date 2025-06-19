using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

public class ARObjectDetection : MonoBehaviour
{
    public Camera arCamera;

    private void UpdateBoxPosition(GameObject box, int[] bbox, float distance)
    {
        // Convertir coordenadas del bbox [x1, y1, x2, y2] a coordenadas de pantalla
        float x1 = bbox[0];
        float y1 = bbox[1];
        float x2 = bbox[2];
        float y2 = bbox[3];

        // Obtener el LineRenderer
        LineRenderer line = box.GetComponent<LineRenderer>();

        // Convertir coordenadas de pantalla a puntos en el mundo 3D
        Vector3[] worldPositions = new Vector3[4];

        // Esquina inferior izquierda
        worldPositions[0] = arCamera.ScreenToWorldPoint(
            new Vector3(x1, y1, distance));

        // Esquina superior izquierda
        worldPositions[1] = arCamera.ScreenToWorldPoint(
            new Vector3(x1, y2, distance));

        // Esquina superior derecha
        worldPositions[2] = arCamera.ScreenToWorldPoint(
            new Vector3(x2, y2, distance));

        // Esquina inferior derecha
        worldPositions[3] = arCamera.ScreenToWorldPoint(
            new Vector3(x2, y1, distance));

        // Asignar las posiciones al LineRenderer
        line.positionCount = 4;
        line.SetPositions(worldPositions);

        // Opcional: Añadir etiqueta de texto
        UpdateLabel(box, bbox, distance);
    }

    private void UpdateLabel(GameObject box, int[] bbox, float distance)
    {
        Text label = box.GetComponentInChildren<Text>();
        if (label != null)
        {
            // Posicionar la etiqueta arriba del bounding box
            float xCenter = (bbox[0] + bbox[2]) / 2f;
            float yTop = bbox[3];

            Vector3 labelPos = arCamera.ScreenToWorldPoint(
                new Vector3(xCenter, yTop + 20, distance)); // +20 para margen

            label.transform.position = labelPos;

            // Mantener la etiqueta mirando a la cámara
            label.transform.LookAt(arCamera.transform);
            label.transform.Rotate(0, 180, 0); // Voltear para que no se vea al revés
        }
    }
}

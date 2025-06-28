using UnityEngine;
using System.IO;

public class Map2DExporter : MonoBehaviour
{
    public Camera orthoCam;
    public int resolution = 2048;

    [ContextMenu("Exportar Mapa 2D")]
    void Start()
    {
        Debug.Log("Iniciando exportación desde Start");
        ExportMap();
    }
    public void ExportMap()
    {
        Debug.Log("Iniciando exportación de mapa...");

        if (orthoCam == null)
        {
            Debug.LogError("No se asignó una cámara ortográfica.");
            return;
        }

        try
        {
            RenderTexture rt = new RenderTexture(resolution, resolution, 24);
            orthoCam.targetTexture = rt;

            Texture2D img = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);

            orthoCam.Render();
            Debug.Log("Cámara renderizada correctamente.");

            RenderTexture.active = rt;
            img.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
            img.Apply();

            orthoCam.targetTexture = null;
            RenderTexture.active = null;
            Destroy(rt);

            string filePath = Application.dataPath + "/map2D.png";
            File.WriteAllBytes(filePath, img.EncodeToPNG());
            Debug.Log("Mapa exportado correctamente en: " + filePath);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error al exportar mapa: " + ex.Message);
        }
    }
}

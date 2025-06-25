using System.Collections;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

public class AutoJsonTester : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private bool startAutoTest = false;
    [SerializeField] private float timeBetweenScenarios = 3f;

    private string jsonPath;
    private int currentScenario = 0;
    private bool isRunning = false;

    void Start()
    {
        // Cambio clave: Escribe en PersistentDataPath en lugar de StreamingAssets
        jsonPath = Path.Combine(Application.persistentDataPath, "test_objects.json"); // Nombre distinto para evitar conflictos

        Debug.Log($"Ruta del JSON de prueba: {jsonPath}"); // Para depuración

        if (startAutoTest)
        {
            StartAutoTest();
        }
    }

    public void StartAutoTest()
    {
        if (!isRunning)
        {
            isRunning = true;
            currentScenario = 0;
            Debug.Log("🚀 INICIANDO PRUEBAS AUTOMÁTICAS (Modo Prueba)");
            StartCoroutine(AutoTestCoroutine());
        }
    }

    public void StopAutoTest()
    {
        isRunning = false;
        Debug.Log("⏹️ PRUEBAS DETENIDAS");
    }

    IEnumerator AutoTestCoroutine()
    {
        while (isRunning)
        {
            string jsonData = CreateScenario(currentScenario);
            WriteJsonFile(jsonData);
            LogCurrentScenario(currentScenario);

            yield return new WaitForSeconds(timeBetweenScenarios);
            currentScenario = (currentScenario + 1) % 6; // Ciclo entre 0 y 5
        }
    }

    string CreateScenario(int scenarioIndex)
    {
        var detection = new ObjectDetection
        {
            timestamp = System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            objects = new System.Collections.Generic.List<Objects>(),
            performance = new Performance
            {
                fps = 30.0f,
                processing_time = Random.Range(20f, 35f),
                cpu_usage = Random.Range(50f, 80f),
                memory_usage = Random.Range(40f, 70f)
            }
        };

        switch (scenarioIndex)
        {
            case 0: // Escenario vacío
                break;

            case 1: // Persona
                detection.objects.Add(new Objects
                {
                    object_id = 1,
                    name = "person",
                    confidence = 0.89f,
                    bbox = new float[] { 200, 150, 300, 400 },
                    speed = 1.2f,
                    distance = 6.0f,
                    direction = "north"
                });
                break;

            case 2: // Auto
                detection.objects.Add(new Objects
                {
                    object_id = 2,
                    name = "car",
                    confidence = 0.95f,
                    bbox = new float[] { 150, 200, 350, 400 },
                    speed = 8.5f,
                    distance = 4.5f,
                    direction = "east"
                });
                break;

            case 3: // Auto peligroso
                detection.objects.Add(new Objects
                {
                    object_id = 2,
                    name = "car",
                    confidence = 0.95f,
                    bbox = new float[] { 150, 200, 350, 400 },
                    speed = 12.0f,
                    distance = 2.2f,
                    direction = "east"
                });
                break;

            case 4: // Múltiples objetos
                detection.objects.Add(new Objects { object_id = 1, name = "person", confidence = 0.87f, bbox = new float[] { 100, 150, 200, 350 }, speed = 1.0f, distance = 7.0f, direction = "north" });
                detection.objects.Add(new Objects { object_id = 3, name = "bicycle", confidence = 0.83f, bbox = new float[] { 50, 100, 150, 300 }, speed = 4.2f, distance = 5.5f, direction = "west" });
                detection.objects.Add(new Objects { object_id = 4, name = "dog", confidence = 0.78f, bbox = new float[] { 250, 300, 320, 380 }, speed = 2.1f, distance = 3.8f, direction = "northeast" });
                break;

            case 5: // Camión de emergencia
                detection.objects.Add(new Objects
                {
                    object_id = 5,
                    name = "truck",
                    confidence = 0.92f,
                    bbox = new float[] { 120, 180, 380, 420 },
                    speed = 15.5f,
                    distance = 2.8f,
                    direction = "west"
                });
                break;
        }

        return JsonConvert.SerializeObject(detection, Formatting.Indented);
    }

    void WriteJsonFile(string jsonContent)
    {
        try
        {
            File.WriteAllText(jsonPath, jsonContent);
            Debug.Log($"Escenario {currentScenario + 1} escrito en: {jsonPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error escribiendo JSON: {e.Message}");
        }
    }

    void LogCurrentScenario(int scenarioIndex)
    {
        string[] scenarioNames = {
            "🔇 Silencio (sin objetos)",
            "🚶 Persona (alerta normal)",
            "🚗 Auto (alerta media)",
            "⚠️ Auto peligroso (alerta alta)",
            "👥 Múltiples objetos (priorizar cercanos)",
            "🚛 Camión (emergencia)"
        };
        Debug.Log($"PRUEBA: {scenarioNames[scenarioIndex]}");
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 150));
        GUILayout.Label($"<b>Modo Prueba</b> {(isRunning ? "🟢" : "🔴")}");

        if (GUILayout.Button(isRunning ? "Detener Pruebas" : "Iniciar Pruebas"))
        {
            if (isRunning) StopAutoTest(); else StartAutoTest();
        }

        if (isRunning)
        {
            GUILayout.Label($"Escenario: {currentScenario + 1}/6");
            GUILayout.Label($"Próximo cambio: {timeBetweenScenarios}s");
        }
        GUILayout.EndArea();
    }
}
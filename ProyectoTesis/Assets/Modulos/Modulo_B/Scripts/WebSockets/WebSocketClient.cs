using System;
using UnityEngine;
using Newtonsoft.Json;
using NativeWebSocket;

public class WebSocketClient : MonoBehaviour
{
    [Header("WebSocket Configuration")]
    public string serverIP = "192.168.1.43";
    public int serverPort = 8080;
    
    private WebSocket websocket;
    private string lastJsonObjectDetection;
    
    // Evento para notificar cambios en la detección de objetos
    public static event Action<ObjectDetection> OnChangeObjectionDetection;
    // NOMBRE
    // Latitud, longitud
    
    async void Start()
    {
        await ConnectToWebSocket();
    }
    
    async System.Threading.Tasks.Task ConnectToWebSocket()
    {
        string websocketUrl = $"ws://{serverIP}:{serverPort}";
        websocket = new WebSocket(websocketUrl);
        
        websocket.OnOpen += () =>
        {
            Debug.Log("✅ Conectado al servidor WebSocket para detección de objetos");
        };
        
        websocket.OnMessage += (bytes) =>
        {
            var rawMessage = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log("📨 Datos de objetos recibidos: " + rawMessage);
            
            // Procesa los datos de detección de objetos
            ProcessObjectDetectionData(rawMessage);
        };
        
        websocket.OnError += (e) =>
        {
            Debug.Log("❌ Error en WebSocket: " + e);
        };
        
        websocket.OnClose += (e) =>
        {
            Debug.Log("🔒 Conexión WebSocket cerrada");
            // Intenta reconectar después de 5 segundos
            Invoke(nameof(ReconnectWebSocket), 5f);
        };
        
        try
        {
            await websocket.Connect();
        }
        catch (Exception e)
        {
            Debug.LogError("Error al conectar WebSocket: " + e.Message);
        }
    }
    
    void ProcessObjectDetectionData(string objectData)
    {
        // Solo procesa si los datos han cambiado
        if (objectData != null && objectData != lastJsonObjectDetection)
        {
            lastJsonObjectDetection = objectData;
            DeserializeJsonObjectDetection(objectData);
            
        }
    }
    
    void DeserializeJsonObjectDetection(string objectDetectionSerialized)
    {
        try
        {
            ObjectDetection objectDetection = JsonConvert.DeserializeObject<ObjectDetection>(objectDetectionSerialized);
            
            if (objectDetection != null)
            {
                // Dispara el evento para notificar a otros scripts
                OnChangeObjectionDetection?.Invoke(objectDetection);
                
                Debug.Log($"👁️ Detección actualizada - {objectDetection.objects.Count} objetos detectados");
                Debug.Log($"📊 FPS: {objectDetection.performance.fps:F1}, CPU: {objectDetection.performance.cpu_usage:F1}%");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error al deserializar detección de objetos: " + e.Message);
        }
    }
    
    async void ReconnectWebSocket()
    {
        Debug.Log("🔄 Intentando reconectar WebSocket...");
        await ConnectToWebSocket();
    }
    
    void Update()
    {
        // Procesa la cola de mensajes de WebSocket
        websocket?.DispatchMessageQueue();
    }
    
    private async void OnApplicationQuit()
    {
        if (websocket != null)
        {
            await websocket.Close();
        }
    }
    
    // Método opcional para verificar el estado de la conexión
    public bool IsConnected()
    {
        return websocket != null && websocket.State == WebSocketState.Open;
    }
    
    // Método opcional para enviar comandos al servidor (si es necesario)
    public async void SendCommand(string command)
    {
        if (IsConnected())
        {
            await websocket.SendText(command);
        }
        else
        {
            Debug.LogWarning("WebSocket no está conectado. No se puede enviar: " + command);
        }
    }
}
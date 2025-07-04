using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Linq;

public class AudioFeedback : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private float audioDelayBetweenAnnouncements = 2f;
    [SerializeField] private float dangerCheckDelay = 0.5f;
    [SerializeField] private float dangerDistanceThreshold = 3f;
    [SerializeField] private float collisionDistanceThreshold = 2f;

    private ObjectDetection currentDetection;
    private ObjectDetection previousDetection;
    private bool isAudioPlaying = false;
    private Coroutine audioCoroutine;

    // Prioridades de peligro (mayor número = más peligroso)
    private Dictionary<string, int> dangerPriority = new Dictionary<string, int>
    {
        {"car", 10},
        {"motorcycle", 9},
        {"bus", 8},
        {"bench", 6},
        {"bicycle", 6},
        {"sports ball", 4},
        {"dining table", 4},
        {"person", 4},
        {"skateboard", 4},
        {"fire hydrant", 3},
        {"umbrella", 3},
        {"cat", 3},
        {"dog", 3},
        {"chair", 3},
        {"backpack", 2},
        {"handbag", 2},
        {"suitcase", 2},
        {"stop sign", 2},
        {"bird", 1},
        {"bottle", 1},
        {"cell phone", 1},
        {"book", 1}
    };

    void Start()
    {
        WebSocketClient.OnChangeObjectionDetection += OnObjectDetectionUpdated;
    }

    void OnDestroy()
    {
        WebSocketClient.OnChangeObjectionDetection -= OnObjectDetectionUpdated;
    }

    private void OnObjectDetectionUpdated(ObjectDetection newDetection)
    {
        previousDetection = currentDetection;
        currentDetection = newDetection;

        ProcessAudioLogic();
    }

    private void ProcessAudioLogic()
    {
        if (currentDetection?.objects == null) return;

        // 1. Verificar peligro inmediato primero
        var dangerousObjects = GetDangerousObjects();
        if (dangerousObjects.Count > 0)
        {
            // Cancelar cualquier audio actual y dar instrucción de emergencia
            StopCurrentAudio();
            StartCoroutine(HandleDangerousObjects(dangerousObjects));
            return;
        }

        // 2. Verificar si hay cambios relevantes en los objetos
        if (HasRelevantChanges())
        {
            // Solo anunciar si no hay audio reproduciéndose
            if (!isAudioPlaying)
            {
                StartCoroutine(AnnounceObjects());
            }
        }

        // 3. Si no hay cambios, mantener silencio
    }

    private List<Objects> GetDangerousObjects()
    {
        var dangerousObjects = new List<Objects>();

        if (currentDetection?.objects == null) return dangerousObjects;

        foreach (var obj in currentDetection.objects)
        {
            // Verificar distancia peligrosa
            if (obj.distance <= dangerDistanceThreshold)
            {
                dangerousObjects.Add(obj);
            }

            // Verificar colisión inminente (objeto muy cerca y moviéndose hacia nosotros)
            if (obj.distance <= collisionDistanceThreshold && obj.speed > 0)
            {
                dangerousObjects.Add(obj);
            }
        }

        // Ordenar por prioridad de peligro y distancia
        return dangerousObjects
            .OrderByDescending(obj => GetDangerLevel(obj))
            .ThenBy(obj => obj.distance)
            .ToList();
    }

    private int GetDangerLevel(Objects obj)
    {
        int baseDanger = dangerPriority.ContainsKey(obj.name) ? dangerPriority[obj.name] : 1;

        // Aumentar peligro si está muy cerca
        if (obj.distance <= collisionDistanceThreshold) baseDanger += 3;
        else if (obj.distance <= dangerDistanceThreshold) baseDanger += 2;

        // Aumentar peligro si se mueve rápido
        if (obj.speed > 3f) baseDanger += 2;
        else if (obj.speed > 1f) baseDanger += 1;

        return baseDanger;
    }

    private bool HasRelevantChanges()
    {
        if (previousDetection?.objects == null || currentDetection?.objects == null)
            return currentDetection?.objects?.Count > 0;

        // Verificar objetos nuevos
        var previousIds = previousDetection.objects.Select(o => o.object_id).ToHashSet();
        var newObjects = currentDetection.objects.Where(o => !previousIds.Contains(o.object_id)).ToList();

        if (newObjects.Count > 0) return true;

        // Verificar cambios significativos en posición/distancia
        foreach (var currentObj in currentDetection.objects)
        {
            var previousObj = previousDetection.objects.FirstOrDefault(o => o.object_id == currentObj.object_id);
            if (previousObj != null)
            {
                // Si la distancia cambió significativamente (más de 1 metro)
                if (Mathf.Abs(currentObj.distance - previousObj.distance) > 1f)
                    return true;

                // Si cambió de dirección
                if (currentObj.direction != previousObj.direction)
                    return true;
            }
        }

        return false;
    }

    private IEnumerator HandleDangerousObjects(List<Objects> dangerousObjects)
    {
        isAudioPlaying = true;

        var mostDangerous = dangerousObjects.First();
        string instruction = GetAdvancedAvoidanceInstruction(mostDangerous);

        // Hablar la instrucción de emergencia
        AndroidTTSManager.Instance.Speak(instruction);

        // Esperar un momento antes de permitir más audio
        yield return new WaitForSeconds(3f);

        isAudioPlaying = false;
    }

    private string GetAdvancedAvoidanceInstruction(Objects dangerousObject)
    {
        string objectName = TranslateObjectName(dangerousObject.name);
        string direction = dangerousObject.direction.ToLower();
        float distance = dangerousObject.distance;
        float speed = dangerousObject.speed;

        // Determinar el tipo de maniobra según el contexto
        AvoidanceAction action = DetermineAvoidanceAction(dangerousObject);

        // Construir mensaje según urgencia y tipo de objeto
        if (distance <= collisionDistanceThreshold)
        {
            return BuildEmergencyMessage(objectName, action, speed > 2f);
        }
        else
        {
            return BuildCautionMessage(objectName, action, direction);
        }
    }

    private AvoidanceAction DetermineAvoidanceAction(Objects obj)
    {
        string direction = obj.direction.ToLower();
        string objectType = obj.name.ToLower();
        float speed = obj.speed;
        float distance = obj.distance;

        // Para vehículos rápidos, priorizar retroceso o parada
        if (IsVehicle(objectType) && speed > 2f)
        {
            switch (direction)
            {
                case "north":
                case "front":
                    return new AvoidanceAction("¡DETENTE INMEDIATAMENTE!", "No te muevas hasta que pase");
                case "south":
                case "back":
                    return new AvoidanceAction("Camina rápido hacia adelante", "El vehículo viene por atrás");
                case "east":
                case "right":
                    return new AvoidanceAction("Muévete rápido a la izquierda", "Aléjate del lado derecho");
                case "west":
                case "left":
                    return new AvoidanceAction("Muévete rápido a la derecha", "Aléjate del lado izquierdo");
                case "northeast":
                    return new AvoidanceAction("Retrocede y ve a la izquierda", "Evita la esquina derecha");
                case "northwest":
                    return new AvoidanceAction("Retrocede y ve a la derecha", "Evita la esquina izquierda");
                case "southeast":
                    return new AvoidanceAction("Avanza y ve a la izquierda", "Sal de esa zona");
                case "southwest":
                    return new AvoidanceAction("Avanza y ve a la derecha", "Sal de esa zona");
                default:
                    return new AvoidanceAction("¡PARA! Evalúa la situación", "Vehículo cerca moviéndose");
            }
        }

        // Para objetos estáticos o lentos
        if (speed <= 1f || IsStaticObject(objectType))
        {
            switch (direction)
            {
                case "north":
                case "front":
                    return new AvoidanceAction("Camina despacio alrededor por la derecha", "Obstáculo fijo adelante");
                case "south":
                case "back":
                    return new AvoidanceAction("Da un paso adelante", "Hay espacio al frente");
                case "east":
                case "right":
                    return new AvoidanceAction("Camina por la izquierda", "Evita el lado derecho");
                case "west":
                case "left":
                    return new AvoidanceAction("Camina por la derecha", "Evita el lado izquierdo");
                case "northeast":
                    return new AvoidanceAction("Ve hacia atrás y a la izquierda", "Rodea el obstáculo");
                case "northwest":
                    return new AvoidanceAction("Ve hacia atrás y a la derecha", "Rodea el obstáculo");
                case "southeast":
                    return new AvoidanceAction("Ve hacia adelante y a la izquierda", "Rodea por el otro lado");
                case "southwest":
                    return new AvoidanceAction("Ve hacia adelante y a la derecha", "Rodea por el otro lado");
                default:
                    return new AvoidanceAction("Camina despacio alrededor", "Obstáculo cerca");
            }
        }

        // Para personas, animales u objetos con movimiento moderado
        switch (direction)
        {
            case "north":
            case "front":
                return new AvoidanceAction("Reduce velocidad y ve por la derecha", "Dale espacio para pasar");
            case "south":
            case "back":
                return new AvoidanceAction("Sigue adelante despacio", "Te está siguiendo");
            case "east":
            case "right":
                return new AvoidanceAction("Mantén tu izquierda", "Dale paso por la derecha");
            case "west":
            case "left":
                return new AvoidanceAction("Mantén tu derecha", "Dale paso por la izquierda");
            case "northeast":
                return new AvoidanceAction("Ve despacio hacia atrás izquierda", "Dale espacio diagonal");
            case "northwest":
                return new AvoidanceAction("Ve despacio hacia atrás derecha", "Dale espacio diagonal");
            case "southeast":
                return new AvoidanceAction("Ve despacio hacia adelante izquierda", "Evita el encuentro");
            case "southwest":
                return new AvoidanceAction("Ve despacio hacia adelante derecha", "Evita el encuentro");
            default:
                return new AvoidanceAction("Reduce velocidad", "Hay movimiento cerca");
        }
    }

    private bool IsVehicle(string objectType)
    {
        return objectType == "car" || objectType == "truck" || objectType == "bus" ||
               objectType == "motorcycle" || objectType == "bicycle";
    }

    private bool IsStaticObject(string objectType)
    {
        return objectType == "pole" || objectType == "tree" || objectType == "obstacle";
    }

    private string BuildEmergencyMessage(string objectName, AvoidanceAction action, bool isMovingFast)
    {
        string urgency = isMovingFast ? "¡PELIGRO!" : "¡Cuidado!";
        return $"{urgency} {objectName} muy cerca. {action.PrimaryAction}";
    }

    private string BuildCautionMessage(string objectName, AvoidanceAction action, string direction)
    {
        string directionText = GetDetailedDirection(direction);
        return $"Precaución: {objectName} {directionText}. {action.PrimaryAction}";
    }

    private string GetDetailedDirection(string direction)
    {
        switch (direction)
        {
            case "north":
            case "front":
                return "justo al frente";
            case "south":
            case "back":
                return "directamente atrás";
            case "east":
            case "right":
                return "a tu derecha";
            case "west":
            case "left":
                return "a tu izquierda";
            case "northeast":
                return "adelante a la derecha";
            case "northwest":
                return "adelante a la izquierda";
            case "southeast":
                return "atrás a la derecha";
            case "southwest":
                return "atrás a la izquierda";
            default:
                return "cerca de ti";
        }
    }

    // Método original mantenido como respaldo
    private string GetAvoidanceDirection(Objects obj)
    {
        // Lógica simple de evasión basada en la dirección del objeto
        switch (obj.direction.ToLower())
        {
            case "north":
            case "front":
                return "Da 3 pasos atrás";
            case "south":
            case "back":
                return "Da 2 pasos adelante";
            case "east":
            case "right":
                return "Da 2 pasos a la izquierda";
            case "west":
            case "left":
                return "Da 2 pasos a la derecha";
            case "northeast":
                return "Da 2 pasos a la izquierda y atrás";
            case "northwest":
                return "Da 2 pasos a la derecha y atrás";
            case "southeast":
                return "Da 2 pasos a la izquierda";
            case "southwest":
                return "Da 2 pasos a la derecha";
            default:
                return "Detente y evalúa";
        }
    }

    private IEnumerator AnnounceObjects()
    {
        isAudioPlaying = true;

        // Obtener hasta 4 objetos más relevantes
        var relevantObjects = GetMostRelevantObjects(4);

        if (relevantObjects.Count > 0)
        {
            string announcement = BuildAnnouncement(relevantObjects);
            AndroidTTSManager.Instance.Speak(announcement);

            // Esperar el tiempo configurado antes de permitir más anuncios
            yield return new WaitForSeconds(audioDelayBetweenAnnouncements);
        }

        isAudioPlaying = false;
    }

    private List<Objects> GetMostRelevantObjects(int maxCount)
    {
        if (currentDetection?.objects == null) return new List<Objects>();

        // Filtrar y ordenar por relevancia (distancia y tipo de objeto)
        return currentDetection.objects
            .Where(obj => obj.distance <= 15f) // Solo objetos dentro de 15 metros
            .OrderBy(obj => obj.distance) // Más cerca primero
            .ThenByDescending(obj => GetDangerLevel(obj)) // Más peligroso primero
            .Take(maxCount)
            .ToList();
    }

    private string BuildAnnouncement(List<Objects> objects)
    {
        if (objects.Count == 0) return "";

        List<string> descriptions = new List<string>();

        foreach (var obj in objects.Take(2)) // Máximo 2-3 segundos de audio
        {
            string objectName = TranslateObjectName(obj.name);
            string position = GetRelativePosition(obj);
            string distance = GetDistanceDescription(obj.distance);

            descriptions.Add($"{objectName} {position} a {distance}");
        }

        // Si hay más objetos, mencionar solo el más peligroso adicional
        if (objects.Count > 2)
        {
            var mostDangerous = objects.Skip(2).OrderByDescending(GetDangerLevel).First();
            string objectName = TranslateObjectName(mostDangerous.name);
            string position = GetRelativePosition(mostDangerous);
            descriptions.Add($"y {objectName} {position}");
        }

        return string.Join(", ", descriptions);
    }

    private string TranslateObjectName(string className)
    {
        var translations = new Dictionary<string, string>
        {
            {"person", "persona"},
            {"car", "auto"},
            {"truck", "camión"},
            {"bus", "autobús"},
            {"motorcycle", "motocicleta"},
            {"bicycle", "bicicleta"},
            {"dog", "perro"},
            {"cat", "gato"},
            {"pole", "poste"},
            {"tree", "árbol"},
            {"obstacle", "obstáculo"}
        };

        return translations.ContainsKey(className) ? translations[className] : className;
    }

    private string GetRelativePosition(Objects obj)
    {
        switch (obj.direction.ToLower())
        {
            case "north":
            case "front":
                return "al frente";
            case "south":
            case "back":
                return "atrás";
            case "east":
            case "right":
                return "a la derecha";
            case "west":
            case "left":
                return "a la izquierda";
            case "northeast":
                return "adelante a la derecha";
            case "northwest":
                return "adelante a la izquierda";
            case "southeast":
                return "atrás a la derecha";
            case "southwest":
                return "atrás a la izquierda";
            default:
                return "cerca";
        }
    }

    private string GetDistanceDescription(float distance)
    {
        if (distance <= 2f) return "muy cerca";
        else if (distance <= 5f) return "cerca";
        else if (distance <= 10f) return "media distancia";
        else return "lejos";
    }

    private void StopCurrentAudio()
    {
        if (audioCoroutine != null)
        {
            StopCoroutine(audioCoroutine);
            audioCoroutine = null;
        }
        isAudioPlaying = false;
    }
}

// Clase auxiliar para acciones de evasión
public class AvoidanceAction
{
    public string PrimaryAction { get; set; }
    public string SecondaryInfo { get; set; }

    public AvoidanceAction(string primaryAction, string secondaryInfo = "")
    {
        PrimaryAction = primaryAction;
        SecondaryInfo = secondaryInfo;
    }
}
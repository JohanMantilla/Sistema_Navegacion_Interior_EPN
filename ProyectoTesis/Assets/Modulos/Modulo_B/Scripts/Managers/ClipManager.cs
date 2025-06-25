using System.Collections.Generic;
using UnityEngine;

public class ClipManager : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip alertSound;
    private float minRangeAlert = 0.2f;
    private float maxRangeAlert = 6f;
    private string currentAlertObject = null;
    private Dictionary<string, float> dangerLevelObjects = new Dictionary<string, float>() {
        {"car", 5},
        {"bus", 5},
        {"motorcycle", 4},
        {"bench", 3},
        {"bicycle", 3},
        {"cat", 3},
        {"dog", 3},
        {"sports ball", 3},
        {"chair",3},
        {"dining table", 3},
        {"person", 3},
        {"stop sign", 2},
        {"skateboard", 2},
        {"fire hydrant", 2},
        {"umbrella", 2},
        {"backpack", 2},
        {"handbag", 2},
        {"suitcase", 2},
        {"bird", 1},
        {"bottle", 1},
        {"cell phone", 1},
        {"book",1}
    };

    void Start()
    {
        // Asegúrate de que el AudioSource esté configurado
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        WebSocketClient.OnChangeObjectionDetection += GetNearestDangerousObject;
    }

    private void OnDisable()
    {
        WebSocketClient.OnChangeObjectionDetection -= GetNearestDangerousObject;
    }

    private void GetNearestDangerousObject(ObjectDetection data)
    {
        float highestScore = 0f;
        Objects mostDangerousObject = null;

        // Primero encontrar el objeto más peligroso
        foreach (var obj in data.objects)
        {
            if (dangerLevelObjects.ContainsKey(obj.name))
            {
                // Solo considerar objetos dentro del rango
                if (obj.distance >= minRangeAlert && obj.distance <= maxRangeAlert)
                {
                    float score = dangerLevelObjects[obj.name] / obj.distance;
                    if (score > highestScore)
                    {
                        highestScore = score;
                        mostDangerousObject = obj;
                    }
                }
            }
        }

        // Luego reproducir la alerta si es necesario
        PlayAlertToNearestDangerousObject(mostDangerousObject);
    }

    void PlayAlertToNearestDangerousObject(Objects mostDangerousObject)
    {
        if (mostDangerousObject != null)
        {
            // Si es un objeto diferente al actual, reproducir alerta
            if (currentAlertObject != mostDangerousObject.name)
            {
                PlayProximityAlert(mostDangerousObject.name);
            }
        }
        else
        {
            // Si no hay objetos peligrosos, detener la alerta
            if (currentAlertObject != null)
            {
                StopClip();
            }
        }
    }

    // Play con proximidad 
    void PlayProximityAlert(string nearestObject)
    {
        if (alertSound != null && audioSource != null)
        {
            currentAlertObject = nearestObject;
            audioSource.clip = alertSound;
            ChangeVolume(0.1f);
            audioSource.Play();

            Debug.Log($"Reproduciendo alerta para: {nearestObject}");
        }
        else
        {
            Debug.LogWarning("AudioSource o AlertSound no están asignados");
        }
    }

    void ChangeVolume(float volume)
    {
        if (audioSource != null)
            audioSource.volume = volume;
    }

    void StopClip()
    {
        currentAlertObject = null;
        if (audioSource != null)
        {
            audioSource.Stop();
            Debug.Log("Alerta detenida");
        }
    }
}
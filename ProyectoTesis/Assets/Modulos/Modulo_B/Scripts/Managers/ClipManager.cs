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

        foreach (var obj in data.objects)
        {
            if (dangerLevelObjects.ContainsKey(obj.name))
            {
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

        PlayAlertToNearestDangerousObject(mostDangerousObject);
    }

    void PlayAlertToNearestDangerousObject(Objects mostDangerousObject)
    {
        if (mostDangerousObject != null)
        {
            if (currentAlertObject != mostDangerousObject.name)
            {
                PlayProximityAlert(mostDangerousObject.name);
            }
        }
        else
        {
            if (currentAlertObject != null)
            {
                StopClip();
            }
        }
    }

    void PlayProximityAlert(string nearestObject)
    {
        if (alertSound != null && audioSource != null)
        {
            currentAlertObject = nearestObject;
            audioSource.clip = alertSound;
            ChangeVolume(0.1f);
            audioSource.Play();
        }
        else {
            return;
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
        }
    }
}
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    [Header("Configuración de Distancias")]
    [SerializeField] private float distanciaCercana = 3f;    // Distancia para sonido urgente
    [SerializeField] private float distanciaMedia = 5f;      // Distancia para sonido normal
    [SerializeField] private float distanciaLejana = 8f;     // Distancia para sonido suave

    [Header("Clips de Audio por Clase")]
    [SerializeField] private AudioClip personSound;
    [SerializeField] private AudioClip carSound;
    [SerializeField] private AudioClip bicycleSound;
    [SerializeField] private AudioClip dogSound;
    [SerializeField] private AudioClip catSound;
    [SerializeField] private AudioClip defaultSound;

    [Header("Configuración")]
    [SerializeField] private float cooldownTiempo = 1f; // Tiempo entre sonidos del mismo objeto
    [SerializeField] private AudioSource audioSource;

    // Control de cooldown para evitar spam de sonidos
    private Dictionary<string, float> ultimoSonidoPorClase = new Dictionary<string, float>();

    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public void PlayAudioForObject(Objects obj)
    {
        if (obj == null || string.IsNullOrEmpty(obj.name)) return;

        // Verificar cooldown
        string key = obj.name + "_" + obj.object_id;
        if (ultimoSonidoPorClase.ContainsKey(key) &&
            Time.time - ultimoSonidoPorClase[key] < cooldownTiempo)
        {
            return;
        }

        // Obtener clip de audio según la clase
        AudioClip clip = GetAudioClipForClass(obj.name);
        if (clip == null) return;

        // Calcular volumen según distancia
        float volume = CalculateVolumeByDistance(obj.distance);

        // Reproducir sonido
        audioSource.PlayOneShot(clip, volume);

        // Actualizar cooldown
        ultimoSonidoPorClase[key] = Time.time;

        // Log para debug
        Debug.Log($"Reproduciendo sonido para {obj.name} a {obj.distance:F1}m con volumen {volume:F2}");
    }

    private AudioClip GetAudioClipForClass(string className)
    {
        switch (className.ToLower())
        {
            case "person": return personSound;
            case "car": return carSound;
            case "bicycle": return bicycleSound;
            case "dog": return dogSound;
            case "cat": return catSound;
            default: return defaultSound;
        }
    }

    private float CalculateVolumeByDistance(float distance)
    {
        if (distance <= distanciaCercana)
            return 1f; // Volumen máximo para objetos muy cercanos
        else if (distance <= distanciaMedia)
            return 0.7f; // Volumen medio
        else if (distance <= distanciaLejana)
            return 0.4f; // Volumen bajo
        else
            return 0f; // No reproducir sonido si está muy lejos
    }

    // Método para instrucciones de navegación (TTS)
    public void SpeakInstruction(string instruction)
    {
        AndroidTTSManager.Instance.Speak(instruction);
        Debug.Log($"TTS: {instruction}");
    }   

    // Método para verificar si debe reproducir sonido según distancia
    public bool ShouldPlaySound(float distance)
    {
        return distance <= distanciaLejana;
    }

    // Método legacy para compatibilidad
    public void PlayAudio()
    {
        if (defaultSound != null)
            audioSource.PlayOneShot(defaultSound);
    }
}
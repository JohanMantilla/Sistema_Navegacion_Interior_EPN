using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class WelcomeUI : MonoBehaviour
{
    [SerializeField] private Button btnSettingWelcome;
    [SerializeField] private TextMeshProUGUI txtSettingWelcome;
    [SerializeField] private string message = "Bienvenidos. Poliubicate. Somos una aplicación accesible que permite a usuarios con deficiencia visual moderada navegar dentro de la Escuela Politecnica Nacional. A continuación proporcionanos permisos y establece configuraciones a tu gusto.";
    private void Awake()
    {
        InitializeElementsUI();
    }
    void Start()
    {
        // Limpiar cualquier texto pendiente del TTS antes de empezar
        if (AndroidTTSManager.Instance != null && AndroidTTSManager.Instance.isInitialize)
        {
            AndroidTTSManager.Instance.Stop(); // Detener y limpiar cola
        }
        StartCoroutine(WaitTTS());
    }

    IEnumerator WaitTTS()
    {
        float timeout = 10f; // Aumentar tiempo de espera
        float elapsed = 0f;

        // Esperar a que AndroidTTSManager esté disponible
        while (AndroidTTSManager.Instance == null && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (AndroidTTSManager.Instance == null)
        {
            Debug.LogWarning("AndroidTTSManager no está disponible");
            yield break;
        }

        // Ahora esperar a que TTS se inicialice
        elapsed = 0f;
        while (!AndroidTTSManager.Instance.isInitialize && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (AndroidTTSManager.Instance.isInitialize && SceneManager.GetActiveScene().name == "WelcomeUI")
        {
            AndroidTTSManager.Instance.Speak(message);
        }
        else
        {
            Debug.LogWarning("No se pudo inicializar TTS en el tiempo esperado");
        }
    }

    void InitializeElementsUI()
    {
        AddListeners();
    }


    void AddListeners()
    {
        if (btnSettingWelcome != null)
        {
            btnSettingWelcome.onClick.AddListener(()=> UIManager.Instance.LoadScene("SettingsUI"));
        }
    }


    private void OnDestroy()
    {
        // Detener TTS si esta UI se destruye
        if (AndroidTTSManager.Instance != null && AndroidTTSManager.Instance.isInitialize)
        {
            AndroidTTSManager.Instance.Stop();
        }
    }

}
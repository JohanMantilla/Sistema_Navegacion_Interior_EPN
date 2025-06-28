using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class NavigationUI : MonoBehaviour
{
    private Button btnSettingNavigationUI;
    private Button btnCameraNavigationUI;
    private Button btnRouteNavigationUI;
    private TextMeshProUGUI txtLocation;

    public GameObject scrollViewEPNLocations;
    private Button btnArrivalLocation;
    private TextMeshProUGUI txtArrivalLocation;
    private TextMeshProUGUI newLocation;

    [SerializeField] private GameObject pnlConfirmationDialog;
    // Variable estática para controlar si ya se reprodujo el mensaje

    private static bool welcomeMessagePlayed = false;
    private string message = "Bienvenido a la pantalla de navegación. Aquí puedes explorar las rutas disponibles dentro de la Escuela Politécnica Nacional.";

    void Awake()
    {
        InitializeUIElements();
    }
    private void OnEnable()
    {
        JsonDataManager.OnJsonRouteUpdated += UpdateUI;
        ItemLocation.OnLocationChanged += UpdateArrivalLocation;
        //ItemLocation.OnSelectLocation += UpdatedUILabel;
    }
    private void OnDisable()
    {
        JsonDataManager.OnJsonRouteUpdated -= UpdateUI;
        ItemLocation.OnLocationChanged -= UpdateArrivalLocation;
        //ItemLocation.OnSelectLocation -= UpdatedUILabel;
    }
    private void Start()
    {
        // Limpiar cualquier texto pendiente del TTS antes de empezar
        if (AndroidTTSManager.Instance != null && AndroidTTSManager.Instance.isInitialize)
        {
            AndroidTTSManager.Instance.Stop(); // Detener y limpiar cola
        }
        
        StartCoroutine(WaitTTS());
    }

    IEnumerator WaitTTS() {
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

        if (AndroidTTSManager.Instance.isInitialize && SceneManager.GetActiveScene().name == "NavigationUI")
        {
            AndroidTTSManager.Instance.Speak(message);
            // Marcar que el mensaje ya se reprodujo
            welcomeMessagePlayed = true;

        }
        else
        {
            Debug.LogWarning("No se pudo inicializar TTS en el tiempo esperado");
        }
    }

    private void InitializeUIElements()
    {
        if (btnSettingNavigationUI == null)
            btnSettingNavigationUI = GameObject.Find("btnSettingNavigationUI")?.GetComponentInChildren<Button>();

        if (btnCameraNavigationUI == null)
            btnCameraNavigationUI = GameObject.Find("btnCameraNavigationUI")?.GetComponent<Button>();

        if (txtLocation == null)
            txtLocation = GameObject.Find("Location")?.GetComponent<TextMeshProUGUI>();
        
        if (txtArrivalLocation == null)
        {
            txtArrivalLocation = GameObject.Find("txtArrivalLocation")?.GetComponent<TextMeshProUGUI>();
        }
        
        if (btnArrivalLocation == null)
        {
            btnArrivalLocation = GameObject.Find("btnArrivalLocation")?.GetComponent<Button>();
        }
        if (scrollViewEPNLocations == null)
        {
            scrollViewEPNLocations = GameObject.Find("EPNLocations");
        }

        if (newLocation == null) {
            newLocation = GameObject.Find("newLocation")?.GetComponent<TextMeshProUGUI>();
        }

        AddListeners();
    }
    private void AddListeners()
    {
        if (btnSettingNavigationUI != null)
        {
            btnSettingNavigationUI.onClick.RemoveAllListeners();
            btnSettingNavigationUI.onClick.AddListener(() => UIManager.Instance.LoadScene("SettingsUI"));
        }
        if (btnCameraNavigationUI != null)
        {
            btnCameraNavigationUI.onClick.RemoveAllListeners();
            btnCameraNavigationUI.onClick.AddListener(() => {
                if (AndroidTTSManager.Instance != null && AndroidTTSManager.Instance.isInitialize && welcomeMessagePlayed)
                {
                    AndroidTTSManager.Instance.Speak("Camará");
                    Invoke(nameof(LoadScene), 2.9f);
                }
                else {
                    LoadScene();
                }
               
            });
        }
        if (btnArrivalLocation != null)
        {
            btnArrivalLocation.onClick.RemoveAllListeners();
            btnArrivalLocation.onClick.AddListener(ShowOrHiddenLocations);
        }
    }

    void LoadScene() {
        if (AndroidTTSManager.Instance != null)
        {
            AndroidTTSManager.Instance.Stop();
        }
        UIManager.Instance.LoadScene("AR");
    }

    void UpdateUI(Route routeData)
    {
        InitializeUIElements();
        if (txtLocation != null && routeData != null)
        {
            txtLocation.text = routeData.route.total_steps.ToString();
        }
    }

    void UpdatedUILabel(Location location)
    {
        InitializeUIElements();
        if (newLocation != null && location != null ) {
            newLocation.text = location.nombre;
        }
    }
    void UpdateArrivalLocation(string nameLocation)
    {
        InitializeUIElements();
        if (txtArrivalLocation != null && nameLocation != "")
        {
            txtArrivalLocation.text = nameLocation;
        }
    }
    void ShowOrHiddenLocations()
    {
        if (scrollViewEPNLocations != null)
        {
            if (AndroidTTSManager.Instance != null && AndroidTTSManager.Instance.isInitialize && welcomeMessagePlayed && scrollViewEPNLocations.activeSelf) {
                AndroidTTSManager.Instance.Speak("Ubicación de destino activada");
            }
            scrollViewEPNLocations.SetActive(!scrollViewEPNLocations.activeSelf);
        }
    }

    // Método opcional para resetear el estado del mensaje (por ejemplo, cuando se reinicia la aplicación)
    public static void ResetWelcomeMessage()
    {
        welcomeMessagePlayed = false;
    }

}

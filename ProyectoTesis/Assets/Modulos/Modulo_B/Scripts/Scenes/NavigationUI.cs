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

    [SerializeField] private GameObject scrollViewEPNLocations;
    private Button btnArrivalLocation;
    private TextMeshProUGUI txtArrivalLocation;
    private TextMeshProUGUI newLocation;

    [SerializeField] private GameObject pnlConfirmationDialog;

    // SOLUCI�N M�VIL: Mantener referencia del padre donde buscar
    [SerializeField] private Transform parentContainer; // Asignar en el Inspector el Canvas o Panel padre

    private static bool welcomeMessagePlayed = false;
    private string message = "Bienvenido a la pantalla de navegaci�n. Aqu� puedes explorar las rutas disponibles dentro de la Escuela Polit�cnica Nacional.";

    //GPS
    // 1. Agrega esta variable al principio de tu clase NavigationUI:
    private bool gpsReady = false;

    void Awake()
    {
        InitializeUIElements();
        if (scrollViewEPNLocations != null)
        {
            scrollViewEPNLocations.SetActive(false);
        }
    }

    private void OnEnable()
    {
        JsonDataManager.OnJsonRouteUpdated += UpdateUI;
        ItemLocation.OnLocationChanged += UpdateArrivalLocation;
        // NUEVA L�NEA: Suscribirse al evento GPS
        SimpleGPSManager.OnGPSReady += OnGPSReady;
    }

    private void OnDisable()
    {
        JsonDataManager.OnJsonRouteUpdated -= UpdateUI;
        ItemLocation.OnLocationChanged -= UpdateArrivalLocation;
        // NUEVA L�NEA: Suscribirse al evento GPS
        SimpleGPSManager.OnGPSReady -= OnGPSReady;
    }

    // 4. Agrega este nuevo m�todo:
    private void OnGPSReady()
    {
        gpsReady = true;

        // Si TTS est� listo Y GPS est� listo, reproducir mensaje
        if (AndroidTTSManager.Instance != null && AndroidTTSManager.Instance.isInitialize &&
            SceneManager.GetActiveScene().name == "NavigationUI" && !welcomeMessagePlayed)
        {
            AndroidTTSManager.Instance.Speak(message);
            welcomeMessagePlayed = true;
        }
    }

    private void Start()
    {
        // Limpiar cualquier texto pendiente del TTS antes de empezar
        if (AndroidTTSManager.Instance != null && AndroidTTSManager.Instance.isInitialize)
        {
            AndroidTTSManager.Instance.Stop();
        }

        StartCoroutine(WaitTTS());
    }

    IEnumerator WaitTTS()
    {
        float timeout = 10f;
        float elapsed = 0f;

        while (AndroidTTSManager.Instance == null && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (AndroidTTSManager.Instance == null)
        {
            Debug.LogWarning("AndroidTTSManager no est� disponible");
            yield break;
        }

        elapsed = 0f;
        while (!AndroidTTSManager.Instance.isInitialize && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (AndroidTTSManager.Instance.isInitialize && SceneManager.GetActiveScene().name == "NavigationUI" && gpsReady && !welcomeMessagePlayed)
        {
            AndroidTTSManager.Instance.Speak(message);
            welcomeMessagePlayed = true;
        }
        else
        {
            Debug.LogWarning("No se pudo inicializar TTS en el tiempo esperado Y GPS");
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

        // SOLUCI�N M�VIL: M�todo que S� funciona en dispositivos
        if (scrollViewEPNLocations == null)
        {
            // M�todo 1: Buscar en el contenedor padre espec�fico
            if (parentContainer != null)
            {
                scrollViewEPNLocations = FindChildByName(parentContainer, "EPNLocations");
            }

            // M�todo 2: Buscar en todos los Canvas activos
            if (scrollViewEPNLocations == null)
            {
                Canvas[] canvases = FindObjectsOfType<Canvas>();
                foreach (Canvas canvas in canvases)
                {
                    scrollViewEPNLocations = FindChildByName(canvas.transform, "EPNLocations");
                    if (scrollViewEPNLocations != null) break;
                }
            }

            // M�todo 3: Buscar por componente espec�fico (si EPNLocations tiene un componente �nico)
            if (scrollViewEPNLocations == null)
            {
                // Ejemplo: si EPNLocations tiene un ScrollRect
                ScrollRect scrollRect = FindObjectOfType<ScrollRect>();
                if (scrollRect != null && scrollRect.name == "EPNLocations")
                {
                    scrollViewEPNLocations = scrollRect.gameObject;
                }
            }
        }

        if (newLocation == null)
        {
            newLocation = GameObject.Find("newLocation")?.GetComponent<TextMeshProUGUI>();
        }

        AddListeners();
    }

    // M�TODO AUXILIAR: Buscar recursivamente en hijos (funciona en m�viles)
    private GameObject FindChildByName(Transform parent, string name)
    {
        if (parent == null) return null;

        // Buscar en hijos directos
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == name)
            {
                return child.gameObject;
            }

            // Buscar recursivamente en nietos
            GameObject found = FindChildByName(child, name);
            if (found != null) return found;
        }

        return null;
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
                    AndroidTTSManager.Instance.Speak("Camar�");
                    Invoke(nameof(LoadScene), 2.9f);
                }
                else
                {
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

    void LoadScene()
    {
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
        if (newLocation != null && location != null)
        {
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
        // SOLUCI�N M�VIL: Re-inicializar si la referencia se perdi�
        if (scrollViewEPNLocations == null)
        {
            Debug.LogWarning("scrollViewEPNLocations es null, re-inicializando...");
            InitializeUIElements();
        }

        if (scrollViewEPNLocations != null)
        {
            bool currentState = scrollViewEPNLocations.activeSelf;
            bool newState = !currentState; // El estado que tendr� DESPU�S del cambio

            // CORRECCI�N: Reproducir TTS basado en el NUEVO estado que tendr�
            if (AndroidTTSManager.Instance != null && AndroidTTSManager.Instance.isInitialize && welcomeMessagePlayed)
            {
                if (newState) // Si se va a ACTIVAR
                {
                    AndroidTTSManager.Instance.Speak("Lista de ubicaciones activada");
                }
                else // Si se va a DESACTIVAR
                {
                    AndroidTTSManager.Instance.Speak("Lista de ubicaciones desactivada");
                }
            }

            scrollViewEPNLocations.SetActive(newState);

            // Debug para verificar en dispositivo
            Debug.Log($"[MOBILE] scrollViewEPNLocations cambi� de {currentState} a {scrollViewEPNLocations.activeSelf}");
        }
        else
        {
            Debug.LogError("[MOBILE] No se pudo encontrar scrollViewEPNLocations en dispositivo");
        }
    }


    public static void ResetWelcomeMessage()
    {
        welcomeMessagePlayed = false;
    }
}
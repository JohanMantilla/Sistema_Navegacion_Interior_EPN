using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private static UIManager instance;

    [Header("Elementos UI")]
    [SerializeField] private Button btnSettingWelcome;
    [SerializeField] private Button btnNextSettingsUI;
    [SerializeField] private Button btnSettingNavigationUI;
    [SerializeField] private Button btnCameraNavigationUI;
    [SerializeField] private Button btnRouteAR;

    public static UIManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<UIManager>();
                if (instance == null)
                {
                    GameObject instanceObject = new GameObject("UIManager");
                    instance = instanceObject.AddComponent<UIManager>();
                }
                DontDestroyOnLoad(instance.gameObject);
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        LoadInitialScene();
        //LoadCurrentSceneUI();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnCurrentScene;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnCurrentScene;
    }

    private void OnCurrentScene(Scene scene, LoadSceneMode mode)
    {
        LoadCurrentSceneUI();
    }

    private void LoadCurrentSceneUI()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        switch (currentSceneName)
        {
            case "WelcomeUI":
                LoadWelcomeUI();
                break;
            case "SettingsUI":
                LoadSettingsUI();
                break;
            case "NavigationUI":
                LoadNavigationUI();
                break;
            case "AR":
                LoadAR();
                break;
        }
    }

    private void LoadWelcomeUI()
    {
        btnSettingWelcome = GameObject.Find("btnSettingWelcome")?.GetComponent<Button>();

        if (btnSettingWelcome != null)
        {
            btnSettingWelcome.onClick.AddListener(() => LoadScene("SettingsUI"));
        }
    }

    private void LoadSettingsUI()
    {
        btnNextSettingsUI = GameObject.Find("btnNextSettingsUI")?.GetComponent<Button>();

        if (btnNextSettingsUI != null)
        {
            btnNextSettingsUI.onClick.AddListener(() => LoadScene("NavigationUI"));
        }
    }

    private void LoadNavigationUI()
    {
        btnSettingNavigationUI = GameObject.Find("btnSettingNavigationUI")?.GetComponent<Button>();
        btnCameraNavigationUI = GameObject.Find("btnCameraNavigationUI")?.GetComponent<Button>();
        if (btnSettingNavigationUI != null)
        {
            btnSettingNavigationUI.onClick.AddListener(() => LoadScene("SettingsUI"));
            btnCameraNavigationUI.onClick.AddListener(()=>LoadScene("AR"));
        }
    }

    private void LoadAR() {
        btnRouteAR = GameObject.Find("btnRouteAR")?.GetComponent<Button>();
        if (btnRouteAR != null) {
            btnRouteAR.onClick.AddListener(()=>LoadScene("NavigationUI"));
        }
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    private void Update()
    {

    }

    public void LoadInitialScene() {
        if (!PlayerPrefs.HasKey("isFirstAppExecution")) {
            PlayerPrefs.SetInt("isFirstAppExecution", 1);
            PlayerPrefs.Save();
            LoadScene("WelcomeUI");
        }
        else {
            LoadScene("NavigationUI");
        }
    }


}

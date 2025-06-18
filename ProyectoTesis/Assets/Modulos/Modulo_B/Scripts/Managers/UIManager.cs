using System;
using TMPro;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class UIManager : MonoBehaviour
{
    private static UIManager instance;
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
    }
    private void OnEnable()
    {
        //JsonDataManager.OnJsonRouteUpdated += UpdateUI;
        //GPSManager.currentUserLocation += mostrarMensaje;
    }
    private void OnDisable()
    {
        //JsonDataManager.OnJsonRouteUpdated -= UpdateUI;
        //GPSManager.currentUserLocation -= mostrarMensaje;
    }
    public void LoadInitialScene()
    {
        if (!PlayerPrefs.HasKey("isFirstAppExecution"))
        {
            PlayerPrefs.SetInt("isFirstAppExecution", 1);
            PlayerPrefs.Save();
            LoadScene("WelcomeUI");
        }
        else
        {
            LoadScene("NavigationUI");
        }
        
    }
    private void LoadWelcomeUI()
    {
        SceneManager.LoadScene("WelcomeUI");
    }
    private void LoadSettingsUI()
    {
        SceneManager.LoadScene("SettingsUI");
    }
    public void LoadNavigationUI()
    {
        SceneManager.LoadScene("NavigationUI");
    }
    private void LoadAR()
    {
        SceneManager.LoadScene("AR");
    }
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
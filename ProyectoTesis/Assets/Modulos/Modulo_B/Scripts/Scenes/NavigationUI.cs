using System.Collections;
using TMPro;
using UnityEngine;
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
    void Awake()
    {
        InitializeUIElements();
    }
    private void OnEnable()
    {
        JsonDataManager.OnJsonRouteUpdated += UpdateUI;
        ItemLocation.OnLocationChanged += UpdateArrivalLocation;
    }
    private void OnDisable()
    {
        JsonDataManager.OnJsonRouteUpdated -= UpdateUI;
        ItemLocation.OnLocationChanged -= UpdateArrivalLocation;
    }
    private void Start()
    {
        
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
            btnCameraNavigationUI.onClick.AddListener(() => UIManager.Instance.LoadScene("AR"));
        }
        if (btnArrivalLocation != null)
        {
            btnArrivalLocation.onClick.RemoveAllListeners();
            btnArrivalLocation.onClick.AddListener(ShowOrHiddenLocations);
        }
    }
    void UpdateUI(Route routeData)
    {
        InitializeUIElements();
        if (txtLocation != null && routeData != null)
        {
            txtLocation.text = routeData.route.total_steps.ToString();
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
            scrollViewEPNLocations.SetActive(!scrollViewEPNLocations.activeSelf);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

public class WelcomeUI : MonoBehaviour
{
    private Button btnSettingWelcome;
    private void Awake()
    {
        InitializeElementsUI();
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void InitializeElementsUI()
    {
        if (btnSettingWelcome == null) {
            btnSettingWelcome = GameObject.Find("btnSettingWelcome")?.GetComponent<Button>();
        }

        AddListeners();
    }

    void AddListeners() {
        if (btnSettingWelcome != null) {
            btnSettingWelcome.onClick.AddListener(()=>UIManager.Instance.LoadScene("SettingsUI"));
        }
    }
}

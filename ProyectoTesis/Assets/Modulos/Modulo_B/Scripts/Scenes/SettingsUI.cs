using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    private Button btnNextSettingsUI;

    void Awake()
    {
        InitializeUIElements();
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void InitializeUIElements() {
        if (btnNextSettingsUI == null) {
            btnNextSettingsUI = GameObject.Find("btnNextSettingsUI")?.GetComponent<Button>();
        }

        AddListeners();
    }

    void AddListeners() {
        if (btnNextSettingsUI != null) { 
            btnNextSettingsUI.onClick.RemoveAllListeners();
            btnNextSettingsUI.onClick.AddListener(()=>UIManager.Instance.LoadScene("NavigationUI"));
        }

    }


}

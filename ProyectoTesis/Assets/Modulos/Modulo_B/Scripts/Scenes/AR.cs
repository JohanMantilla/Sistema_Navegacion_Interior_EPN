using UnityEngine;
using UnityEngine.UI;

public class AR : MonoBehaviour
{
    private Button btnRoute;
    private Button btnCameraAR;

    void Awake()
    {
        InitializeUIElements();
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    void InitializeUIElements() {
        if (btnRoute == null) {
            btnRoute = GameObject.Find("btnRoute")?.GetComponent<Button>();
        }

        if (btnCameraAR == null)
        {
            btnCameraAR = GameObject.Find("btnCameraAR")?.GetComponent<Button>();
        }

        AddListeners();
    }

    void AddListeners() {
        if (btnRoute != null) {
            btnRoute.onClick.RemoveAllListeners();
            btnRoute.onClick.AddListener(()=>UIManager.Instance.LoadScene("NavigationUI"));
        }

    }

}

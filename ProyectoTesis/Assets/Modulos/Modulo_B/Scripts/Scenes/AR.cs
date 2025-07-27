using UnityEngine;
using UnityEngine.UI;

public class AR : MonoBehaviour
{
    private Button btnRoute;
    [SerializeField] private Button btnNavigation;

    void Awake()
    {
        InitializeUIElements();
    }

    void Start()
    {
        //InitializeUIElements();
    }

    void InitializeUIElements() {
        
        AddListeners();
    }
    
    void AddListeners() {
        if(btnNavigation != null)
        {
            btnNavigation.onClick.RemoveAllListeners();
            btnNavigation.onClick.AddListener(() => UIManager.Instance.LoadScene("NavigationUI"));
        }
    }

}

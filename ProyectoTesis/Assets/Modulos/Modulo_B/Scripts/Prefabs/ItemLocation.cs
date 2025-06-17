using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemLocation : MonoBehaviour
{
    public Button btnLocation;
    public TextMeshProUGUI txtLocationAttribute;
    public Image icon;
    public static event Action<string> OnLocationChanged;

    public GameObject pnlDialog;
    public TextMeshProUGUI txtNameLocation;
    public Button btnCheck;
    public Button btnCancel;

    void Awake()
    {
        //initializeElementsDialog();
    }

    void Start()
    {
        pnlDialog.SetActive(false);
    }

    public void SetElement(Location locationItemModel)
    {
        txtLocationAttribute.text = locationItemModel.nombre;
        btnLocation.onClick.AddListener(() => SelectedItem(locationItemModel));
    }

    public void SelectedItem(Location locationItemModel)
    {
        
        showConfirmationDialog();
        txtNameLocation.text = locationItemModel.nombre;
        // 1.Condicional de acepta se envia el disparador, si no no. 
        if (btnCheck != null)
        {
            btnCheck.onClick.RemoveAllListeners();
            btnCheck.onClick.AddListener(() => OnClickItem(locationItemModel));
            //pnlDialog.SetActive(false); 
        }

        if (btnCancel != null)
        {
            btnCancel.onClick.RemoveAllListeners();
            btnCancel.onClick.AddListener(() => pnlDialog.SetActive(false));
            //pnlDialog.SetActive(false); 
        }

        //OnLocationChanged?.Invoke(locationItemModel.nombre); 
    }

    void OnClickItem(Location locationItemModel)
    {
        OnLocationChanged?.Invoke(locationItemModel.nombre);
        pnlDialog.SetActive(false);
    }

    void showConfirmationDialog()
    {
        pnlDialog.SetActive(true);
    }

    void initializeElementsDialog()
    {

        if (pnlDialog == null)
        {
            pnlDialog = GameObject.Find("PnlConfirmationDialog");
        }

        if (txtNameLocation == null)
        {
            txtNameLocation = GameObject.Find("TxtNameLocation")?.GetComponent<TextMeshProUGUI>();
        }
        if (btnCheck == null)
        {
            btnCheck = GameObject.Find("check")?.GetComponent<Button>();
        }

        if (btnCancel == null)
        {
            btnCancel = GameObject.Find("cancel")?.GetComponent<Button>();
        }


    }


    bool ValidateClick(bool isClicked)
    {
        return isClicked;
    }


}
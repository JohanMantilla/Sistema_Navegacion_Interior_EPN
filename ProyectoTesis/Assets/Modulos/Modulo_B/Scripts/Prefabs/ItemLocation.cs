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
        
        if (btnCheck != null)
        {
            btnCheck.onClick.RemoveAllListeners();
            btnCheck.onClick.AddListener(() => OnClickItem(locationItemModel));
        }

        if (btnCancel != null)
        {
            btnCancel.onClick.RemoveAllListeners();
            btnCancel.onClick.AddListener(() => hiddenConfirmationDialog());
        }

        //OnLocationChanged?.Invoke(locationItemModel.nombre); 
    }

    void OnClickItem(Location locationItemModel)
    {
        Debug.Log("Entro a OnClicKItem : " + locationItemModel.nombre);
        OnLocationChanged?.Invoke(locationItemModel.nombre);
        hiddenConfirmationDialog();
    }

    void showConfirmationDialog()
    {
        pnlDialog.SetActive(true);
    }
    private void hiddenConfirmationDialog()
    {
        pnlDialog.SetActive(false);
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


}
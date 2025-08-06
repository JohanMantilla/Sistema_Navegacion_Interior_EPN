using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
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

    public static event Action<Location> OnSelectLocation;
    public static event Action<float,float> OnChangeEndPosition;
    private const string PERSISTENT_LOCATION = "Persistence_Location"; 

    void Awake()
    {
        //initializeElementsDialog();
    }

    void Start()
    {
        pnlDialog.SetActive(false);
        StartCoroutine(WaitTTS());
    }

    IEnumerator WaitTTS()
    {
        float timeout = 10f; // Aumentar tiempo de espera
        float elapsed = 0f;

        // Esperar a que AndroidTTSManager esté disponible
        while (AndroidTTSManager.Instance == null && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (AndroidTTSManager.Instance == null)
        {
            Debug.LogWarning("AndroidTTSManager no está disponible");
            yield break;
        }

        // Ahora esperar a que TTS se inicialice
        elapsed = 0f;
        while (!AndroidTTSManager.Instance.isInitialize && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

    }

    public void SetElement(Location locationItemModel)
    {
        txtLocationAttribute.text = locationItemModel.nombre;
        btnLocation.onClick.AddListener(() => SelectedItem(locationItemModel));
    }

    public void SelectedItem(Location locationItemModel)
    {
        if (AndroidTTSManager.Instance != null && AndroidTTSManager.Instance.isInitialize)
        {
            AndroidTTSManager.Instance.Speak("Has seleccionado: " + locationItemModel.nombre);
        }
        Invoke(nameof(showConfirmationDialog), 0.5f);
        txtNameLocation.text = locationItemModel.nombre;

        if (btnCheck != null)
        {
            btnCheck.onClick.RemoveAllListeners();
            btnCheck.onClick.AddListener(() => OnConfirmationClick(locationItemModel));
        }

        if (btnCancel != null)
        {
            btnCancel.onClick.RemoveAllListeners();
            btnCancel.onClick.AddListener(() => OnCancelled());
        }

        //OnLocationChanged?.Invoke(locationItemModel.nombre); 
    }

    void OnConfirmationClick(Location locationItemModel)
    {
        Debug.Log("Entro a OnClicKItem : " + locationItemModel.nombre);
        if (AndroidTTSManager.Instance != null && AndroidTTSManager.Instance.isInitialize) {
            AndroidTTSManager.Instance.Speak("Confirmado");
        }
        OnLocationChanged?.Invoke(locationItemModel.nombre);
        // aca envio
        OnSelectLocation?.Invoke(locationItemModel);
        // aca envio a Erika
        OnChangeEndPosition?.Invoke(locationItemModel.longitude,locationItemModel.latitude);

        Invoke(nameof(hiddenConfirmationDialog), 0.5f);
    }


    void showConfirmationDialog()
    {
        pnlDialog.SetActive(true);
    }
    private void hiddenConfirmationDialog()
    {
        pnlDialog.SetActive(false);
    }

    private void OnCancelled() {
        if (AndroidTTSManager.Instance != null && AndroidTTSManager.Instance.isInitialize)
        {
            AndroidTTSManager.Instance.Speak("Cancelado");
        }
        Invoke(nameof(hiddenConfirmationDialog), 0.5f);
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
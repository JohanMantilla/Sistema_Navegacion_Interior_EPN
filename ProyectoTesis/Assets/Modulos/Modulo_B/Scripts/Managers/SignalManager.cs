using UnityEngine;

public class SignalManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
  

    void Start()
    {
        SettingsUI.onVisualActive += imprimir;

    }

    // Update is called once per frame
    void Update()
    {

    }

    void imprimir(bool isEnabled)
    {
        Debug.Log("Toggle state: " + isEnabled);
    }


}

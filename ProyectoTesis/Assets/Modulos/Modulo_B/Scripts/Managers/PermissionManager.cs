using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Android;
using System;

public class PermissionManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button btnCameraPermission;
    [SerializeField] private Button btnLocationPermission;

    [Header("Sprites")]
    [SerializeField] private Sprite spriteBarOn;
    [SerializeField] private Sprite spriteBarOff;

    [Header("Button Image Property")]
    [SerializeField] private Image btnCamaraPermissionImage;
    [SerializeField] private Image btnLocationPermissionImage;

    private bool cameraPermissionActivated = false;
    private bool locationPermissionActivated = false;


    void Start()
    {
        loadUserPermissionPrefs();
        changeSpritePermissionBarOnClick();
        btnCameraPermission.onClick.AddListener(updatedCameraPermissionOnClick);
        btnLocationPermission.onClick.AddListener(updatedLocationPermissionOnClick);

    }

    void Update()
    {

    }

    void changeSpritePermissionBarOnClick()
    {
        btnCamaraPermissionImage.sprite = cameraPermissionActivated ? spriteBarOn : spriteBarOff;
        btnLocationPermissionImage.sprite = locationPermissionActivated ? spriteBarOn :spriteBarOff;
    }
    
    //modificar un poco
    void updatedCameraPermissionOnClick()
    {
        /*
        cameraPermissionActivated = !cameraPermissionActivated;
        savePermissionStates();
        changeSpritePermissionBarOnClick();
        */

        if (!cameraPermissionActivated)
        {
            // Verificar si ya tiene el permiso
            if (Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                cameraPermissionActivated = true;
                savePermissionStates();
                changeSpritePermissionBarOnClick();
            }
            else
            {
                // Pedir permiso y usar callback
                var callbacks = new PermissionCallbacks();
                callbacks.PermissionGranted += OnCameraPermissionGranted;
                callbacks.PermissionDenied += OnCameraPermissionDenied;

                Permission.RequestUserPermission(Permission.Camera, callbacks);
            }
        }
        else
        {
            cameraPermissionActivated = false;
            savePermissionStates();
            changeSpritePermissionBarOnClick();
        }
    }
    //Cambiar nombres, sintaxis es la misma que use
    void OnCameraPermissionGranted(string permissionName)
    {
        cameraPermissionActivated = true;
        savePermissionStates();
        changeSpritePermissionBarOnClick();
    }

    void OnCameraPermissionDenied(string permissionName)
    {
        cameraPermissionActivated = false;
        savePermissionStates();
        changeSpritePermissionBarOnClick();
    }

    //modificar un poco
    void updatedLocationPermissionOnClick()
    {
        if (!locationPermissionActivated)
        {
            // Verificar si ya tiene el permiso
            if (Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                locationPermissionActivated = true;
                savePermissionStates();
                changeSpritePermissionBarOnClick();
            }
            else
            {
                // Pedir permiso y usar callback
                var callbacks = new PermissionCallbacks();
                callbacks.PermissionGranted += OnLocationPermissionGranted;
                callbacks.PermissionDenied += OnLocationPermissionDenied;

                Permission.RequestUserPermission(Permission.FineLocation, callbacks);
            }
        }
        else
        {
            locationPermissionActivated = false;
            savePermissionStates();
            changeSpritePermissionBarOnClick();
        }
    }
    //Cambiar nombres, sintaxis es la misma que use
    void OnLocationPermissionGranted(string permissionName)
    {
        locationPermissionActivated = true;
        savePermissionStates();
        changeSpritePermissionBarOnClick();
    }

    void OnLocationPermissionDenied(string permissionName)
    {
        locationPermissionActivated = false;
        savePermissionStates();
        changeSpritePermissionBarOnClick();
    }




    void savePermissionStates()
    {
        PlayerPrefs.SetInt("CameraPermission", cameraPermissionActivated ? 1 : 0);
        PlayerPrefs.SetInt("LocationPermission", locationPermissionActivated ? 1 : 0);
        PlayerPrefs.Save();
    }

    void loadUserPermissionPrefs()
    {
        /*
        cameraPermissionActivated = PlayerPrefs.GetInt("CameraPermission", 0) == 1;
        locationPermissionActivated = PlayerPrefs.GetInt("LocationPermission", 0) == 1;
        */

        // Para cámara: verificar tanto las preferencias guardadas como el permiso real
        bool savedCameraPermission = PlayerPrefs.GetInt("CameraPermission", 0) == 1;
        bool hasRealCameraPermission = Permission.HasUserAuthorizedPermission(Permission.Camera);
        cameraPermissionActivated = savedCameraPermission && hasRealCameraPermission;

        // Para ubicación: mantener solo las preferencias (sin permiso real)
        locationPermissionActivated = PlayerPrefs.GetInt("LocationPermission", 0) == 1;
    }

    public bool isCameraPermissionActivated() { return cameraPermissionActivated; }

    public bool isLocationPermissionActivated() { return locationPermissionActivated; }




}

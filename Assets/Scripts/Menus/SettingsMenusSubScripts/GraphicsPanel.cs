using System;
using System.Collections.Generic;
using Mirror;
using Mirror.Examples.Common.Controllers.Player;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityServiceLocator;

public class GraphicsPanel : MonoBehaviour
{
    //to modify the values on the graphics panel, brightness, fullscreen mode, vsync and pixelization
    private Utility utils;
    public PlayerInformationManager pim;
    private Volume volume;
    private Bloom bloom;
    private Vignette vignette;
    public Slider brightnessSlider;
    public TMP_Text brightnessSliderText;
    public Slider fovSlider;
    [Range(30f, 60f)]
    public float currentFOV;
    public float tempFov;
    public float minFOV = 30f;
    public float maxFOV = 60f;
    public float fovChangeSpeed = 10f;
    public bool fullscreen = true;
    public Toggle fullscreenToggle;
    public Text fullscreenlabelText;
    public Toggle vsyncToggle;
    public Text vsyncText;
    public float currentBrightness;
    public RenderTexture highPixelTexture, midHighPixelTexture, midLowPixelTexture, lowPixelTexture, renderTexture;
    public Camera mainCamera;
    public CinemachineBrain playerCamera;
    public CinemachineCamera playerVirtualCamera;
    public GameObject player;
    private Color ambientLightColor = Color.white;
    public Renderer pixelRender;
    public RawImage pixelizationRawImage;
    string sceneName = "SettingsMenu";

    private BlackboardController blackboardController;
    private Blackboard blackboard;

    private FizzyNetworkManager manager;
    private FizzyNetworkManager Manager
    {
        get
        {
            if (manager != null)
            {
                return manager;
            }

            if (FizzyNetworkManager.singleton != null)
            {
                return manager = FizzyNetworkManager.singleton as FizzyNetworkManager;
            }

            return ServiceLocator.For(this).Get<FizzyNetworkManager>();
        }
    }

    private void Awake()
    {
        try
        {
            Transform parent = this.transform.parent;
            blackboardController = ServiceLocator.Global.Get<BlackboardController>();
            blackboard = blackboardController.GetBlackboard();
            utils = ServiceLocator.Global.Get<Utility>();
            if (utils.isOnline)
            {
                if (blackboard.TryGetValue(BlackboardController.BlackboardKeyStrings.LocalPLayer, out GameObject localPlayer))
                {
                    pim = localPlayer.gameObject.GetComponent<PlayerInformationManager>();
                    mainCamera = localPlayer.GetComponentInChildren<Camera>();
                    Target target = pim.gameObject.GetComponent<TargetDirectory>().GetTarget(TargetDirectory.TargetType.Camera).target;
                    playerVirtualCamera = target.gameObject.GetComponentInChildren<CinemachineCamera>();
                }
            }
            else
            {
                pim = utils.gameObject.GetComponent<PlayerInformationManager>();                
            }
        }
        catch (Exception e)
        {
            utils.DebugOut(e);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }

        fovSlider.minValue = 30f;
        fovSlider.maxValue = 60f;
        fovSlider.value = pim.settings.fovValue.value;
        
        playerVirtualCamera.Lens.FieldOfView = fovSlider.value;

        //fovSlider.onValueChanged.AddListener(SetFovFromSlider);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        try
        {
            PullSettings(pim);
        }
        catch (Exception e)
        {
            utils.DebugOut(e);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }

    //private void Update()
    //{
    //    playerCamera = Camera.main.GetComponent<CinemachineBrain>();
    //    CinemachineCamera playerVirtualCamera;
    //    if (playerCamera != null)
    //    {
    //        playerVirtualCamera = playerCamera.GetComponent<CinemachineCamera>();
    //        tempFov = playerVirtualCamera.Lens.FieldOfView;
    //    }
    // }

    public void PullSettings(PlayerInformationManager _pim)
    {
        try
        {
            Screen.brightness = _pim.settings.brightness.value;
            brightnessSlider.value = _pim.settings.brightness.value;
            fullscreenToggle.isOn = _pim.settings.fullscreen.value;
            vsyncToggle.isOn = _pim.settings.vsyncCount.value;
            fovSlider.value = _pim.settings.fovValue.value;
        }
        catch (Exception e)
        {
            utils.DebugOut(e);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }

    public void ResetSettings()
    {
        try
        {
            pim.settings.brightness.ResetProperty();
            pim.settings.fullscreen.ResetProperty();
            pim.settings.vsyncCount.ResetProperty();
            pim.settings.pixelizationValue.ResetProperty();
            pim.settings.fovValue.ResetProperty();
            PullSettings(pim);
        }
        catch (Exception e)
        {
            utils.DebugOut(e);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }

    public void Brightness()
    {

        try
        {
            Screen.brightness = brightnessSlider.value;
            pim.settings.brightness.value = brightnessSlider.value;
            RenderSettings.ambientLight = ambientLightColor;
            ambientLightColor.a = brightnessSlider.value;
            //brightnessSliderText.text = brightnessSlider.value.ToString();
            //float sliderMax = bloom.scatter.max + vignette.intensity.max;
            //float sliderCurent = bloom.scatter.value + vignette.intensity.value;
            //brightnessSlider.value = sliderCurent / sliderMax;
            //brightnessSliderText.text = brightnessSlider.value.ToString();
        }
        catch (Exception e)
        {
            utils.DebugOut(e);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }

    public void FullscreenToggle()
    {
        try
        {
            Screen.fullScreen = fullscreenToggle.isOn;
            pim.settings.fullscreen.value = fullscreenToggle.isOn;
            if (fullscreenToggle.isOn)
            {
                fullscreenlabelText.text = "On";
            }
            else
            {
                fullscreenlabelText.text = "Off";
            }
        }
        catch (Exception e)
        {
            utils.DebugOut(e);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }

    public void VsyncToggle()
    {
        try
        {
            QualitySettings.vSyncCount = vsyncToggle.isOn ? 1 : 0;
            pim.settings.vsyncCount.value = vsyncToggle.isOn;
            if (vsyncToggle.isOn)
            {
                vsyncText.text = "On";
            }
            else
            {
                vsyncText.text = "Off";
            }
        }
        catch (Exception ex)
        {
            utils.DebugOut(ex);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }

    //public void SetFovFromSlider(float newFov)
    //{
    //    try
    //    {
    //        //something in here is causing the value to return to 0
    //        pim.settings.fovValue.value = newFov;
    //        playerVirtualCamera.Lens.FieldOfView = pim.settings.fovValue.value;
    //    }
    //    catch (Exception fov)
    //    {
    //        utils.DebugOut(fov);
    //        utils.DebugOut(new System.Diagnostics.StackTrace());
    //    }
    //}

    public void SetSliderFromCameraFOV()
    {
        currentFOV = fovSlider.value;
        PlayerPrefs.SetFloat("FOVValue", currentFOV);
    }

    //public void FOVSlider()
    //{
    //    currentFOV = tempFov;
    //    //may need this formula later. but not sure if it's doing what I wanted it to do.
    //        /*Mathf.Lerp(currentFOV, fovSlider.value, fovChangeSpeed * Time.deltaTime);*/
    //    //currentFOV = 40f;
    //    //tempCurrentFov = GetSliderValueFromFOV(fovSlider.value);
    //    pim.settings.fovValue.value = currentFOV;
    //    playerVirtualCamera.Lens.FieldOfView = currentFOV;

    //    //PlayerPrefs.SetFloat("FOVValue", fovSlider.value);
    //    Debug.LogError($"FOVValue : {fovSlider.value}");
    //}

    //public void PixelationSlider()

    //{

    //    float highPixel = 0.75f;
    //    float midHighPixel = 0.5f;
    //    float midLowPixel = 0.25f;
    //    float lowPixel = 0f;
    //    try
    //    {
    //        RenderTexture textureToApply = null;

    //        if (pixelSlider.value >= highPixel)
    //        {
    //            textureToApply = highPixelTexture;
    //            mainCamera.targetTexture = highPixelTexture;
    //            if (pixelizationRawImage != null)
    //            {
    //                pixelizationRawImage.texture = highPixelTexture;
    //                pixelizationRawImage.gameObject.SetActive(true);
    //            }
    //            PlayerPrefs.SetFloat("PixelizationValue", pixelSlider.value);
    //            Debug.LogError("High Pixelization");
    //        }
    //        else if (pixelSlider.value >= midHighPixel && pixelSlider.value < highPixel)
    //        {
    //            textureToApply = midHighPixelTexture;
    //            mainCamera.targetTexture = midHighPixelTexture;
    //            if (pixelizationRawImage != null)
    //            {
    //                pixelizationRawImage.texture = midHighPixelTexture;
    //                pixelizationRawImage.gameObject.SetActive(true);
    //            }
    //            PlayerPrefs.SetFloat("PixelizationValue", pixelSlider.value);
    //            Debug.LogError("Mid High Pixelization");
    //        }
    //        else if (pixelSlider.value >= midLowPixel && pixelSlider.value < midHighPixel)
    //        {
    //            textureToApply = midLowPixelTexture;
    //            mainCamera.targetTexture = midLowPixelTexture;
    //            if (pixelizationRawImage != null)
    //            {
    //                pixelizationRawImage.texture = midLowPixelTexture;
    //                pixelizationRawImage.gameObject.SetActive(true);
    //            }
    //            PlayerPrefs.SetFloat("PixelizationValue", pixelSlider.value);
    //            Debug.LogError("Mid Low Pixelization");
    //        }
    //        else if(pixelSlider.value >= lowPixel && pixelSlider.value < midLowPixel)
    //        {
    //            textureToApply = lowPixelTexture;
    //            mainCamera.targetTexture = lowPixelTexture;
    //            if (pixelizationRawImage != null)
    //            {
    //                pixelizationRawImage.texture = lowPixelTexture;
    //                pixelizationRawImage.gameObject.SetActive(true);
    //            }
    //            PlayerPrefs.SetFloat("PixelizationValue", pixelSlider.value);
    //            Debug.LogError("Low Pixelization");
    //        }
    //        else if(pixelSlider.value == 0 )
    //        {
    //            textureToApply = renderTexture;
    //            mainCamera.targetTexture = renderTexture;
    //            if (pixelizationRawImage != null)
    //            {
    //                pixelizationRawImage.texture = renderTexture;
    //                pixelizationRawImage.gameObject.SetActive(true);
    //            }
    //            PlayerPrefs.SetFloat("PixelizationValue", pixelSlider.value);
    //            Debug.LogError("Not Pixelization");
    //        }


    //        if (utils.isOnline)
    //        {
    //            // Additional logic for online mode can be added here if needed
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        utils.DebugOut(ex);
    //        utils.DebugOut(new System.Diagnostics.StackTrace());
    //    }
    //}

    public void ApplyTempSettings(PlayerInformationManager pim, bool isOnline)
    {
        try
        {
            if (isOnline)
            {
                playerVirtualCamera.Lens.FieldOfView = pim.settings.fovValue.value;
            }

            Screen.brightness = pim.settings.brightness.value;
        }
        catch (Exception ex)
        {
            utils.DebugOut(ex);
            utils.DebugOut(new System.Diagnostics.StackTrace());
        }
    }
}

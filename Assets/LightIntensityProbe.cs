using Mirror.BouncyCastle.Asn1.X509;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public class LightIntensityProbe : MonoBehaviour
{
    public float lightLevel;
    [SerializeField] public Renderer monitoredRenderer;
    [SerializeField] TMP_Text intensityDisplay;
    [SerializeField] GameObject probe;

    private void Update() => GetLightIntensity();

    private void GetLightIntensity()
    {
        //LightProbes.GetInterpolatedProbe(probe.transform.position, monitoredRenderer, out SphericalHarmonicsL2 harmonics);
        ////float b = 0f;
        ////float g = 0f;
        ////float r = 0f;
        ////for (int i = 0; i < 9; i++)
        ////{
        ////    b += harmonics[0, i];
        ////    g += harmonics[1, i];
        ////    r += harmonics[2, i];
        ////}
        ////b = b / 9;
        ////g = g / 9;
        ////r = r / 9;
        ////lightLevel = b + g + r;
        //lightLevel = (0.2989f * harmonics[0, 0]) + (0.5870f * harmonics[1, 0]) + (0.1140f * harmonics[2, 0]);
        //intensityDisplay.text = lightLevel.ToString();

    }
}

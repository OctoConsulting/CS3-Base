using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI validText;
    public TextMeshProUGUI latText;
    public TextMeshProUGUI lngText;
    public TextMeshProUGUI altText;
    public static TextMeshProUGUI centerText;
    public static TextMeshProUGUI errorText;

    private void Awake()
    {
        centerText = GameObject.Find("CenterText").GetComponent<TextMeshProUGUI>();
        errorText = GameObject.Find("ErrorText").GetComponent<TextMeshProUGUI>();
    }
    // Start is called before the first frame update
    void Start()
    {
        errorText.text = "";
    }

    // Update is called once per frame
    void Update()
    {
#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
        // keep the validText object up to date from the beginning
        validText.text = GeolocationSDKView.geoVALIDATION;
        // wait until we get valid geolocation service running
        if (GeolocationSDKView.geoINIT == "- Successful -")
        {
            latText.text = GeolocationSDKView.geoNextLAT;
            lngText.text = GeolocationSDKView.geoNextLNG;
            altText.text = GeolocationSDKView.geoNextALT;
        }

#else // running in the editor
        // keep the validText object up to date from the beginning
        validText.text = "Running in Editor";
        latText.text = "No Latitude";
        lngText.text = "No Longitude";
        altText.text = "No Altitude";

#endif
    }


    public static void PuckCenterPressed()
    {
        centerText.text = "Center PRESSED!";
    }
    public static void PuckCenterReleased()
    {
        centerText.text = "~ Center ~";
    }

    public static void HudsonInputError(string message)
    {
        errorText.text = message;
    }

}

using System.Collections;
using UnityEngine;
using TMPro;
using System.Text;
using System.Globalization;


#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
using System;
using Windows.Foundation;
using Windows.Devices.Geolocation;
#endif

public class GeoLocationManager : MonoBehaviour
{

    #region Public Members to set in Editor
    [Header("GeoLocation Key Data")]
    public TextMeshProUGUI GeoLatText;
    public TextMeshProUGUI GeoLngText;
    public TextMeshProUGUI GeoAltText;

    [Header("GeoLocation Status")]
    public TextMeshProUGUI AccessStatusText;
    public TextMeshProUGUI ExceptionText;

    [Header("GeoLocation Misc")]
    public TextMeshProUGUI GeoLastLatText;
    public TextMeshProUGUI GeoLastLngText;
    public TextMeshProUGUI GeoLastAltText;
    public TextMeshProUGUI GeoValidationText;
    public TextMeshProUGUI GeoInitText;

    #endregion

    ///<summary> In the case where this code is applied to a fresh scene and maybe the inspector fields weren't populated
    /// fill them here. Of course this requires the core UI elements to remain unchanged... </summary>
    private void Awake()
    {
        if (GeoLatText == null)
            GeoLatText = GameObject.Find("LatCoord").GetComponent<TextMeshProUGUI>();
        if (GeoLngText == null)
            GeoLngText = GameObject.Find("LngCoord").GetComponent<TextMeshProUGUI>();
        if (GeoAltText == null)
            GeoLngText = GameObject.Find("Altitude").GetComponent<TextMeshProUGUI>();
        if (AccessStatusText == null)
            AccessStatusText = GameObject.Find("StatusData").GetComponent<TextMeshProUGUI>();
        if (ExceptionText == null)
            ExceptionText = GameObject.Find("ExceptionData").GetComponent<TextMeshProUGUI>();

    }

    private void Update()
    {
        // if we're running on the headset...
#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
// set the fields based on the GeolocationSDK data
GeoLatText.text = GeolocationSDKView.geoNextLAT;
GeoLngText.text = GeolocationSDKView.geoNextLNG;
GeoAltText.text = GeolocationSDKView.geoNextALT;

AccessStatusText.text = GeolocationSDKView.geoSTATUS;
ExceptionText.text = GeolocationSDKView.geoEXCEPTION;

#else
        // we're running in the editor
        GeoLatText.text = "00.00000000000000000";
        GeoLngText.text = "-11.11111111111111111";
        GeoAltText.text = "00.00";

        AccessStatusText.text = "<- Editor ->";
        ExceptionText.text = "<- Editor ->";

#endif

    }

}
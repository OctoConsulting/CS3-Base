using System.Collections;
using UnityEngine;
using TMPro;
using System.Text;
using System.Globalization;

#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
using Windows.Devices.Geolocation;
#endif

/// <summary>
/// This class encapsulates the View of the Geolocation SDK sample app.  It implements which and how
/// data is displayed in the UI.  This is a MonoBehavior script attached to the main UI Canvas in the
/// Unity scene.
/// </summary>
public class GeolocationSDKView : MonoBehaviour
{
    private static string geoAccessResult = string.Empty;
    private float GEOLOCATION_VALIDATION_START_DELAY = 15.0f; //seconds

    /// <summary>
    /// This class is implemented as a thread-safe lazy-instantiated singleton.
    /// </summary>
    GeolocationSDKView()
    {
        //Empty on purpose
    }
    private static readonly object padLock = new object();
    private static GeolocationSDKView instance = null;
    public static GeolocationSDKView Instance
    {
        get
        {
            if (instance != null)
            {
                return instance;
            }
            lock (padLock)
            {
                if (instance != null)
                {
                    instance = new GeolocationSDKView();
                }
            }
            return instance;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        GameObject mainCanvasUserUI = GameObject.FindWithTag("MainCanvasUserUI");
        GameObject mainCanvasDeveloperUI = GameObject.FindWithTag("MainCanvasDeveloperUI");
#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
#if USING_USER_UI
        mainCanvasDeveloperUI.SetActive(false);
#else
        mainCanvasUserUI.SetActive(false);
#endif
        StartCoroutine(StartBubbledExceptionCatcher());
        StartCoroutine(StartGeolocationValidationDelayed());
#else
        UpdateTextMeshProByTag("GeolocationValidationStartCounter", "- Unsupported platform in this SDK sample -");
#endif
    }

    // Update is called once per frame
    void Update()
    {
        //Empty on purpose
    }

#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
    /// <summary>
    /// Coroutine that will display in the UI the description of any exception that may have bubbled from
    /// the Windows.Devices.Geolocation objects.
    /// </summary>
    IEnumerator StartBubbledExceptionCatcher()
    {
        while (true)
        {
            UpdateTextMeshProByTag("GeolocationBubbledException", GeolocationSDKViewModel.Instance.bubbledExceptionMessage);
            yield return new WaitForSeconds(2.0f);
        }
    }
#endif

#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
    /// <summary>
    /// Coroutine that triggers the Geolocator initialization after a delay time.
    /// </summary>
    IEnumerator StartGeolocationValidationDelayed()
    {
        while (true)
        {
            if (Time.realtimeSinceStartup > GEOLOCATION_VALIDATION_START_DELAY)
            {
                UpdateTextMeshProByTag("GeolocationValidationStartCounter", "- Completed -");
                GeolocationAccessStatus geolocationAccessStatus = GeolocationSDKViewModel.Instance.GetGeolocationAccessStatus();
                switch (geolocationAccessStatus)
                {
                    case GeolocationAccessStatus.Allowed:
                        geoAccessResult = "<- Allowed ->";
                        break;
                    case GeolocationAccessStatus.Denied:
                        geoAccessResult = "<- Denied ->";
                        break;
                    case GeolocationAccessStatus.Unspecified:
                        geoAccessResult = "<- Unspecified ->";
                        break;
                }
                UpdateTextMeshProByTag("GeolocationAccessStatus", geoAccessResult);
                StartCoroutine(InitializeGeolocator());
                yield break;
            }
            else
            {
                yield return null;
                UpdateTextMeshProByTag("GeolocationValidationStartCounter", "T - " + (GEOLOCATION_VALIDATION_START_DELAY - Time.realtimeSinceStartup).ToString("F2", CultureInfo.InvariantCulture) + " seconds");
            }
        }
    }
#endif

#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
    /// <summary>
    /// Coroutine that triggers the Geolocator initialization from the ViewModel.
    /// </summary>
    IEnumerator InitializeGeolocator()
    {
        if (GeolocationSDKViewModel.Instance.IsGeolocationAccessAllowed)
        {
            if (GeolocationSDKViewModel.Instance.InitializeGeolocator(PositionAccuracy.Default, 100))
            {
                InitializeGeolocatorCallback();
            }
        }
        else 
        {
            Debug.LogWarning("Attempted to initialize Geolocator when geolocation access is not allowed.");
        }

        yield return null;
    }
#endif

#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
    /// <summary>
    /// Callback of InitializeGeolocator() that updates the UI after a successfull Geolocator initialiation.
    /// </summary>
    void InitializeGeolocatorCallback()
    {
        UpdateTextMeshProByTag("GeolocationInitialization", "- Successful -");
        StartCoroutine(UpdateGeolocationInitializationParametersUICoroutine());
        StartCoroutine(UpdateGeolocationStatusUICoroutine());
        StartCoroutine(UpdateGeolocationPositionUICoroutine());
    }
#endif

#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
    /// <summary>
    /// Coroutine that updates the UI with the Geolocator parameters.
    /// </summary>
    IEnumerator UpdateGeolocationInitializationParametersUICoroutine()
    {
        UpdateGeolocationInitializationParametersUICallback();
        yield return null;
    }
#endif

#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
    /// <summary>
    /// Callback of UpdateGeolocationInitializationParametersUICoroutine() to update the UI.
    /// </summary>
    void UpdateGeolocationInitializationParametersUICallback()
    {
        switch (GeolocationSDKViewModel.Instance.PositionAccuracy)
        {
            case PositionAccuracy.Default:
                UpdateTextMeshProByTag("GeolocationInitDesiredAccuracy", "- Default -");
                break;
            case PositionAccuracy.High:
                UpdateTextMeshProByTag("GeolocationInitDesiredAccuracy", "- High -");
                break;
            default:
                break;
        }
        UpdateTextMeshProByTag("GeolocationInitDesiredAccuracyInMeters", GeolocationSDKViewModel.Instance.DesiredAccuracyInMeters.ToString());
        UpdateTextMeshProByTag("GeolocationInitReportInterval", GeolocationSDKViewModel.Instance.ReportInterval.ToString());
        UpdateTextMeshProByTag("GeolocationInitMovementThreshold", GeolocationSDKViewModel.Instance.MovementThreshold.ToString());
    }
#endif

#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
    /// <summary>
    /// Coroutine that checks for new geolocation status in the ViewModel queue.  If there is a new geolocation
    /// status then the UI is updated accordingly.
    /// </summary>
    IEnumerator UpdateGeolocationStatusUICoroutine()
    {
        while (true) //Here one can add a flag to stop polling for Geolocation status changes ;-)
        {
            yield return new WaitForSeconds(1.0f);
            lock (GeolocationSDKViewModel.Instance.geolocationStatusQueue)
            {
                if (GeolocationSDKViewModel.Instance.geolocationStatusQueue.Count > 0)
                {
                    PositionStatus lastStatus = GeolocationSDKViewModel.Instance.geolocationStatusQueue.Dequeue();
                    string lastStatusString = GetStringForStatus(lastStatus);
                    UpdateGeolocationStatusUICallback(lastStatusString);
                }
            }
        }
    }
#endif

#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
    /// <summary>
    /// Callback of UpdateGeolocationStatusUICoroutine() to update the UI.
    /// </summary>
    /// <param name="geolocationStatus"></param>
    void UpdateGeolocationStatusUICallback(string geolocationStatus)
    {
        UpdateTextMeshProByTag("GeolocationLastStatus", geolocationStatus);
    }
#endif

#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
    /// <summary>
    /// Coroutine that checks for a new geolocation position in the ViewModel queue.  If there is a new geolocation
    /// position then the UI is updated accordingly.
    /// </summary>
    IEnumerator UpdateGeolocationPositionUICoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1.0f);
            lock (GeolocationSDKViewModel.Instance.geolocationPositionQueue)
            {
                if (GeolocationSDKViewModel.Instance.geolocationPositionQueue.Count > 0)
                {
                    Geoposition lastPosition = GeolocationSDKViewModel.Instance.geolocationPositionQueue.Dequeue();
                    UpdateGeolocationNextPositionInQueueUICallback(lastPosition);
                    UpdateGeolocationLastPositionReadUICallback(lastPosition);
                }
                else
                {
                    UpdateGeolocationNextPositionInQueueUICallback("No new position");
                }
            }
        }
    }
#endif

#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
    /// <summary>
    /// Callback of UpdateGeolocationPositionUICoroutine() to update the UI.
    /// </summary>
    /// <param name="geoposition">Geoposition data</param>
    void UpdateGeolocationNextPositionInQueueUICallback(Geoposition geoposition)
    {
        UpdateTextMeshProByTag("NextPositionInQueueLatitude", geoposition.Coordinate.Point.Position.Latitude.ToString());
        UpdateTextMeshProByTag("NextPositionInQueueLongitude", geoposition.Coordinate.Point.Position.Longitude.ToString());
        UpdateTextMeshProByTag("NextPositionInQueueAltitude", geoposition.Coordinate.Point.Position.Altitude.ToString());
    }
#endif

#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
    /// <summary>
    /// Callback of UpdateGeolocationPositionUICoroutine() to update the UI.
    /// </summary>
    /// <param name="message">Message to display in each field</param>
    void UpdateGeolocationNextPositionInQueueUICallback(string message)
    {
        UpdateTextMeshProByTag("NextPositionInQueueLatitude", message);
        UpdateTextMeshProByTag("NextPositionInQueueLongitude", message);
        UpdateTextMeshProByTag("NextPositionInQueueAltitude", message);
    }
#endif

#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
    /// <summary>
    /// Callback of UpdateGeolocationPositionUICoroutine() to update the UI.
    /// </summary>
    /// <param name="geoposition">Geoposition data</param>
    void UpdateGeolocationLastPositionReadUICallback(Geoposition geoposition)
    {
        UpdateTextMeshProByTag("LastPositionReadLatitude", geoposition.Coordinate.Point.Position.Latitude.ToString());
        UpdateTextMeshProByTag("LastPositionReadLongitude", geoposition.Coordinate.Point.Position.Longitude.ToString());
        UpdateTextMeshProByTag("LastPositionReadAltitude", geoposition.Coordinate.Point.Position.Altitude.ToString());
    }
#endif

#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
    /// <summary>
    /// Gets a string for the UI of a given PositionStatus.
    /// </summary>
    /// <param name="positionStatus">PositionStatus to convert to string.</param>
    /// <returns>String version of the PositionStatus parameter.</returns>
    private string GetStringForStatus(PositionStatus positionStatus)
    {
        switch (positionStatus)
        {
            case PositionStatus.Ready:
                return "- Ready -";
            case PositionStatus.Initializing:
                return "- Initializing -";
            case PositionStatus.NoData:
                return "- No data -";
            case PositionStatus.Disabled:
                return "- Disabled -";
            case PositionStatus.NotInitialized:
                return "- Not initialized -";
            case PositionStatus.NotAvailable:
                return "- Not available -";
            default:
                return "- WARNING: Unknown position status -";
        }
    }
#endif

    /// <summary>
    /// Helper method to update a UI component.
    /// NOTE:  This is not an optimized implementation and its usage is Not encouraged for production
    ///        code.
    /// </summary>
    /// <param name="tag">Tag of the TextMeshPro UI element to update.</param>
    /// <param name="newText">Text that replaces the existing text in the TextMeshPro.</param>
    public void UpdateTextMeshProByTag(string tag, string newText)
    {
        GameObject textMeshProGameObject = GameObject.FindWithTag(tag);

        if (textMeshProGameObject != null)
        {
            textMeshProGameObject.GetComponent<TextMeshProUGUI>().text = newText;
        }
        else
        {
            Debug.LogWarning($"Unknown TextMeshPro tag: '{tag}'");
        }
    }

}

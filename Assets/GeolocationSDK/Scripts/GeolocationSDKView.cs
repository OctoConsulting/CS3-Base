using System.Collections;
using UnityEngine;
using TMPro;
using System.Text;
using System.Globalization;

#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
using Windows.Devices.Geolocation;
#endif

/// <summary>
/// This class has been updated from the main SDK example which contained a very specific implementation.
/// </summary>
public class GeolocationSDKView : MonoBehaviour
{
    ///<summary> All the variables to pass out to other classes</summary>
    #region Public Access
    public static string geoNextLAT = string.Empty;
    public static string geoLastLAT = string.Empty;
    public static string geoNextLNG = string.Empty;
    public static string geoLastLNG = string.Empty;
    public static string geoNextALT = string.Empty;
    public static string geoLastALT = string.Empty;
    public static string geoSTATUS = string.Empty;
    public static string geoLastSTATUS = string.Empty;
    public static string geoEXCEPTION = string.Empty;
    public static string geoVALIDATION = string.Empty;
    public static string geoINIT = string.Empty;

    public static string desiredAccuracyInMeters = string.Empty;
    public static string desiredAccuracy = string.Empty;
    public static string reportInterval = string.Empty;
    public static string movementThreshold = string.Empty;

    #endregion


    /// <summary>
    /// This class is implemented as a thread-safe lazy-instantiated singleton.
    /// </summary>
    GeolocationSDKView()
    {
        //Empty on purpose
    }

    #region Private Members
    private static string geoAccessResult = string.Empty;
    private float GEOLOCATION_VALIDATION_START_DELAY = 15.0f; //seconds

    #endregion


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

#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
        StartCoroutine(StartBubbledExceptionCatcher());
        StartCoroutine(StartGeolocationValidationDelayed());

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
            geoEXCEPTION = GeolocationSDKViewModel.Instance.bubbledExceptionMessage;
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
                geoVALIDATION = "- Completed -";
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
                geoSTATUS = geoAccessResult;
                StartCoroutine(InitializeGeolocator());
                yield break;
            }
            else
            {
                yield return null;
                geoVALIDATION = "T - " + (GEOLOCATION_VALIDATION_START_DELAY - Time.realtimeSinceStartup).ToString("F2", CultureInfo.InvariantCulture) + " seconds";
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
        geoINIT = "- Successful -";
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
                    geoLastSTATUS = lastStatusString;
                }
            }
        }
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
        geoNextLAT = geoposition.Coordinate.Point.Position.Latitude.ToString();
        geoNextLNG = geoposition.Coordinate.Point.Position.Longitude.ToString();
        geoNextALT = geoposition.Coordinate.Point.Position.Altitude.ToString();
    }
#endif

#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
    /// <summary>
    /// Callback of UpdateGeolocationPositionUICoroutine() to update the UI.
    /// </summary>
    /// <param name="message">Message to display in each field</param>
    void UpdateGeolocationNextPositionInQueueUICallback(string message)
    {
        geoNextLAT =  message;
        geoNextLNG =  message;
        geoNextALT =  message;
    }
#endif

#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
    /// <summary>
    /// Callback of UpdateGeolocationPositionUICoroutine() to update the UI.
    /// </summary>
    /// <param name="geoposition">Geoposition data</param>
    void UpdateGeolocationLastPositionReadUICallback(Geoposition geoposition)
    {
        geoLastLAT = geoposition.Coordinate.Point.Position.Latitude.ToString();
        geoLastLNG =  geoposition.Coordinate.Point.Position.Longitude.ToString();
        geoLastALT =  geoposition.Coordinate.Point.Position.Altitude.ToString();
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


#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
        /// <summary>
        /// Callback of UpdateGeolocationInitializationParametersUICoroutine() to update the UI.
        /// </summary>
        void UpdateGeolocationInitializationParametersUICallback()
        {
            switch (GeolocationSDKViewModel.Instance.PositionAccuracy)
            {
                case PositionAccuracy.Default:
                    desiredAccuracy = "- Default -";
                    break;
                case PositionAccuracy.High:
                   desiredAccuracy = "- High -";
                    break;
                default:
                    break;
            }
            desiredAccuracyInMeters = GeolocationSDKViewModel.Instance.DesiredAccuracyInMeters.ToString();
            reportInterval = GeolocationSDKViewModel.Instance.ReportInterval.ToString();
            movementThreshold = GeolocationSDKViewModel.Instance.MovementThreshold.ToString();
        }
#endif


}

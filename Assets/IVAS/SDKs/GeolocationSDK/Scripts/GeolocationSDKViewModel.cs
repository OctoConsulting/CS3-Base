using System.Collections.Generic;
using UnityEngine;

#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
using Windows.Devices.Geolocation;
#endif

/// <summary>
/// This class encapsulates the ViewModel of the Geolocation SDK sample app.  It implements the logic
/// and keeps the state of the app.  This class holds two queues to store the changes in geolocation
/// position and status.  These new positions and statuses are picked up by the View to refresh the
/// UI correspondingly.  Given the simplicity of the SDK sample app the rest of this class acts mostly
/// as a pass-through class, however, it is important to understand that in this class is where any
/// additional logic would be implemented for expanding the SDK into a production-ready product.  Examples
/// of additional logic to implement in this class would be: validating user permissions, validating
/// hardware specifications, validating OS version, condition the geolocator precission to a certain
/// condition, etc etc etc.
/// </summary>
public class GeolocationSDKViewModel
{
    public bool IsGeolocationAccessAllowed { get; private set; } = false;

#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
    public PositionStatus GeolocationStatus { get; private set; } = PositionStatus.NotInitialized;
    public Geoposition GeolocationPosition { get; private set; }
    public PositionAccuracy PositionAccuracy { get; private set; } = PositionAccuracy.Default;
    public uint DesiredAccuracyInMeters { get; private set; } = uint.MaxValue;
    public uint ReportInterval { get; private set; } = 5000;
    public double MovementThreshold { get; private set; } = double.NaN;
    public string bubbledExceptionMessage = string.Empty;

    public Queue<PositionStatus> geolocationStatusQueue = new Queue<PositionStatus>();
    public Queue<Geoposition> geolocationPositionQueue = new Queue<Geoposition>();
#endif

    /// <summary>
    /// This class is implemented as a thread-safe lazy-instantiated singleton.
    /// </summary>
    GeolocationSDKViewModel()
    {
#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
        GeolocationSDKModel.Instance.GeolocatorStatusChangedEvent += OnStatusChanged;
        GeolocationSDKModel.Instance.GeolocatorPositionChangedEvent += OnPositionChanged;
#else
        //Empty on purpose
#endif
    }
    private static readonly object padLock = new object();
    private static GeolocationSDKViewModel instance = null;
    public static GeolocationSDKViewModel Instance
    {
        get
        {
            if (instance != null)
            {
                return instance;
            }
            lock (padLock)
            {
                if (instance == null)
                {
                    instance = new GeolocationSDKViewModel();
                }
            }
            return instance;
        }
    }

#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
    /// <summary>
    /// Gets the Geolocation Access Status for the Windows.Devices.Geolocation namespace via the Model.
    /// </summary>
    public GeolocationAccessStatus GetGeolocationAccessStatus()
    {
        //What else could you do here in the MVVM pattern?
        //Add logic to deal when this method can be called; for example: 
        //Is the user authorized? Is the device supposed to support it?, etc
        //In this SDK we use this method at the ViewModel as a pass-thru method, however, 
        //it is important to understand that, according to the MVVM architecture,
        //this is the best place where you could implement the logic related to
        //getting the geolocation access status ... friendly advice: use MVVM! it will save you
        //tons of hours in the future (including sleeping hours ;-)) in the future and it is highly scalable.

        GeolocationAccessStatus geolocationAccessStatus = GeolocationSDKModel.Instance.GetGeolocationAccessStatus();

        if (geolocationAccessStatus == GeolocationAccessStatus.Allowed)
        {
            IsGeolocationAccessAllowed = true;
        }

        return geolocationAccessStatus;
    }
#endif

#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
    /// <summary>
    /// Implements the logic to initialize the geolocator via the Model.
    /// </summary>
    /// <param name="positionAccuracy">PositionAccuracy for the Geolocator initialization.</param>
    /// <param name="desiredAccuracyInMeters">The desirec accuracy in meters for the Geolocator initialization.</param>
    /// <returns></returns>
    public bool InitializeGeolocator(PositionAccuracy positionAccuracy,
                                     uint desiredAccuracyInMeters)
    {
        //What else could you do here in the MVVM pattern?
        //Add logic to deal with custom logic involved in how to initialize the geolocator here.
        //In this SDK we use this method at the ViewModel as a pass-thru method, however, 
        //it is important to understand that, according to the MVVM architecture,
        //this is the best place where you could implement the logic related to initialize the
        //geolocator.

        PositionAccuracy = positionAccuracy;
        DesiredAccuracyInMeters = desiredAccuracyInMeters;

        if (IsGeolocationAccessAllowed)
        {
            if (GeolocationSDKModel.Instance.InitializeGeolocator(PositionAccuracy,
                                                                  DesiredAccuracyInMeters))
            {
                ReportInterval = GeolocationSDKModel.Instance.ReportInterval;
                MovementThreshold = GeolocationSDKModel.Instance.MovementThreshold;

                return true;
            }
            else
            {
                ReportInterval = uint.MaxValue;
                MovementThreshold = double.NaN;

                return false;
            }
        }
        else
        {
            Debug.LogWarning("Attempted to initialize the Geolocator when Geolocation Access is not allowed.");
        }
        return false;
    }
#endif

#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
    /// <summary>
    /// Event triggered when the corresponding Windows.Devices.Geolocation.Geolocator event is fired.
    /// </summary>
    /// <param name="sender">The Windows.Devices.Geolocation.Geolocator event source</param>
    /// <param name="args">Status changed event arguments.</param>
    private void OnStatusChanged(Geolocator sender, StatusChangedEventArgs args)
    {
        lock(geolocationStatusQueue)
        {
            geolocationStatusQueue.Enqueue(args.Status);
        }
    }
#endif

#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
    /// <summary>
    /// Event triggered when the corresponding Windows.Devices.Geolocation.Geolocator event is fired.
    /// </summary>
    /// <param name="sender">The Windows.Devices.Geolocation.Geolocator event source.</param>
    /// <param name="args">Status changed event arguments.</param>
    private void OnPositionChanged(Geolocator sender, PositionChangedEventArgs args)
    {
        lock (geolocationPositionQueue)
        {
            geolocationPositionQueue.Enqueue(args.Position);
        }
    }
#endif
}
